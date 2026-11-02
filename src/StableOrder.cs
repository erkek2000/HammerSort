using System;
using System.Collections.Generic;

namespace HammerSort
{
    // Always partition the captured game order, never yesterday's sorted order.
    internal sealed class StableOrder<T> where T : class
    {
        private readonly List<T> baseline = new List<T>();
        private readonly List<T> pending = new List<T>();
        private readonly List<T> ordered = new List<T>();

        internal void Capture(List<T> source)
        {
            baseline.Clear();
            baseline.AddRange(source);
            if (pending.Capacity < source.Count) pending.Capacity = source.Count;
            if (ordered.Capacity < source.Count) ordered.Capacity = source.Count;
        }

        internal bool Apply(List<T> destination, Predicate<T> preferred)
        {
            ordered.Clear();
            pending.Clear();
            for (int i = 0; i < baseline.Count; ++i)
            {
                T entry = baseline[i];
                if (preferred == null || preferred(entry)) ordered.Add(entry);
                else pending.Add(entry);
            }
            ordered.AddRange(pending);
            bool changed = destination.Count != ordered.Count;
            if (changed)
            {
                destination.Clear();
                destination.AddRange(ordered);
                return true;
            }
            for (int i = 0; i < ordered.Count; ++i)
            {
                if (ReferenceEquals(destination[i], ordered[i])) continue;
                destination[i] = ordered[i];
                changed = true;
            }
            return changed;
        }
    }
}
