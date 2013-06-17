using System;

namespace BEPUutilities.ResourceManagement
{
    /// <summary>
    /// Manages a cache of a type of resource.
    /// </summary>
    /// <typeparam name="T">Type of object to pool.</typeparam>
    public abstract class ResourcePool<T> where T : class, new()
    {
        /// <summary>
        /// Gets the number of resources in the pool.
        /// Even if the resource count hits 0, resources
        /// can still be requested; they will be allocated
        /// dynamically.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Gets or sets the function that configures new instances.
        /// This is only called once per object created for the resource pool.
        /// </summary>
        public Action<T> InstanceInitializer { get; set; }

        /// <summary>
        /// Gives an item back to the resource pool.
        /// </summary>
        /// <param name="item">Item to return.</param>
        public abstract void GiveBack(T item);

        /// <summary>
        /// Initializes the pool with some resources.
        /// Throws away excess resources.
        /// </summary>
        /// <param name="initialResourceCount">Number of resources to include.</param>
        public abstract void Initialize(int initialResourceCount);

        /// <summary>
        /// Takes an item from the resource pool.
        /// </summary>
        /// <returns>Item to take.</returns>
        public abstract T Take();

        /// <summary>
        /// Creates and returns a new resource.
        /// </summary>
        /// <returns>New resource.</returns>
        protected T CreateNewResource()
        {
            var toReturn = new T();
            if (InstanceInitializer != null)
                InstanceInitializer(toReturn);
            return toReturn;
        }

        /// <summary>
        /// Removes all elements from the pool.
        /// </summary>
        public abstract void Clear();

    }
}