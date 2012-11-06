using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.DataStructures;
using Microsoft.Xna.Framework;
using BEPUphysics.Threading;

namespace BEPUphysics.BroadPhaseSystems.SortAndSweep
{
    /// <summary>
    /// Simple and standard implementation of the one-axis sort and sweep (sweep and prune) algorithm.
    /// </summary>
    /// <remarks>
    /// In small scenarios, it can be the quickest option.  It uses very little memory.
    /// However, it tends to scale poorly relative to other options and can slow down significantly when entries cluster along the axis.
    /// Additionally, it supports no queries at all.
    /// </remarks>
    public class SortAndSweep1D : BroadPhase
    {
        /// <summary>
        /// Constructs a new sort and sweep broad phase.
        /// </summary>
        /// <param name="threadManager">Thread manager to use in the broad phase.</param>
        public SortAndSweep1D(IThreadManager threadManager)
            : base(threadManager)
        {
            sweepSegment = Sweep;
            backbuffer = new RawList<BroadPhaseEntry>();
        }

        /// <summary>
        /// Constructs a new sort and sweep broad phase.
        /// </summary>
        public SortAndSweep1D()
        {

            sweepSegment = Sweep;
            backbuffer = new RawList<BroadPhaseEntry>();
        }


        RawList<BroadPhaseEntry> entries = new RawList<BroadPhaseEntry>();
        /// <summary>
        /// Adds an entry to the broad phase.
        /// </summary>
        /// <param name="entry">Entry to add.</param>
        public override void Add(BroadPhaseEntry entry)
        {
            base.Add(entry);
            //Entities do not set up their own bounding box before getting stuck in here.  If they're all zeroed out, the tree will be horrible.
            Vector3 offset;
            Vector3.Subtract(ref entry.boundingBox.Max, ref entry.boundingBox.Min, out offset);
            if (offset.X * offset.Y * offset.Z == 0)
                entry.UpdateBoundingBox();
            //binary search for the approximately correct location.  This helps prevent large first-frame sort times.
            int minIndex = 0; //inclusive
            int maxIndex = entries.count; //exclusive
            int index = 0;
            while (maxIndex - minIndex > 0)
            {
                index = (maxIndex + minIndex) / 2;
                if (entries.Elements[index].boundingBox.Min.X > entry.boundingBox.Min.X)
                    maxIndex = index;
                else if (entries.Elements[index].boundingBox.Min.X < entry.boundingBox.Min.X)
                    minIndex = ++index;
                else
                    break; //Found an equal value!

            }
            entries.Insert(index, entry);
        }

        /// <summary>
        /// Removes an entry from the broad phase.
        /// </summary>
        /// <param name="entry">Entry to remove.</param>
        public override void Remove(BroadPhaseEntry entry)
        {
            base.Remove(entry);
            entries.Remove(entry);
        }

        Action<int> sweepSegment;
        protected override void UpdateMultithreaded()
        {
            if (backbuffer.count != entries.count)
            {
                backbuffer.Capacity = entries.Capacity;
                backbuffer.count = entries.count;
            }
            Overlaps.Clear();
            //Sort along x axis using insertion sort; the list will be nearly sorted, so very few swaps are necessary.
            for (int i = 1; i < entries.count; i++)
            {
                var entry = entries.Elements[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    if (entry.boundingBox.Min.X < entries.Elements[j].boundingBox.Min.X)
                    {
                        entries.Elements[j + 1] = entries.Elements[j];
                        entries.Elements[j] = entry;
                    }
                    else
                        break;
                }

            }

            //TODO: Multithreaded sorting could help in some large cases.
            //The overhead involved in this implementation is way too high for reasonable object counts.
            //for (int i = 0; i < sortSegmentCount; i++)
            //    SortSection(i);

            ////MergeSections(0, 1);
            ////MergeSections(2, 3);
            ////MergeSections(0, 2);
            ////MergeSections(1, 3);

            //MergeSections(0, 1);
            //MergeSections(2, 3);
            //MergeSections(4, 5);
            //MergeSections(6, 7);

            //MergeSections(0, 2);
            //MergeSections(1, 3);
            //MergeSections(4, 6);
            //MergeSections(5, 7);

            //MergeSections(0, 4);
            //MergeSections(1, 5);
            //MergeSections(2, 6);
            //MergeSections(3, 7);

            //var temp = backbuffer;
            //backbuffer = entries;
            //entries = temp;

            ThreadManager.ForLoop(0, sweepSegmentCount, sweepSegment);
        }

