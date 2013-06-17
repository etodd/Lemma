using BEPUutilities.DataStructures;
using BEPUutilities.ResourceManagement;

namespace BEPUphysics.BroadPhaseSystems.SortAndSweep
{
    internal class SortedGrid2DSet
    {
        //TODO: The cell set is the number one reason why Grid2DSortAndSweep fails in corner cases.
        //One option:
        //Instead of trying to maintain a sorted set, stick to a dictionary + RawList combo.
        //The update phase can add active cell-object pairs to a raw list.  Could do bottom-up recreation too, though contention might be an issue.
        //Another option: Some other parallel-enumerable set, possibly with tricky hashing.

        internal RawList<GridCell2D> cells = new RawList<GridCell2D>();
        UnsafeResourcePool<GridCell2D> cellPool = new UnsafeResourcePool<GridCell2D>();

        internal int count;

        internal bool TryGetIndex(ref Int2 cellIndex, out int index, out int sortingHash)
        {
            sortingHash = cellIndex.GetSortingHash();
            int minIndex = 0; //inclusive
            int maxIndex = count; //exclusive
            index = 0;
            while (maxIndex - minIndex > 0) //If the testing interval has a length of zero, we've done as much as we can.
            {
                index = (maxIndex + minIndex) / 2;
                if (cells.Elements[index].sortingHash > sortingHash)
                    maxIndex = index;
                else if (cells.Elements[index].sortingHash < sortingHash)
                    minIndex = ++index;
                else
                {
                    //Found an equal sorting hash!
                    //The hash can collide, and we cannot add an entry to 
                    //an incorrect index.  It would break the 'cell responsibility' 
                    //used by the cell update process to avoid duplicate overlaps.
                    //So, check if the index we found is ACTUALLY correct.
                    if (cells.Elements[index].cellIndex.Y == cellIndex.Y && cells.Elements[index].cellIndex.Z == cellIndex.Z)
                    {
                        return true;
                    }
                    //If it was not the correct index, let it continue searching.
                }

            }
            return false;
        }

        internal bool TryGetCell(ref Int2 cellIndex, out GridCell2D cell)
        {
            int index;
            int sortingHash;
            if (TryGetIndex(ref cellIndex, out index, out sortingHash))
            {
                cell = cells.Elements[index];
                return true;
            }
            cell = null;
            return false;
        }

        internal void Add(ref Int2 index, Grid2DEntry entry)
        {
            int cellIndex;
            int sortingHash;
            if (TryGetIndex(ref index, out cellIndex, out sortingHash))
            {
                cells.Elements[cellIndex].Add(entry);
                return;
            }
            var cell = cellPool.Take();
            cell.Initialize(ref index, sortingHash);
            cell.Add(entry);
            cells.Insert(cellIndex, cell);
            count++;

            ////Take an index.  See if it's taken in the set.
            ////If it's already there, then add the entry to the cell.
            ////If it's not already there, create a new cell and add the entry to the cell and insert it at the index located.

            //int sortingHash = index.GetSortingHash();
            //int minIndex = 0; //inclusive
            //int maxIndex = count; //exclusive
            //int i = 0;
            //while (maxIndex - minIndex > 0) //If the testing interval has a length of zero, we've done as much as we can.
            //{
            //    i = (maxIndex + minIndex) / 2;
            //    if (cells.Elements[i].sortingHash > sortingHash)
            //        maxIndex = i;
            //    else if (cells.Elements[i].sortingHash < sortingHash)
            //        minIndex = ++i;
            //    else
            //    {
            //        //Found an equal sorting hash!
            //        //The hash can collide, and we cannot add an entry to 
            //        //an incorrect index.  It would break the 'cell responsibility' 
            //        //used by the cell update process to avoid duplicate overlaps.
            //        //So, check if the index we found is ACTUALLY correct.
            //        if (cells.Elements[i].cellIndex.Y == index.Y && cells.Elements[i].cellIndex.Z == index.Z)
            //        {
            //            cells.Elements[i].Add(entry);
            //            return;
            //        }
            //        //If it was not the correct index, let it continue searching.
            //    }

            //}
            //var cell = cellPool.Take();
            //cell.Initialize(ref index, sortingHash);
            //cell.Add(entry);
            //cells.Insert(i, cell);
            //count++;

        }

        internal void Remove(ref Int2 index, Grid2DEntry entry)
        {
            int cellIndex;
            int sortingHash;
            if (TryGetIndex(ref index, out cellIndex, out sortingHash))
            {
                cells.Elements[cellIndex].Remove(entry);
                if (cells.Elements[cellIndex].entries.Count == 0)
                {
                    //The cell is now empty.  Give it back to the pool.
                    var toRemove = cells.Elements[cellIndex];
                    //There's no cleanup to do on the grid cell.
                    //Its list is empty, and the rest is just value types.
                    cells.RemoveAt(cellIndex);
                    cellPool.GiveBack(toRemove);
                    count--;
                }
            }


            //int sortingHash = index.GetSortingHash();
            //int minIndex = 0; //inclusive
            //int maxIndex = count; //exclusive
            //int i = 0;
            //while (maxIndex - minIndex > 0) //If the testing interval has a length of zero, we've done as much as we can.
            //{
            //    i = (maxIndex + minIndex) / 2;
            //    if (cells.Elements[i].sortingHash > sortingHash)
            //        maxIndex = i;
            //    else if (cells.Elements[i].sortingHash < sortingHash)
            //        minIndex = ++i;
            //    else
            //    {
            //        //Found an equal sorting hash!
            //        //The hash can collide, and we cannot add an entry to 
            //        //an incorrect index.  It would break the 'cell responsibility' 
            //        //used by the cell update process to avoid duplicate overlaps.
            //        //So, check if the index we found is ACTUALLY correct.
            //        if (cells.Elements[i].cellIndex.Y == index.Y && cells.Elements[i].cellIndex.Z == index.Z)
            //        {
            //            cells.Elements[i].Remove(entry);
            //            if (cells.Elements[i].entries.count == 0)
            //            {
            //                //The cell is now empty.  Give it back to the pool.
            //                var toRemove = cells.Elements[i];
            //                //There's no cleanup to do on the grid cell.
            //                //Its list is empty, and the rest is just value types.
            //                cells.RemoveAt(i);
            //                cellPool.GiveBack(toRemove);
            //                count--;
            //            }
            //            return;
            //        }
            //        //If it was not the correct index, let it continue searching.
            //    }

            //}
            ////Getting here should be impossible.

        }



    }
}
