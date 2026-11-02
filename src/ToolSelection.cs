using System;

namespace HammerSort
{
    internal static class ToolSelection
    {
        internal static bool ShouldSort(string sharedName, Func<bool> isOdinArchitectActive)
        {
            // Untranslated shared names are stable across the player's selected language.
            // Short-circuit before consulting the optional plugin for every other tool.
            return sharedName == "$item_hammer"
                || ((sharedName == "$odin_hammer" || sharedName == "$StoneChisel")
                    && isOdinArchitectActive());
        }
    }
}
