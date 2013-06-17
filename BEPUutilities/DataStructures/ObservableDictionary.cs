using System;
using System.Collections.Generic;

namespace BEPUutilities.DataStructures
{
    ///<summary>
    /// Dictionary that provides events when the inner dictionary is changed.
    ///</summary>
    ///<typeparam name="TKey">Type of the keys in the dictionary.</typeparam>
    ///<typeparam name="TValue">Type of the values in the dictionary.</typeparam>
    public class ObservableDictionary<TKey, TValue>
    {
        ///<summary>
        /// Gets or sets the dictionary wrapped by the observable dictionary.
        /// While the inner dictionary can be changed, making modifications to it will
        /// not trigger any changed events.
        ///</summary>
        public Dictionary<TKey, TValue> WrappedDictionary { get; private set; }

        /// <summary>
        /// Constructs a new observable dictionary.
        /// </summary>
        public ObservableDictionary()
        {
            WrappedDictionary = new Dictionary<TKey, TValue>();
        }

        ///<summary>
        /// Adds a pair to the dictionary.
        ///</summary>
        ///<param name="key">Key of the element.</param>
        ///<param name="value">Value of the element.</param>
        public void Add(TKey key, TValue value)
        {
            WrappedDictionary.Add(key, value);
            OnChanged();
        }

        ///<summary>
        /// Removes a key and its associated value from the dictionary, if present.
        ///</summary>
        ///<param name="key">Key of the element to remove.</param>
        ///<returns>Whether or not the object was found.</returns>
        public bool Remove(TKey key)
        {
            if (WrappedDictionary.Remove(key))
            {
                OnChanged();
                return true;
            }
            return false;
        }

        ///<summary>
        /// Clears the dictionary of all elements.
        ///</summary>
        public void Clear()
        {
            WrappedDictionary.Clear();
            OnChanged();
        }

        ///<summary>
        /// Fires when the dictionary's elements are changed using the wrapping functions.
        ///</summary>
        public event Action Changed;

        protected void OnChanged()
        {
            if (Changed != null)
            {
                Changed();
            }
        }

        
    }
}
