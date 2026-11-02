using System;
using System.Collections.Generic;
using System.Diagnostics;
using HammerSort;

internal static class StableOrderTests
{
    private sealed class Entry { internal int Id; internal bool Affordable; }
    private static readonly Predicate<Entry> Affordable = delegate(Entry e) { return e.Affordable; };
    private static int assertions;

    private static void Assert(bool value, string message)
    {
        ++assertions;
        if (!value) throw new Exception(message);
    }

    private static int Main()
    {
        try
        {
            ToolSelectionTests.Run();
            Random random = new Random(42);
            foreach (int size in new[] { 0, 1, 2, 15, 90, 1000 })
            {
                List<Entry> original = new List<Entry>();
                for (int i = 0; i < size; ++i) original.Add(new Entry { Id = i });
                List<Entry> actual = new List<Entry>(original);
                StableOrder<Entry> order = new StableOrder<Entry>();
                order.Capture(actual);
                for (int round = 0; round < 100; ++round)
                {
                    foreach (Entry entry in original) entry.Affordable = random.Next(2) == 0;
                    List<Entry> expected = original.FindAll(Affordable);
                    expected.AddRange(original.FindAll(delegate(Entry e) { return !e.Affordable; }));
                    order.Apply(actual, Affordable);
                    Assert(actual.Count == size, "Entry lost or added");
                    for (int i = 0; i < size; ++i) Assert(ReferenceEquals(actual[i], expected[i]), "Unstable order");
                    Assert(!order.Apply(actual, Affordable), "Unchanged order should avoid UI refresh");
                }
                order.Apply(actual, null);
                for (int i = 0; i < size; ++i) Assert(ReferenceEquals(actual[i], original[i]), "Restore failed");
                foreach (Entry entry in original) entry.Affordable = true;
                Assert(!order.Apply(actual, Affordable), "All affordable should preserve original order");
                foreach (Entry entry in original) entry.Affordable = false;
                Assert(!order.Apply(actual, Affordable), "None affordable should preserve original order");
                // A game/mod list rebuild is a new baseline, including removals and additions.
                actual.Reverse();
                if (size > 0) actual.RemoveAt(0);
                actual.Add(new Entry { Id = -1, Affordable = true });
                List<Entry> rebuilt = new List<Entry>(actual);
                order.Capture(actual);
                order.Apply(actual, Affordable);
                Assert(actual[0].Id == -1, "Rebuilt availability was not applied");
                order.Apply(actual, null);
                for (int i = 0; i < rebuilt.Count; ++i) Assert(ReferenceEquals(actual[i], rebuilt[i]), "Rebuilt baseline lost");
            }
            List<Entry> large = new List<Entry>();
            for (int i = 0; i < 1000; ++i) large.Add(new Entry { Id = i, Affordable = i % 2 == 0 });
            StableOrder<Entry> benchmark = new StableOrder<Entry>();
            benchmark.Capture(large);
            benchmark.Apply(large, Affordable);
            Stopwatch timer = Stopwatch.StartNew();
            for (int i = 0; i < 10000; ++i) benchmark.Apply(large, Affordable);
            timer.Stop();
            Console.WriteLine("PASS: " + assertions + " assertions; 10,000 partitions of 1,000 entries: " + timer.ElapsedMilliseconds + " ms.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
