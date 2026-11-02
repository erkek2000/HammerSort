using System;
using BepInEx;
using BepInEx.Bootstrap;

namespace HammerSort
{
    internal static class OdinArchitectCompatibility
    {
        internal const string PluginGuid = "Raelaziel.OdinArchitect";
        private static BaseUnityPlugin plugin;
        internal static readonly Func<bool> IsActive = CheckIsActive;

        internal static bool Initialize()
        {
            // BepInEx's soft dependency ensures OdinArchitect loads first when present.
            // Keep no assembly reference to OdinArchitect or its dependencies.
            PluginInfo info;
            plugin = Chainloader.PluginInfos.TryGetValue(PluginGuid, out info) ? info.Instance : null;
            return plugin != null;
        }

        private static bool CheckIsActive()
        {
            return plugin != null && plugin.isActiveAndEnabled;
        }
    }
}
