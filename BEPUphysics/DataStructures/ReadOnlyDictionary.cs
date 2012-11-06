using System.Collections;
using System.Collections.Generic;

namespace BEPUphysics.DataStructures
{
    //TODO: This could be handled better.

    ///<summary>
    /// Wraps a dictionary in a read only collection.
    ///</summary>
    ///<typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    ///<typeparam name="TValue">Type of values in the dictionary.</typeparam>
    public struct ReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

        /// <summary>
        /// Constructs a new read-only wrapper dictionary.
        /// </summary>
        /// <param name="dictionary">Internal dictionary to use.</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count
        {
            get { return dictionary.Count; }
        }

        /// <summary>
        /// Gets whether or not this dictionary is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the value associated with the key in the dictionary.
        /// </summary>
        /// <param name="key">Key to look for in the dictionary.</param>
        /// <returns>Value associated with the key.</returns>
        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
        }

        /// <summary>
        /// Gets an enumerable set of keys in the dictionary.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get { return new ReadOnlyEnumerable<TKey>(dictionary.Keys); }
        }

        /// <summary>
        /// Gets an enumerable set of values in the dictionary.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get { return new ReadOnlyEnumerable<TValue>(dictionary.Values); }
        }

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Gets an enumerator for key-value pairs in the dictionary.
        /// </summary>
        /// <returns>Enumerator for the dictionary.</returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for key-value pairs in the dictionary.
        /// </summary>
        /// <returns>Enumerator for the dictionary.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Determines if the dictionary contains a key-value pair.
        /// </summary>
        /// <param name="item">Key-value pair to look for.</param>
        /// <returns>Whether or not the key-value pair is present.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        /// <summary>
        /// Determines if the dictionary contains a given key.
        /// </summary>
        /// <param name="key">Key to check for.</param>
        /// <returns>Whether or not the key is contained.</returns>
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Copies the key-value pairs of the dictionary into an array.
        /// </summary>
        /// <param name="array">Target array.</param>
        /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Tries to retrieve a value from the dictionary using a key.
        /// </summary>
        /// <param name="key">Key to look for.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Whether or not the key exists.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }
    }
}