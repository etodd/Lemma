using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.DataStructures;

namespace BEPUphysics.DeactivationManagement
{
    /// <summary>
    /// Read only list containing the simulation island members associated with a simulation island connection.
    /// </summary>
    public struct SimulationIslandMemberList : IList<SimulationIslandMember>
    {
        RawList<SimulationIslandConnection.Entry> entries;

        internal SimulationIslandMemberList(RawList<SimulationIslandConnection.Entry> entries)
        {
            this.entries = entries;
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
        public int IndexOf(SimulationIslandMember item)
        {
            return Array.IndexOf(entries.Elements, item, 0, entries.count);
        }

        void IList<SimulationIslandMember>.Insert(int index, SimulationIslandMember item)
        {
            throw new NotSupportedException("The list is read-only.");
        }

        void IList<SimulationIslandMember>.RemoveAt(int index)
        {
            throw new NotSupportedException("The list is read-only.");
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        /// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
        public SimulationIslandMember this[int index]
        {
            get
            {
                return entries.Elements[index].Member;
            }
            set
            {
                throw new NotSupportedException("The list is read-only.");
            }
        }

        void ICollection<SimulationIslandMember>.Add(SimulationIslandMember item)
        {
            throw new NotSupportedException("The list is read-only.");
        }

        void ICollection<SimulationIslandMember>.Clear()
        {
            throw new NotSupportedException("The list is read-only.");
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Contains(SimulationIslandMember item)
        {
            return IndexOf(item) > -1;
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(SimulationIslandMember[] array, int arrayIndex)
        {
            for (int i = 0; i < entries.count; i++)
            {
                array[i + arrayIndex] = entries.Elements[i].Member;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count
        {
            get { return entries.count; }
        }

        bool ICollection<SimulationIslandMember>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<SimulationIslandMember>.Remove(SimulationIslandMember item)
        {
            throw new NotSupportedException("The list is read-only.");
        }

        public IEnumerator<SimulationIslandMember> GetEnumerator()
        {
            return new Enumerator(entries);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(entries);
        }

        /// <summary>
        /// Enumerators the simulation island members in the member list.
        /// </summary>
        public struct Enumerator : IEnumerator<SimulationIslandMember>
        {
            private RawList<SimulationIslandConnection.Entry> entries;
            private int index;

            internal Enumerator(RawList<SimulationIslandConnection.Entry> entries)
            {
                this.entries = entries;
                index = -1;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public SimulationIslandMember Current
            {
                get { return entries.Elements[index].Member; }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
            }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <returns>
            /// The current element in the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception><filterpriority>2</filterpriority>
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
                index++;
                return index >= entries.count;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                index = -1;
            }
        }
    }
}
