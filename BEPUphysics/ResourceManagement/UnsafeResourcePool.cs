using System;
using System.Collections.Generic;

namespace BEPUphysics.ResourceManagement
{
    /// <summary>
    /// Manages a resource type, but performs no locking to handle asynchronous access.
    /// </summary>
    /// <typeparam name="T">Type of object to store in the pool.</typeparam>
    public class UnsafeResourcePool<T> : ResourcePool<T> where T : class, new()
    {
        private readonly Stack<T> stack;

        /// <summary>
        /// Constructs a new locking resource pool.
        /// </summary>
        /// <param name="initialResourceCount">Number of resources to include in the pool by default.</param>
        /// <param name="initializer">Function to initialize new instances in the resource pool with.</param>
        public UnsafeResourcePool(int initialResourceCount, Action<T> initializer)
        {
            InstanceInitializer = initializer;
            stack = new Stack<T>(initialResourceCount);
            Initialize(initialResourceCount);
        }


        /// <summary>
        /// Constructs a new locking resource pool.
        /// </summary>
        /// <param name="initialResourceCount">Number of resources to include in the pool by default.</param>
        public UnsafeResourcePool(int initialResourceCount)
            : this(initialResourceCount, null)
        {
        }

        /// <summary>
        /// Constructs a new locking resource pool.
        /// </summary>
        public UnsafeResourcePool()
            : this(10)
        {
        }

        /// <summary>
        /// Gets the number of resources in the pool.
        /// Even if the resource count hits 0, resources
        /// can still be requested; they will be allocated
        /// dynamically.
        /// </summary>
        public override int Count
        {
            get { return stack.Count; }
        }

        /// <summary>
        /// Gives an item back to the resource pool.
        /// </summary>
        /// <param name="item">Item to return.</param>
        public override void GiveBack(T item)
        {
            stack.Push(item);
        }


        /// <summary>
        /// Initializes the pool with some resources.
        /// Throws away excess resources.
        /// </summary>
        /// <param name="initialResourceCount">Number of resources to include.</param>
        public override void Initialize(int initialResourceCount)
        {
            while (stack.Count > initialResourceCount)
            {
                stack.Pop();
            }
            if (InstanceInitializer != null)
                foreach (T t in stack)
                {
                    InstanceInitializer(t);
                }
            while (stack.Count < initialResourceCount)
            {
                stack.Push(CreateNewResource());
            }
        }

        /// <summary>
        /// Takes an item from the resource pool.
        /// </summary>
        /// <returns>Item to take.</returns>
        public override T Take()
        {
            if (stack.Count > 0)
            {
                return stack.Pop();
            }
            else
            {
                return CreateNewResource();
            }
        }

        /// <summary>
        /// Clears out the resource pool.
        /// </summary>
        public override void Clear()
        {
            stack.Clear();
        }
    }
}