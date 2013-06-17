using System;
using System.Collections.Generic;

namespace BEPUphysics.BroadPhaseEntries
{
    ///<summary>
    /// List of collidable objects overlapping another collidable.
    ///</summary>
    public struct CollidableCollection : IList<Collidable>
    {

        ///<summary>
        /// Enumerator for the CollidableCollection.
        ///</summary>
        public struct Enumerator : IEnumerator<Collidable>
        {
            CollidableCollection collection;
            int index;
            ///<summary>
            /// Constructs an enumerator.
            ///</summary>
            ///<param name="collection">Collection to which the enumerator belongs.</param>
            public Enumerator(CollidableCollection collection)
            {
                this.collection = collection;
                index = -1;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public Collidable Current
            {
                get { return collection[index]; }
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
            /// <exception>The collection was modified after the enumerator was created.
            ///   <cref>T:System.InvalidOperationException</cref>
            /// </exception><filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                return ++index < collection.Count;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception>The collection was modified after the enumerator was created.
            ///   <cref>T:System.InvalidOperationException</cref>
            /// </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                index = -1;
            }
        }

        ///<summary>
        /// Constructs a new CollidableCollection.
        ///</summary>
        ///<param name="owner">The collidable to which the collection belongs.</param>
        public CollidableCollection(Collidable owner)
        {
            this.owner = owner;
        }

        internal Collidable owner;


        ///<summary>
        /// Gets an enumerator which can be used to enumerate over the list.
        ///</summary>
        ///<returns>Enumerator for the collection.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<Collidable> IEnumerable<Collidable>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }


        /// <summary>
        /// Determines the index of a specific item in the <see>
        ///                                                  <cref>T:System.Collections.Generic.IList`1</cref>
        ///                                                </see> .
        /// </summary>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">The object to locate in the <see>
        ///                                                  <cref>T:System.Collections.Generic.IList`1</cref>
        ///                                                </see> .</param>
        public int IndexOf(Collidable item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (item == this[i])
                    return i;
            }
            return -1;
        }


        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        /// <param name="index">The zero-based index of the element to get or set.</param><exception>
        ///                                                                                 <cref>T:System.ArgumentOutOfRangeException</cref>
        ///                                                                                 <paramref name="index"/> is not a valid index in the <see>
        ///                                                                                                                                        <cref>T:System.Collections.Generic.IList`1</cref>
        ///                                                                                                                                      </see>
        ///                                                                                 .</exception><exception>The property is set and the
        ///                                                                                                <cref>T:System.NotSupportedException</cref>
        ///                                                                                                <see>
        ///                                                                                                  <cref>T:System.Collections.Generic.IList`1</cref>
        ///                                                                                                </see>
        ///                                                                                                is read-only.</exception>
        public Collidable this[int index]
        {
            get
            {
                //It's guaranteed to be a CollisionInformation, because it's a member of a CollidablePairHandler.
                return (Collidable)(owner.pairs[index].broadPhaseOverlap.entryA == owner ? owner.pairs[index].broadPhaseOverlap.entryB : owner.pairs[index].broadPhaseOverlap.entryA);
            }
            set
            {
                throw new NotSupportedException();
            }
        }


        /// <summary>
        /// Determines whether the <see>
        ///                          <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                        </see> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see>
        ///                                                   <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                                                 </see> ; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see>
        ///                                                  <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                                                </see> .</param>
        public bool Contains(Collidable item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (item == this[i])
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Copies the elements of the <see>
        ///                              <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                            </see> to an <see>
        ///                                           <cref>T:System.Array</cref>
        ///                                         </see> , starting at a particular <see>
        ///                                                                             <cref>T:System.Array</cref>
        ///                                                                           </see> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see>
        ///                                           <cref>T:System.Array</cref>
        ///                                         </see> that is the destination of the elements copied from <see>
        ///                                                                                                      <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                                                                                                    </see> . The <see>
        ///                                                                                                                   <cref>T:System.Array</cref>
        ///                                                                                                                 </see> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(Collidable[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see>
        ///                                                <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                                              </see> .
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see>
        ///                                           <cref>T:System.Collections.Generic.ICollection`1</cref>
        ///                                         </see> .
        /// </returns>
        public int Count
        {
            get { return owner.pairs.Count; }
        }

        bool ICollection<Collidable>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<Collidable>.Remove(Collidable item)
        {
            throw new NotSupportedException();
        }

        void ICollection<Collidable>.Add(Collidable item)
        {
            throw new NotSupportedException();
        }

        void ICollection<Collidable>.Clear()
        {
            throw new NotSupportedException();
        }

        void IList<Collidable>.Insert(int index, Collidable item)
        {
            throw new NotSupportedException();
        }

        void IList<Collidable>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}
