using System;

namespace BEPUutilities.DataStructures
{
    /// <summary>
    /// Locked queue supporting dequeues from both ends.
    /// </summary>
    /// <typeparam name="T">Type of contained elements.</typeparam>
    public class ConcurrentDeque<T>
    {
        private readonly SpinLock locker = new SpinLock();
        internal T[] array;

        private int count;
        internal int firstIndex;
        internal int lastIndex = -1;

        public ConcurrentDeque(int capacity)
        {
            array = new T[capacity];
        }

        public ConcurrentDeque()
            : this(16)
        {
        }

        /// <summary>
        /// Number of elements in the deque.
        /// </summary>
        public int Count
        {
            get { return count; }
        }

        public override string ToString()
        {
            return "Count: " + count;
        }

        //TODO:  SPEED UP THESE ENQUEUES!

        /// <summary>
        /// Enqueues an element to the tail of the queue with locking.
        /// </summary>
        /// <param name="item">Dequeued element, if any.</param>
        /// <returns>True if an element could be dequeued, false otherwise.</returns>
        public void Enqueue(T item)
        {
            //bool taken = false;
            //locker.Enter(ref taken);
            locker.Enter();
            try
            {
                //Enqueues go to the tail only; it's like a queue.
                //head ----> tail

                if (count == array.Length)
                {
                    //Resize
                    //TODO: Better shift-resize
                    T[] oldArray = array;
                    array = new T[Math.Max(4, oldArray.Length * 2)];
                    //Copy the old first-end to the first part of the new array.
                    Array.Copy(oldArray, firstIndex, array, 0, oldArray.Length - firstIndex);
                    //Copy the old begin-first to the second part of the new array.
                    Array.Copy(oldArray, 0, array, oldArray.Length - firstIndex, firstIndex);
                    firstIndex = 0;
                    lastIndex = count - 1;
                }


                lastIndex++;
                if (lastIndex == array.Length)
                    lastIndex = 0;
                array[lastIndex] = item;
                count++;
            }
            finally
            {
                locker.Exit();
                //locker.Exit();
            }
        }

        /// <summary>
        /// Tries to dequeue the first element of the queue with locking.
        /// </summary>
        /// <param name="item">Dequeued element, if any.</param>
        /// <returns>True if an element could be dequeued, false otherwise.</returns>
        public bool TryDequeueFirst(out T item)
        {
            //bool taken = false;
            //locker.Enter(ref taken);
            locker.Enter();

            try
            {
                if (count > 0)
                {
                    item = array[firstIndex];
                    array[firstIndex] = default(T);
                    firstIndex++;
                    if (firstIndex == array.Length)
                        firstIndex = 0;
                    count--;
                    return true;
                }
                item = default(T);
                return false;
            }
            finally
            {
                locker.Exit();
                //locker.Exit();
            }
        }

        /// <summary>
        /// Tries to dequeue the last element of the queue with locking.
        /// </summary>
        /// <param name="item">Dequeued element, if any.</param>
        /// <returns>True if an element could be dequeued, false otherwise.</returns>
        public bool TryDequeueLast(out T item)
        {
            //bool taken = false;
            //locker.Enter(ref taken);
            locker.Enter();

            try
            {
                if (count > 0)
                {
                    item = array[lastIndex];
                    array[lastIndex] = default(T);
                    lastIndex--;
                    if (lastIndex < 0)
                        lastIndex += array.Length;
                    count--;
                    return true;
                }
                item = default(T);
                return false;
            }
            finally
            {
                locker.Exit();
                //locker.Exit();
            }
        }

        /// <summary>
        /// Tries to dequeue the first element of the queue without locking.
        /// </summary>
        /// <param name="item">Dequeued element, if any.</param>
        /// <returns>True if an element could be dequeued, false otherwise.</returns>
        public bool TryUnsafeDequeueFirst(out T item)
        {
            if (count > 0)
            {
                item = array[firstIndex];
                array[firstIndex] = default(T);
                firstIndex++;
                if (firstIndex == array.Length)
                    firstIndex = 0;
                count--;
                return true;
            }
            item = default(T);
            return false;
        }

        /// <summary>
        /// Tries to dequeue the last element of the queue without locking.
        /// </summary>
        /// <param name="item">Dequeued element, if any.</param>
        /// <returns>True if an element could be dequeued, false otherwise.</returns>
        public bool TryUnsafeDequeueLast(out T item)
        {
            if (count > 0)
            {
                item = array[lastIndex];
                array[lastIndex] = default(T);
                lastIndex--;
                if (lastIndex < 0)
                    lastIndex += array.Length;
                count--;
                return true;
            }
            item = default(T);
            return false;
        }

        /// <summary>
        /// Enqueues an element onto the tail of the deque without locking.
        /// </summary>
        /// <param name="item">Element to enqueue.</param>
        public void UnsafeEnqueue(T item)
        {
            //Enqueues go to the tail only; it's like a queue.
            //head ----> tail

            if (count == array.Length)
            {
                //TODO: if it's always powers of 2, then resizing is quicker.
                //Resize
                T[] oldArray = array;
                array = new T[oldArray.Length * 2];
                //Copy the old first-end to the first part of the new array.
                Array.Copy(oldArray, firstIndex, array, 0, oldArray.Length - firstIndex);
                //Copy the old begin-first to the second part of the new array.
                Array.Copy(oldArray, 0, array, oldArray.Length - firstIndex, firstIndex);
                firstIndex = 0;
                lastIndex = count - 1;
            }


            lastIndex++;
            if (lastIndex == array.Length)
                lastIndex = 0;
            array[lastIndex] = item;
            count++;
        }
    }
}