using System;
using System.Collections.Generic;
using HammerSort;

internal static class SelectionRebuildTests
{
    private sealed class Entry { internal int Prefab; internal bool Affordable; }
    private static readonly Predicate<Entry> Affordable = delegate(Entry item) { return item.Affordable; };

    private static void Assert(bool result, string message)
    {
        if (!result) throw new Exception(message);
    }

    private static int FindPrefab(List<Entry> entries, Entry target)
    {
        // Valheim accepts either a menu prefab or a placed copy of the same prefab.
        for (int i = 0; i < entries.Count; ++i)
            if (entries[i].Prefab == target.Prefab) return i;
        return -1;
    }

    private static Entry GameSelect(List<Entry> current, List<Entry> baseline, Entry target)
    {
        // Verified order of calls in Player.SetSelectedPiece(Piece): resolve an index,
        // SetBuildCategory rebuilds the table, then select using the saved index.
        int index = FindPrefab(current, target);
        current.Clear();
        current.AddRange(baseline);
        int x = index % 15;
        int y = index / 15;
        return current[y * 15 + x];
    }

    internal static void Run()
    {
        Entry stakewall = new Entry { Prefab = 0 };
        Entry stonePortal = new Entry { Prefab = 1, Affordable = true };
        List<Entry> portalBaseline = new List<Entry> { stakewall, stonePortal };
        List<Entry> portalMenu = new List<Entry>(portalBaseline);
        StableOrder<Entry> portalOrder = new StableOrder<Entry>();
        portalOrder.Capture(portalMenu);
        portalOrder.Apply(portalMenu, Affordable);
        Assert(ReferenceEquals(portalMenu[0], stonePortal), "Affordable Stone Portal was not displayed first");
        Assert(ReferenceEquals(GameSelect(portalMenu, portalBaseline, stonePortal), stakewall), "Stone Portal/Stakewall bug was not reproduced");
        portalOrder.Apply(portalMenu, Affordable);
        portalOrder.Apply(portalMenu, null);
        Assert(ReferenceEquals(GameSelect(portalMenu, portalBaseline, stonePortal), stonePortal), "Clicking Stone Portal still selected Stakewall");

        bool reproducedOldBug = false;
        int verifiedClicks = 0;
        foreach (int categorySize in new[] { 1, 15, 16, 90, 137 })
        {
            List<Entry> baseline = new List<Entry>();
            for (int i = 0; i < categorySize; ++i)
                baseline.Add(new Entry { Prefab = i, Affordable = i % 3 == 1 });
            List<Entry> current = new List<Entry>(baseline);
            StableOrder<Entry> order = new StableOrder<Entry>();
            order.Capture(current);
            for (int inventoryChange = 0; inventoryChange < 4; ++inventoryChange)
            {
                for (int clickedSlot = 0; clickedSlot < categorySize; ++clickedSlot)
                {
                    order.Apply(current, Affordable);
                    Entry clicked = current[clickedSlot];
                    Entry oldResult = GameSelect(current, baseline, clicked);
                    if (!ReferenceEquals(oldResult, clicked)) reproducedOldBug = true;

                    order.Apply(current, Affordable);
                    // The production selection prefix performs this restoration before
                    // Valheim's first lookup, rather than waiting for its rebuild hook.
                    order.Apply(current, null);
                    Entry selected = GameSelect(current, baseline, clicked);
                    Assert(ReferenceEquals(selected, clicked), "Clicked icon selected a different prefab after rebuild");

                    // Repainting a sorted menu retains the selected prefab by identity.
                    order.Apply(current, Affordable);
                    Assert(FindPrefab(current, selected) >= 0, "Selection disappeared after repaint");

                    // Compact-grid input stays positional; it must not use the new prefix.
                    int x = clickedSlot % 15;
                    int y = clickedSlot / 15;
                    Assert(ReferenceEquals(current[y * 15 + x], clicked), "Compact-grid slot mapping changed");

                    // Piece-based selection is also used for copying existing structures.
                    Entry placedCopy = new Entry { Prefab = clicked.Prefab };
                    order.Apply(current, null);
                    Assert(GameSelect(current, baseline, placedCopy).Prefab == clicked.Prefab, "Placed-piece lookup failed");
                    ++verifiedClicks;
                }
                foreach (Entry item in baseline) item.Affordable = !item.Affordable;
            }
        }
        Assert(reproducedOldBug, "Regression fixture did not reproduce the old selection-index bug");
        Console.WriteLine("PASS: reproduced the pre-fix wrong-piece bug; " + verifiedClicks + " clicks preserve prefab identity across rebuilds, inventory changes and row boundaries.");
    }
}
