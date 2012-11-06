using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.BroadPhaseEntries;

namespace BEPUphysics.BroadPhaseSystems
{
    public class BruteForce : BroadPhase
    {
        public List<BroadPhaseEntry> entries = new List<BroadPhaseEntry>();

        public override void Add(BroadPhaseEntry entry)
        {
            entries.Add(entry);
        }

        public override void Remove(BroadPhaseEntry entry)
        {
            entries.Remove(entry);
        }

        protected override void UpdateMultithreaded()
        {
            UpdateSingleThreaded();
        }

        protected override void UpdateSingleThreaded()
        {
            Overlaps.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    if (entries[i].boundingBox.Intersects(entries[j].boundingBox))
                        base.TryToAddOverlap(entries[i], entries[j]);
                }
            }
        }
    }
}
