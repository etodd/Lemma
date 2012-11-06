using System;
using System.Collections.Generic;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Convenience collection of contacts and their associated data.
    ///</summary>
    public class ContactCollection : IList<ContactInformation>
    {

        ///<summary>
        /// Enumerator for the contact collection.
        ///</summary>
        public struct Enumerator : IEnumerator<ContactInformation>
        {
            ContactCollection contactCollection;
            int index;
            int count;

            internal Enumerator(ContactCollection contactCollection)
            {
                this.contactCollection = contactCollection;
                index = -1;
                count = contactCollection.Count;
            }


            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public ContactInformation Current
            {
                get { return contactCollection[index]; }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                return ++index < count;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                index = -1;
                count = contactCollection.Count;
            }
        }

        CollidablePairHandler pair;

        internal ContactCollection(CollidablePairHandler pair)
        {
            this.pair = pair;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count
        {
            get
            {
                return pair.ContactCount;
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        /// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
        public ContactInformation this[int index]
        {
            get
            {
                ContactInformation toReturn;
                pair.GetContactInformation(index, out toReturn);
                return toReturn;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        IEnumerator<ContactInformation> IEnumerable<ContactInformation>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        ///<summary>
        /// Gets an enumerator for the collection.
        ///</summary>
        ///<returns>Enumerator for the contact collection.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Contains(ContactInformation item)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (this[i].Contact == item.Contact)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(ContactInformation[] array, int arrayIndex)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        public int IndexOf(ContactInformation item)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (this[i].Contact == item.Contact)
                    return i;
            }
            return -1;
        }

        bool ICollection<ContactInformation>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<ContactInformation>.Remove(ContactInformation item)
        {
            throw new NotSupportedException();
        }

        void ICollection<ContactInformation>.Add(ContactInformation item)
        {
            throw new NotSupportedException();
        }

        void ICollection<ContactInformation>.Clear()
        {
            throw new NotSupportedException();
        }

        void IList<ContactInformation>.Insert(int index, ContactInformation item)
        {
            throw new NotSupportedException();
        }

        void IList<ContactInformation>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }







    }
}
