using System;
using System.Collections.Generic;

namespace BEPUutilities.DataStructures
{
    ///<summary>
    /// No-frills list that wraps an accessible array.
    ///</summary>
    ///<typeparam name="T">Type of elements contained by the list.</typeparam>
    public class RawList<T> : IList<T>
    {
        ///<summary>
        /// Direct access to the elements owned by the raw list.
        /// Be careful about the operations performed on this list;
        /// use the normal access methods if in doubt.
        ///</summary>
        public T[] Elements;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// Can also be set; setting the count is a direct change to the count integer and does not change the state of the array.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count { get; set; }

        ///<summary>
        /// Constructs an empty list.
        ///</summary>
        public RawList()
        {
            Elements = new T[4];
        }
        ///<summary>
        /// Constructs an empty list.
        ///</summary>
        ///<param name="initialCapacity">Initial capacity to allocate for the list.</param>
        ///<exception cref="ArgumentException">Thrown when the initial capacity is zero or negative.</exception>
        public RawList(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException("Initial capacity must be positive.");
            Elements = new T[initialCapacity];
        }

        ///<summary>
        /// Constructs a raw list from another list.
        ///</summary>
        ///<param name="elements">List to copy.</param>
        public RawList(IList<T> elements)
            : this(Math.Max(elements.Count, 4))
        {
            elements.CopyTo(Elements, 0);
            Count = elements.Count;
        }

        /// <summary>
        /// Removes an element from the list.
        /// </summary>
        /// <param name="index">Index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Count--;
            if (index < Count)
                Array.Copy(Elements, index + 1, Elements, index, Count - index);

            Elements[Count] = default(T);
        }

        /// <summary>
        /// Removes an element from the list without maintaining order.
        /// </summary>
        /// <param name="index">Index of the element to remove.</param>
        public void FastRemoveAt(int index)
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Count--;
            if (index < Count)
            {
                Elements[index] = Elements[Count];
            }
            Elements[Count] = default(T);

        }

        ///<summary>
        /// Gets or sets the current size allocated for the list.
        ///</summary>
        public int Capacity
        {
            get
            {
                return Elements.Length;
            }
            set
            {
                T[] newArray = new T[value];
                Array.Copy(Elements, newArray, Count);
                Elements = newArray;
            }
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public void Add(T item)
        {
            if (Count == Elements.Length)
            {
                Capacity = Elements.Length * 2;
            }
            Elements[Count++] = item;

        }

        ///<summary>
        /// Adds a range of elements to the list from another list.
        ///</summary>
        ///<param name="items">Elements to add.</param>
        public void AddRange(RawList<T> items)
        {
            int neededLength = Count + items.Count;
            if (neededLength > Elements.Length)
            {
                int newLength = Elements.Length * 2;
                if (newLength < neededLength)
                    newLength = neededLength;
                Capacity = newLength;
            }
            Array.Copy(items.Elements, 0, Elements, Count, items.Count);
            Count = neededLength;

        }

        ///<summary>
        /// Adds a range of elements to the list from another list.
        ///</summary>
        ///<param name="items">Elements to add.</param>
        public void AddRange(List<T> items)
        {
            int neededLength = Count + items.Count;
            if (neededLength > Elements.Length)
            {
                int newLength = Elements.Length * 2;
                if (newLength < neededLength)
                    newLength = neededLength;
                Capacity = newLength;
            }
            items.CopyTo(0, Elements, Count, items.Count);
            Count = neededLength;

        }

        ///<summary>
        /// Adds a range of elements to the list from another list.
        ///</summary>
        ///<param name="items">Elements to add.</param>
        public void AddRange(IList<T> items)
        {
            int neededLength = Count + items.Count;
            if (neededLength > Elements.Length)
            {
                int newLength = Elements.Length * 2;
                if (newLength < neededLength)
                    newLength = neededLength;
                Capacity = newLength;
            }
            items.CopyTo(Elements, 0);
            Count = neededLength;

        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            Array.Clear(Elements, 0, Count);
            Count = 0;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the collection without maintaining element order.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public bool FastRemove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;
            FastRemoveAt(index);
            return true;
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        public int IndexOf(T item)
        {
            return Array.IndexOf(Elements, item, 0, Count);
        }

        /// <summary>
        /// Copies the elements from the list into an array.
        /// </summary>
        /// <returns>An array containing the elements in the list.</returns>
        public T[] ToArray()
        {
            var toReturn = new T[Count];
            Array.Copy(Elements, toReturn, Count);
            return toReturn;
        }


        #region IList<T> Members


        /// <summary>
        /// Inserts the element at the specified index.
        /// </summary>
        /// <param name="index">Index to insert the item.</param>
        /// <param name="item">Element to insert.</param>
        public void Insert(int index, T item)
        {
            if (index < Count)
            {
                if (Count == Elements.Length)
                {
                    Capacity = Elements.Length * 2;
                }

                Array.Copy(Elements, index, Elements, index + 1, Count - index);
                Elements[index] = item;
                Count++;
            }
            else
                Add(item);
        }
        /// <summary>
        /// Inserts the element at the specified index without maintaining list order.
        /// </summary>
        /// <param name="index">Index to insert the item.</param>
        /// <param name="item">Element to insert.</param>
        public void FastInsert(int index, T item)
        {
            if (index < Count)
            {
                if (Count == Elements.Length)
                {
                    Capacity = Elements.Length * 2;
                }

                Array.Copy(Elements, index, Elements, index + 1, Count - index);
                Elements[Count] = Elements[index];
                Elements[index] = item;
                Count++;
            }
            else
                Add(item);
        }

        /// <summary>
        /// Gets or sets the element of the list at the given index.
        /// </summary>
        /// <param name="index">Index in the list.</param>
        /// <returns>Element at the given index.</returns>
        public T this[int index]
        {
            get
            {
                if (index < Count && index >= 0)
                    return Elements[index];
                else
                    throw new IndexOutOfRangeException("Index is outside of the list's bounds.");
            }
            set
            {
                if (index < Count && index >= 0)
                    Elements[index] = value;
                else
                    throw new IndexOutOfRangeException("Index is outside of the list's bounds.");
            }
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Determines if an item is present in the list.
        /// </summary>
        /// <param name="item">Item to be tested.</param>
        /// <returns>Whether or not the item was contained by the list.</returns>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Copies the list's contents to the array.
        /// </summary>
        /// <param name="array">Array to receive the list's contents.</param>
        /// <param name="arrayIndex">Index in the array to start the dump.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(Elements, 0, array, arrayIndex, Count);
        }


        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region IEnumerable<T> Members

        ///<summary>
        /// Gets an enumerator for the list.
        ///</summary>
        ///<returns>Enumerator for the list.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion



        ///<summary>
        /// Sorts the list.
        ///</summary>
        ///<param name="comparer">Comparer to use to sort the list.</param>
        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(Elements, 0, Count, comparer);
        }


        ///<summary>
        /// Enumerator for the RawList.
        ///</summary>
        public struct Enumerator : IEnumerator<T>
        {
            RawList<T> list;
            int index;
            ///<summary>
            /// Constructs a new enumerator.
            ///</summary>
            ///<param name="list"></param>
            public Enumerator(RawList<T> list)
            {
                index = -1;
                this.list = list;
            }
            public T Current
            {
                get { return list.Elements[index]; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return list.Elements[index]; }
            }

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }


    }
}
