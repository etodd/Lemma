using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.DataStructures;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace BEPUphysics.BroadPhaseSystems.SortAndSweep
{
    class GridCell2D
    {
        internal RawList<Grid2DEntry> entries = new RawList<Grid2DEntry>();
        internal Int2 cellIndex;
        internal int sortingHash;

        internal void Initialize(ref Int2 cellIndex, int hash)
        {
            this.cellIndex = cellIndex;
            sortingHash = hash;
        }

        internal int GetIndex(float x)
        {
            int minIndex = 0; //inclusive
            int maxIndex = entries.count; //exclusive
            int index = 0;
            while (maxIndex - minIndex > 0)
            {
                index = (maxIndex + minIndex) / 2;
                if (entries.Elements[index].item.boundingBox.Min.X > x)
                    maxIndex = index;
                else if (entries.Elements[index].item.boundingBox.Min.X < x)
                    minIndex = ++index;
                else
                    break; //Found an equal value!

            }
            return index;
        }

        internal void Add(Grid2DEntry entry)
        {
            //binary search for the approximately correct location.  This helps prevent large first-frame sort times.
            entries.Insert(GetIndex(entry.item.boundingBox.Min.X), entry);
        }

        internal void Remove(Grid2DEntry entry)
        {
            entries.Remove(entry);
        }

        internal void UpdateOverlaps(Grid2DSortAndSweep owner)
        {
            //Sort along x axis using insertion sort; the list will be nearly sorted, so very few swaps are necessary.
            for (int i = 1; i < entries.count; i++)
            {
                var entry = entries.Elements[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    if (entry.item.boundingBox.Min.X < entries.Elements[j].item.boundingBox.Min.X)
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
                Grid2DEntry a = entries.Elements[i];
                Grid2DEntry b;
                //TODO: Microoptimize
                for (int j = i + 1; j < entries.count && a.item.boundingBox.Max.X >= (b = entries.Elements[j]).item.boundingBox.Min.X; j++)
                {
                    if (!(a.item.boundingBox.Min.Y > b.item.boundingBox.Max.Y || a.item.boundingBox.Max.Y < b.item.boundingBox.Min.Y ||
                          a.item.boundingBox.Min.Z > b.item.boundingBox.Max.Z || a.item.boundingBox.Max.Z < b.item.boundingBox.Min.Z))
                    {
                        //Now we know this pair is overlapping, but we do not know if this overlap is already added.
                        //Rather than use a hashset or other heavy structure to check, rely on the rules of the grid.

                        //It's possible to avoid adding pairs entirely unless we are the designated 'responsible' cell.
                        //All other cells will defer to the cell 'responsible' for a pair.
                        //A simple rule for determining the cell which is responsible is to choose the cell which is the 
                        //smallest index in the shared cells.  So first, compute that cell's index.

                        Int2 minimumSharedIndex = a.previousMin;

                        if (minimumSharedIndex.Y < b.previousMin.Y)
                            minimumSharedIndex.Y = b.previousMin.Y;
                        if (minimumSharedIndex.Y > b.previousMax.Y)
                            minimumSharedIndex.Y = b.previousMax.Y;

                        if (minimumSharedIndex.Z < b.previousMin.Z)
                            minimumSharedIndex.Z = b.previousMin.Z;
                        if (minimumSharedIndex.Z > b.previousMax.Z)
                            minimumSharedIndex.Z = b.previousMax.Z;

                        //Is our cell the minimum cell?
                        if (minimumSharedIndex.Y == cellIndex.Y && minimumSharedIndex.Z == cellIndex.Z)
                            owner.TryToAddOverlap(a.item, b.item);





                    }
                }
            }
        }


        public override string ToString()
        {
            return "{" + cellIndex.Y + ", " + cellIndex.Z + "}: " + entries.count;
        }

    }
}
