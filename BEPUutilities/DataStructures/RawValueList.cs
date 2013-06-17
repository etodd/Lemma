using System;

namespace BEPUutilities.DataStructures
{
    //THIS SHOULD ONLY BE USED ON VALUE TYPES WHICH HAVE NO REFERENCES TO HEAP OBJECTS.
    ///<summary>
    /// No-frills list used for value types that contain no reference types.
    ///</summary>
    ///<typeparam name="T">Type of the elements in the list.</typeparam>
    public class RawValueList<T> where T : struct
    {
        ///<summary>
        /// Directly accessible array of elements in the list.
        /// Be careful about which operations are applied to the array;
        /// if in doubt, use the regular access methods.
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
        public RawValueList()
        {
            Elements = new T[4];
        }
        ///<summary>
        /// Constructs an empty list.
        ///</summary>
        ///<param name="initialCapacity">Initial capacity of the list.</param>
        ///<exception cref="ArgumentException">Thrown when the initial capcity is less than or equal to zero.</exception>
        public RawValueList(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException("Initial capacity must be positive.");
            Elements = new T[initialCapacity];
        }


        ///<summary>
        /// Removes an element from the list.
        ///</summary>
        ///<param name="index">Index of the element to remove.</param>
        ///<exception cref="ArgumentOutOfRangeException">Thrown when the index is not present in the list.</exception>
        public void RemoveAt(int index)
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

        ///<summary>
        /// Adds an element to the list.
        ///</summary>
        ///<param name="item">Item to add.</param>
        public void Add(ref T item)
        {
            if (Count == Elements.Length)
            {
                Capacity = Elements.Length * 2;
            }
            Elements[Count++] = item;

        }

        ///<summary>
        /// Clears the list of all elements.
        ///</summary>
        public void Clear()
        {
            Count = 0;
        }

        ///<summary>
        /// Removes an element from the list.
        ///</summary>
        ///<param name="item">Item to remove.</param>
        ///<returns>Whether or not the item was present in the list.</returns>
        public bool Remove(ref T item)
        {
            int index = IndexOf(ref item);
            if (index == -1)
                return false;
            RemoveAt(index);
            return true;
        }

        ///<summary>
        /// Gets the index of an element in the list.
        ///</summary>
        ///<param name="item">Item to search for.</param>
        ///<returns>Index of the searched element.</returns>
        public int IndexOf(ref T item)
        {
            return Array.IndexOf(Elements, item);
        }

    }
}
