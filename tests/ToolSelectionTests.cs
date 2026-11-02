using System;
using HammerSort;

internal static class ToolSelectionTests
{
    private static void Assert(bool result, string message)
    {
        if (!result) throw new Exception(message);
    }

    internal static void Run()
    {
        Func<bool> unexpectedLookup = delegate { throw new Exception("Unnecessary optional plugin check"); };
        Assert(ToolSelection.ShouldSort("$item_hammer", unexpectedLookup), "Vanilla hammer must work without OdinArchitect");
        foreach (string name in new[] { null, "", "$item_hoe", "$item_cultivator", "$stonechisel", "Odin's Hammer", "$item_other_mod_hammer" })
            Assert(!ToolSelection.ShouldSort(name, unexpectedLookup), "An unrelated tool was enabled: " + name);

        bool active = false;
        int lookups = 0;
        Func<bool> optionalPlugin = delegate { ++lookups; return active; };
        foreach (string name in new[] { "$odin_hammer", "$StoneChisel" })
        {
            Assert(!ToolSelection.ShouldSort(name, optionalPlugin), "Absent/disabled OdinArchitect must not enable its tool");
            active = true;
            Assert(ToolSelection.ShouldSort(name, optionalPlugin), "Active OdinArchitect must enable both custom menus");
            active = false;
            Assert(!ToolSelection.ShouldSort(name, optionalPlugin), "Disabling OdinArchitect must take effect without restarting");
        }
        Assert(lookups == 6, "Optional plugin status must be checked only for its tools");
        Assert(ToolSelection.ShouldSort("$item_hammer", optionalPlugin), "Vanilla fallback failed after disabling OdinArchitect");
        Assert(lookups == 6, "Vanilla fallback must not query OdinArchitect");
        Console.WriteLine("PASS: OdinArchitect tool selection, absent/disabled fallback, live status changes and vanilla-only fast path.");
    }
}
