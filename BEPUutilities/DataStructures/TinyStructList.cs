using System;

namespace BEPUutilities.DataStructures
{
    /// <summary>
    /// Special datatype used for heapless lists without unsafe/stackalloc.
    /// Since reference types would require heap-side allocation and
    /// do not match well with this structure's ref-parameter based access,
    /// only structs are allowed.
    /// Stores a maximum of 8 entries.
    /// </summary>
    /// <typeparam name="T">Struct type to use.</typeparam>
    public struct TinyStructList<T> where T : struct, IEquatable<T>
    {
        private T entry1;
        private T entry2;
        private T entry3;
        private T entry4;

        private T entry5;
        private T entry6;
        private T entry7;
        private T entry8;

        internal int count;

        /// <summary>
        /// Gets the current number of elements in the list.
        /// </summary>
        public int Count
        {
            get { return count; }
        }

        /// <summary>
        /// Creates a string representation of the list.
        /// </summary>
        /// <returns>String representation of the list.</returns>
        public override string ToString()
        {
            return "TinyStructList<" + typeof (T) + ">, Count: " + count;
        }

        /// <summary>
        /// Tries to add an element to the list.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <returns>Whether or not the item could be added.
        /// Will return false when the list is full.</returns>
        public bool Add(ref T item)
        {
            switch (count)
            {
                case 0:
                    entry1 = item;
                    break;
                case 1:
                    entry2 = item;
                    break;
                case 2:
                    entry3 = item;
                    break;
                case 3:
                    entry4 = item;
                    break;
                case 4:
                    entry5 = item;
                    break;
                case 5:
                    entry6 = item;
                    break;
                case 6:
                    entry7 = item;
                    break;
                case 7:
                    entry8 = item;
                    break;
                default:
                    return false;
            }
            count++;
            return true;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            //Everything is a struct in this kind of list, so not much work to do!
            count = 0;
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="index">Index to retrieve.</param>
        /// <param name="item">Retrieved item.</param>
        /// <returns>Whether or not the index was valid.</returns>
        public bool Get(int index, out T item)
        {
            if (index > count - 1 || index < 0)
            {
                item = default(T);
                return false;
            }
            switch (index)
            {
                case 0:
                    item = entry1;
                    return true;
                case 1:
                    item = entry2;
                    return true;
                case 2:
                    item = entry3;
                    return true;
                case 3:
                    item = entry4;
                    return true;
                case 4:
                    item = entry5;
                    return true;
                case 5:
                    item = entry6;
                    return true;
                case 6:
                    item = entry7;
                    return true;
                case 7:
                    item = entry8;
                    return true;
                default:
                    //Curious!
                    item = default(T);
                    return false;
            }
        }

        /// <summary>
        /// Gets the index of the item in the list, if it is present.
        /// </summary>
        /// <param name="item">Item to look for.</param>
        /// <returns>Index of the item, if present.  -1 otherwise.</returns>
        public int IndexOf(ref T item)
        {
            //This isn't a super fast operation.
            if (entry1.Equals(item))
                return 0;
            if (entry2.Equals(item))
                return 1;
            if (entry3.Equals(item))
                return 2;
            if (entry4.Equals(item))
                return 3;
            if (entry5.Equals(item))
                return 4;
            if (entry6.Equals(item))
                return 5;
            if (entry7.Equals(item))
                return 6;
            if (entry8.Equals(item))
                return 7;
            return -1;
        }

        /// <summary>
        /// Tries to remove an element from the list.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>Whether or not the item existed in the list.</returns>
        public bool Remove(ref T item)
        {
            //Identity-based removes aren't a super high priority feature, so can be a little slower.
            int index = IndexOf(ref item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">Index of the element to remove.</param>
        /// <returns>Whether or not the item could be removed.
        /// Returns false if the index is out of bounds.</returns>
        public bool RemoveAt(int index)
        {
            if (index > count - 1 || index < 0)
                return false;
            switch (index)
            {
                case 0:
                    entry1 = entry2;
                    entry2 = entry3;
                    entry3 = entry4;
                    entry4 = entry5;
                    entry5 = entry6;
                    entry6 = entry7;
                    entry7 = entry8;
                    break;
                case 1:
                    entry2 = entry3;
                    entry3 = entry4;
                    entry4 = entry5;
                    entry5 = entry6;
                    entry6 = entry7;
                    entry7 = entry8;
                    break;
                case 2:
                    entry3 = entry4;
                    entry4 = entry5;
                    entry5 = entry6;
                    entry6 = entry7;
                    entry7 = entry8;
                    break;
                case 3:
                    entry4 = entry5;
                    entry5 = entry6;
                    entry6 = entry7;
                    entry7 = entry8;
                    break;
                case 4:
                    entry5 = entry6;
                    entry6 = entry7;
                    entry7 = entry8;
                    break;
                case 5:
                    entry6 = entry7;
                    entry7 = entry8;
                    break;
                case 6:
                    entry7 = entry8;
                    break;
            }
            count--;
            return true;
        }

        /// <summary>
        /// Tries to add an element to the list.
        /// </summary>
        /// <param name="index">Index to replace.</param>
        /// <param name="item">Item to add.</param>
        /// <returns>Whether or not the item could be replaced.
        /// Returns false if the index is invalid.</returns>
        public bool Replace(int index, ref T item)
        {
            if (index > count - 1 || index < 0)
            {
                return false;
            }
            switch (index)
            {
                case 0:
                    entry1 = item;
                    break;
                case 1:
                    entry2 = item;
                    break;
                case 2:
                    entry3 = item;
                    break;
                case 3:
                    entry4 = item;
                    break;
                case 4:
                    entry5 = item;
                    break;
                case 5:
                    entry6 = item;
                    break;
                case 6:
                    entry7 = item;
                    break;
                case 7:
                    entry8 = item;
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}