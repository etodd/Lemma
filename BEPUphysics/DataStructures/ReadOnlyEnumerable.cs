using System.Collections;
using System.Collections.Generic;

namespace BEPUphysics.DataStructures
{
    ///<summary>
    /// WRaps an enumerable in a temporary enumeration struct.
    ///</summary>
    ///<typeparam name="T">Type of the enumerable being iterated.</typeparam>
    public struct ReadOnlyEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> enumerable;

        ///<summary>
        /// Constructs a new read only enumerable.
        ///</summary>
        ///<param name="enumerable">Enumerable to wrap.</param>
        public ReadOnlyEnumerable(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return enumerable.GetEnumerator();
        }

        #endregion
    }
}