using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HammerSort
{
    [BepInPlugin(Guid, "HammerSort", Version)]
    [BepInDependency(OdinArchitectCompatibility.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class HammerSortPlugin : BaseUnityPlugin
    {
        public const string Author = "ERKEK2000";
        public const string Guid = Author + ".valheim.hammersort";
        public const string Version = "1.1.0";
        internal static ConfigEntry<bool> SortEnabled;
        internal static ConfigEntry<bool> OtherTools;
        internal static int Revision;
        private Harmony harmony;

        private void Awake()
        {
            SortEnabled = Config.Bind("General", "Enabled", true,
                "Put pieces you have materials for first, preserving the game order in both groups.");
            OtherTools = Config.Bind("General", "OtherBuildTools", false,
                "Also sort the hoe, cultivator and other build tools. The vanilla hammer and active OdinArchitect tools are supported by default.");
            SortEnabled.SettingChanged += OnSettingChanged;
            OtherTools.SettingChanged += OnSettingChanged;
            if (OdinArchitectCompatibility.Initialize())
                Logger.LogInfo("OdinArchitect detected; Odin's Hammer and Stone Chisel sorting is available automatically.");
            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(HammerSortPlugin).Assembly);
            Logger.LogInfo("HammerSort " + Version + " by " + Author + " loaded (client-side, inventory-based sorting).");
        }

        private static void OnSettingChanged(object sender, EventArgs args) { Invalidate(); }
        internal static void Invalidate() { unchecked { ++Revision; } }

        private void OnDestroy()
        {
            if (SortEnabled != null) SortEnabled.SettingChanged -= OnSettingChanged;
            if (OtherTools != null) OtherTools.SettingChanged -= OnSettingChanged;
            Browser.Restore();
            QuickGrid.Restore();
            if (harmony != null) harmony.UnpatchSelf();
        }

        internal static bool ShouldSort(Player player)
        {
            if (!SortEnabled.Value || player == null || player.GetBuildTool() == null) return false;
            if (OtherTools.Value) return true;
            ItemDrop.ItemData item = RightItem(player);
            return item != null && ToolSelection.ShouldSort(item.m_shared.m_name, OdinArchitectCompatibility.IsActive);
        }

        private static readonly Func<Humanoid, ItemDrop.ItemData> RightItem =
            (Func<Humanoid, ItemDrop.ItemData>)Delegate.CreateDelegate(
                typeof(Func<Humanoid, ItemDrop.ItemData>),
                AccessTools.Method(typeof(Humanoid), "GetRightItem"));

        [HarmonyPatch(typeof(Player), "OnInventoryChanged")]
        private static class InventoryChanged
        {
            private static void Postfix(Player __instance)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer)) Invalidate();
            }
        }

        [HarmonyPatch(typeof(Player), "SetNoPlacementCost")]
        private static class PlacementCostChanged
        {
            private static void Postfix() { Invalidate(); }
        }

        [HarmonyPatch(typeof(PieceTable), "UpdateAvailable")]
        private static class PiecesChanged
        {
            // Restore indices before the game overwrites the category lists.
            private static void Prefix(PieceTable __instance) { QuickGrid.BeforeRebuild(__instance); }
            private static void Postfix(PieceTable __instance)
            {
                QuickGrid.AfterRebuild(__instance);
                Invalidate();
            }
        }

        [HarmonyPatch(typeof(BuildUi), "UpdatePieceButtons")]
        private static class BrowserRebuilt
        {
            private static int depth;
            private static void Prefix() { ++depth; }

            [HarmonyPriority(Priority.Last)]
            private static void Postfix(BuildUi __instance, List<BuildUiPieceButton> ___m_pieceButtons)
            {
                // Search can recursively rebuild the browser. Only capture the final game order.
                if (depth == 1) Browser.Capture(__instance, ___m_pieceButtons);
            }

            private static Exception Finalizer(Exception __exception) { --depth; return __exception; }
        }

        [HarmonyPatch(typeof(BuildUi), "Update")]
        private static class BrowserUpdated
        {
            private static void Postfix(BuildUi __instance) { Browser.Refresh(__instance); }
        }

        [HarmonyPatch(typeof(BuildUi), "OpenBuildMenu")]
        private static class BrowserOpened
        {
            private static void Prefix() { Invalidate(); }
        }

        [HarmonyPatch(typeof(Hud), "UpdateBuild")]
        private static class QuickGridUpdated
        {
            private static void Prefix(Hud __instance, Player player, ref bool __1)
            {
                if (QuickGrid.Refresh(__instance, player)) __1 = true;
            }
        }
    }

    internal static class Materials
    {
        private static readonly Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        private static int revision = -1;
        private static Player owner;
        private static bool freePlacement;
        private static bool anyFreeCost;
        private static ZoneSystem zone;
        internal static readonly Predicate<Piece> HasMaterials = Check;
        internal static readonly Predicate<BuildUiPieceButton> ButtonHasMaterials = CheckButton;

        internal static void Prepare(Player player)
        {
            if (ReferenceEquals(owner, player) && revision == HammerSortPlugin.Revision) return;
            owner = player;
            revision = HammerSortPlugin.Revision;
            freePlacement = player.PlacementCostDisabled;
            zone = ZoneSystem.instance;
            anyFreeCost = zone != null && (zone.GetGlobalKey(GlobalKeys.NoBuildCost) || zone.GetGlobalKey(GlobalKeys.NoCraftCost));
            counts.Clear();
            List<ItemDrop.ItemData> items = player.GetInventory().GetAllItems();
            for (int i = 0; i < items.Count; ++i)
            {
                ItemDrop.ItemData item = items[i];
                // Match CountItems(name, -1, true), including world-level filtering.
                if (item.m_worldLevel < Game.m_worldLevel) continue;
                string name = item.m_shared.m_name;
                int count;
                counts.TryGetValue(name, out count);
                counts[name] = count + item.m_stack;
            }
        }

        private static bool CheckButton(BuildUiPieceButton button) { return Check(button.Piece); }

        private static bool Check(Piece piece)
        {
            if (piece == null) return false;
            if (piece.m_repairPiece || piece.m_removePiece || freePlacement) return true;
            if (anyFreeCost && zone.GetGlobalKey(piece.FreeBuildKey())) return true;
            Piece.Requirement[] resources = piece.m_resources;
            if (resources == null) return true;
            for (int i = 0; i < resources.Length; ++i)
            {
                Piece.Requirement resource = resources[i];
                if (resource == null || resource.m_resItem == null || resource.m_amount <= 0) continue;
                int count;
                if (!counts.TryGetValue(resource.m_resItem.m_itemData.m_shared.m_name, out count)
                    || count < resource.m_amount) return false;
            }
            return true;
        }
    }

    internal static class Browser
    {
        private static BuildUi owner;
        private static List<BuildUiPieceButton> buttons;
        private static readonly StableOrder<BuildUiPieceButton> order = new StableOrder<BuildUiPieceButton>();
        private static int revision = -1;
        private static bool sorting;
        private static readonly Action<BuildUi> ConfigureNavigation = (Action<BuildUi>)Delegate.CreateDelegate(
            typeof(Action<BuildUi>), AccessTools.Method(typeof(BuildUi), "ConfigureButtonNavigation"));

        internal static void Capture(BuildUi ui, List<BuildUiPieceButton> source)
        {
            owner = ui;
            buttons = source;
            order.Capture(source);
            revision = -1;
            Refresh(ui);
        }

        internal static void Refresh(BuildUi ui)
        {
            if (!ReferenceEquals(owner, ui)) return;
            Player player = Player.m_localPlayer;
            if (player == null) return;
            bool enabled = HammerSortPlugin.ShouldSort(player);
            if (revision == HammerSortPlugin.Revision && sorting == enabled) return;
            revision = HammerSortPlugin.Revision;
            sorting = enabled;
            if (enabled) Materials.Prepare(player);
            if (order.Apply(buttons, enabled ? Materials.ButtonHasMaterials : null)) UpdateLayout();
        }

        private static void UpdateLayout()
        {
            // Retain button objects, click handlers, search visibility and selected Piece.
            for (int i = 0; i < buttons.Count; ++i) buttons[i].transform.SetAsLastSibling();
            ConfigureNavigation(owner);
        }

        internal static void Restore()
        {
            if (owner != null && buttons != null && order.Apply(buttons, null)) UpdateLayout();
        }
    }

    internal static class QuickGrid
    {
        private sealed class Category
        {
            internal readonly StableOrder<Piece> Order = new StableOrder<Piece>();
            internal List<Piece> Pieces;
            internal int Revision = -1;
            internal bool Active;
        }

        private static PieceTable table;
        private static Category[] categories;
        private static readonly FieldInfo CategoryLists = AccessTools.Field(typeof(PieceTable), "m_availablePiecesByCategory");

        internal static bool Refresh(Hud hud, Player player)
        {
            if (!ReferenceEquals(player, Player.m_localPlayer)) return false;
            if (!HammerSortPlugin.ShouldSort(player)) { Restore(); return false; }
            if (hud.m_buildUi == null || !hud.m_buildUi.gameObject.activeSelf) return false;
            PieceTable current = player.GetBuildTool();
            if (!ReferenceEquals(table, current))
            {
                Restore();
                table = current;
                Capture();
            }
            int category = (int)table.GetSelectedCategory();
            if (categories == null || category < 0 || category >= categories.Length) return false;
            Category state = categories[category];
            if (state.Revision == HammerSortPlugin.Revision) return false;
            Materials.Prepare(player);
            bool changed = Apply(category, Materials.HasMaterials);
            state.Revision = HammerSortPlugin.Revision;
            return changed;
        }

        private static void Capture()
        {
            // Reflection only on a new tool or a game-driven list rebuild.
            List<List<Piece>> lists = (List<List<Piece>>)CategoryLists.GetValue(table);
            if (categories == null || categories.Length != lists.Count) categories = new Category[lists.Count];
            for (int i = 0; i < lists.Count; ++i)
            {
                if (categories[i] == null) categories[i] = new Category();
                categories[i].Pieces = lists[i];
                categories[i].Order.Capture(lists[i]);
                categories[i].Revision = -1;
                categories[i].Active = false;
            }
        }

        private static bool Apply(int category, Predicate<Piece> preferred)
        {
            Category state = categories[category];
            state.Active = preferred != null;
            int width = Math.Max(1, PieceTable.m_gridWidth);
            Piece selected = At(state.Pieces, table.m_selectedPiece, category, width);
            Piece last = At(state.Pieces, table.m_lastSelectedPiece, category, width);
            if (!state.Order.Apply(state.Pieces, preferred)) return false;
            Remap(state.Pieces, table.m_selectedPiece, category, width, selected);
            Remap(state.Pieces, table.m_lastSelectedPiece, category, width, last);
            return true;
        }

        private static Piece At(List<Piece> pieces, Vector2Int[] indices, int category, int width)
        {
            if (indices == null || category >= indices.Length) return null;
            Vector2Int index = indices[category];
            int offset = index.y * width + index.x;
            return offset >= 0 && offset < pieces.Count ? pieces[offset] : null;
        }

        private static void Remap(List<Piece> pieces, Vector2Int[] indices, int category, int width, Piece piece)
        {
            if (ReferenceEquals(piece, null) || indices == null || category >= indices.Length) return;
            for (int i = 0; i < pieces.Count; ++i)
            {
                if (ReferenceEquals(pieces[i], piece)) { indices[category] = new Vector2Int(i % width, i / width); return; }
            }
        }

        internal static void BeforeRebuild(PieceTable rebuilding)
        {
            if (ReferenceEquals(table, rebuilding)) Restore();
        }

        internal static void AfterRebuild(PieceTable rebuilding)
        {
            if (ReferenceEquals(table, rebuilding)) Capture();
        }

        internal static void Restore()
        {
            if (table == null || categories == null) return;
            for (int i = 0; i < categories.Length; ++i)
            {
                if (!categories[i].Active) continue;
                Apply(i, null);
                categories[i].Revision = -1;
            }
        }
    }
}