        protected override void UpdateSingleThreaded()
        {
            Overlaps.Clear();
            //Sort along x axis using insertion sort; the list will be nearly sorted, so very few swaps are necessary.
            for (int i = 1; i < entries.count; i++)
            {
                var entry = entries.Elements[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    if (entry.boundingBox.Min.X < entries.Elements[j].boundingBox.Min.X)
                    {
                        entries.Elements[j + 1] = entries.Elements[j];
                        entries.Elements[j] = entry;
                    }
                    else
                        break;
                }

            }
            //Sweep the list looking for overlaps.
            for (int i = 0; i < entries.count; i++)
            {
                BoundingBox a = entries.Elements[i].boundingBox;
                for (int j = i + 1; j < entries.count && a.Max.X >= entries.Elements[j].boundingBox.Min.X; j++)
                {
                    if (!(a.Min.Y > entries.Elements[j].boundingBox.Max.Y || a.Max.Y < entries.Elements[j].boundingBox.Min.Y ||
                          a.Min.Z > entries.Elements[j].boundingBox.Max.Z || a.Max.Z < entries.Elements[j].boundingBox.Min.Z))
                    {
                        TryToAddOverlap(entries.Elements[i], entries.Elements[j]);
                    }
                }
            }
        }


        //TODO: It is possible to distribute things a bit better.  Instead of lumping all of the remainder into the final, put and 

        int sweepSegmentCount = 32;
        void Sweep(int segment)
        {
            int intervalLength = entries.count / sweepSegmentCount;
            int end;
            if (segment == sweepSegmentCount - 1)
                end = entries.count;
            else
                end = intervalLength * (segment + 1);
            for (int i = intervalLength * segment; i < end; i++)
            {
                BoundingBox a = entries.Elements[i].boundingBox;
                for (int j = i + 1; j < entries.count && a.Max.X >= entries.Elements[j].boundingBox.Min.X; j++)
                {
                    if (!(a.Min.Y > entries.Elements[j].boundingBox.Max.Y || a.Max.Y < entries.Elements[j].boundingBox.Min.Y ||
                          a.Min.Z > entries.Elements[j].boundingBox.Max.Z || a.Max.Z < entries.Elements[j].boundingBox.Min.Z))
                    {
                        TryToAddOverlap(entries.Elements[i], entries.Elements[j]);
                    }
                }
            }
        }

        int sortSegmentCount = 4;
        void SortSection(int section)
        {
            int intervalLength = entries.count / sortSegmentCount;
            int start = section * intervalLength;
            int end;
            if (section == sortSegmentCount - 1)
                end = entries.count;
            else
                end = intervalLength * (section + 1);

            for (int i = start + 1; i < end; i++)
            {
                var entry = entries.Elements[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    if (entry.boundingBox.Min.X < entries.Elements[j].boundingBox.Min.X)
                    {
                        entries.Elements[j + 1] = entries.Elements[j];
                        entries.Elements[j] = entry;
                    }
                    else
                        break;
                }

            }
        }

        RawList<BroadPhaseEntry> backbuffer;
        void MergeSections(int a, int b)
        {
            int intervalLength = entries.count / sortSegmentCount;
            //'a' is known to be less than b, which means it cannot be the last section.
            int aStart = intervalLength * a;
            int aEnd = intervalLength * (a + 1);
            int bStart = intervalLength * b;
            int bEnd;
            int length;
            if (b == sortSegmentCount - 1)
            {
                bEnd = entries.count;
                length = intervalLength + entries.count - bStart;
            }
            else
            {
                bEnd = intervalLength * (b + 1);
                length = intervalLength * 2;
            }

            int aIndex = aStart, bIndex = bStart;
            int i = 0;
            while (i < length)
            {
                //Compute the location in the buffer array to put the minimum element.
                int bufferIndex;
                if (i >= intervalLength) //a length is intervalLength.
                    bufferIndex = bStart + i - intervalLength;
                else
                    bufferIndex = aStart + i;

                if (aIndex < aEnd && bIndex < bEnd)
                {
                    //Compare the element at a to the one at b.
                    if (entries.Elements[aIndex].boundingBox.Min.X < entries.Elements[bIndex].boundingBox.Min.X)
                    {
                        //a was the minimum element.  Put it into the buffer and increment the considered a index.
                        backbuffer.Elements[bufferIndex] = entries.Elements[aIndex++];
                    }
                    else
                    {
                        //b was the minimum element.  Put it into the buffer and increment the considered b index.
                        backbuffer.Elements[bufferIndex] = entries.Elements[bIndex++];
                    }
                }
                else if (aIndex < aEnd)
                {
                    //B is at the max, so just use a.
                    backbuffer.Elements[bufferIndex] = entries.Elements[aIndex++];
                }
                else
                {
                    //A is at the max, so just use b.
                    backbuffer.Elements[bufferIndex] = entries.Elements[bIndex++];
                }
                i++;
            }
        }
    }
}
