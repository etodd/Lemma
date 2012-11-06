using System;

namespace BEPUphysics.Threading
{
    internal class ConcurrentDeque<T>
    {
        //public readonly object locker = new object();
        //System.Threading.SpinLock locker = new System.Threading.SpinLock();
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

        public int Count
        {
            get { return count; }
        }

        public override string ToString()
        {
            return "Count: " + count;
        }

        //TODO:  SPEED UP THESE ENQUEUES!
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