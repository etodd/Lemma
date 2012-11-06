using System;
using System.Collections.Generic;
using System.Threading;

namespace BEPUphysics.Threading
{
    /// <summary>
    /// Manages the engine's threads.
    /// </summary>
    /// <remarks>
    /// Uses a simple round-robin threadpool.
    /// It is recommended that other thread managers are used instead of this one;
    /// it is kept for compatability and a fallback in case of problems.
    /// </remarks>
    public class SimpleThreadManager : IThreadManager
    {
        private readonly ManualResetEvent allThreadsIdleNotifier = new ManualResetEvent(false);
        private readonly object disposedLocker = new object();
        private readonly Action<object> doLoopSectionDelegate;
        private readonly List<LoopSection> taskInfos = new List<LoopSection>();

        private readonly List<WorkerThread> workers = new List<WorkerThread>();

        /// <summary>
        /// Index into the thread loop lists, incremented after each task allocation.
        /// </summary>
        private int currentTaskAllocationIndex;

        private bool disposed;
        private int loopTasksPerThread;

        private int tasksRemaining = 1;

        /// <summary>
        /// Constructs the thread manager.
        /// </summary>
        public SimpleThreadManager()
        {
            LoopTasksPerThread = 1;
            doLoopSectionDelegate = new Action<object>(DoLoopSection);
        }

        /// <summary>
        /// Releases resources used by the object.
        /// </summary>
        ~SimpleThreadManager()
        {
            Dispose();
        }

        /// <summary>
        /// Gets or sets the number of tasks to create per thread when doing forLoops.
        /// </summary>
        public int LoopTasksPerThread
        {
            get { return loopTasksPerThread; }
            set
            {
                loopTasksPerThread = value;
                RemakeLoopSections();
            }
        }

        #region IThreadManager Members

        /// <summary>
        /// Gets the number of threads currently handled by the manager.
        /// </summary>
        public int ThreadCount
        {
            get { return workers.Count; }
        }


        /// <summary>
        /// Blocks the current thread until all tasks have been completed.
        /// </summary>
        public void WaitForTaskCompletion()
        {
            if (Interlocked.Decrement(ref tasksRemaining) == 0)
            {
                allThreadsIdleNotifier.Set();
            }
            allThreadsIdleNotifier.WaitOne();

            //When it gets here, it means things are successfully idle'd.
            tasksRemaining = 1;
            allThreadsIdleNotifier.Reset();
        }


        /// <summary>
        /// Adds a thread to the manager.
        /// </summary>
        public void AddThread()
        {
            AddThread(null, null);
        }

        /// <summary>
        /// Adds a thread to the manager.
        /// </summary>
        /// <param name="initialization">A function to run to perform any initialization on the new thread.</param>
        /// <param name="initializationInformation">Data to give the ParameterizedThreadStart for initialization.</param>
        public void AddThread(Action<object> initialization, object initializationInformation)
        {
            lock (workers)
            {
                var worker = new WorkerThread(this, initialization, initializationInformation);
                workers.Add(worker);
                RemakeLoopSections();
            }
        }


        /// <summary>
        /// Removes a thread and blocks until success.
        /// </summary>
        public void RemoveThread()
        {
            EnqueueTask(null, null);
            WaitForTaskCompletion();
            RemakeLoopSections();
        }


        /// <summary>
        /// Gives the thread manager a new task to run.
        /// </summary>
        /// <param name="task">Task to run.</param>
        /// <param name="taskInformation">Information to be used by the task.</param>
        public void EnqueueTask(Action<object> task, object taskInformation)
        {
            lock (workers)
            {
                workers[currentTaskAllocationIndex].EnqueueTask(task, taskInformation);
                currentTaskAllocationIndex = (currentTaskAllocationIndex + 1) % workers.Count;
            }
        }


        /// <summary>
        /// Loops from the starting index (inclusive) to the ending index (exclusive), calling the loopBody at each iteration.
        /// The forLoop function will not return until all iterations are complete.
        /// This is meant to be used in a 'fork-join' model; only a single thread should be running a forLoop
        /// at any time.
        /// </summary>
        /// <param name="startIndex">Inclusive starting index.</param>
        /// <param name="endIndex">Exclusive ending index.</param>
        /// <param name="loopBody">Function that handles an individual iteration of the loop.</param>
        public void ForLoop(int startIndex, int endIndex, Action<int> loopBody)
        {
            int subdivisions = workers.Count * loopTasksPerThread;
            int iterationCount = endIndex - startIndex;
            for (int b = 0; b < subdivisions; b++)
            {
                taskInfos[b].loopBody = loopBody;
                taskInfos[b].iterationCount = iterationCount;
                EnqueueTaskSequentially(doLoopSectionDelegate, taskInfos[b]);
            }
            WaitForTaskCompletion();
        }


        /// <summary>
        /// Releases threads and resources used by the thread manager.
        /// </summary>
        public void Dispose()
        {
            lock (disposedLocker)
            {
                if (!disposed)
                {
                    disposed = true;
                    ShutDown();
                    allThreadsIdleNotifier.Close();
                    GC.SuppressFinalize(this);
                }
            }
        }

        #endregion

        /// <summary>
        /// Enqueues a task.
        /// This method also does not perform any locking; it should only be called when all worker threads of the thread pool are idle and all calls to this method are from the same thread.
        /// </summary>
        /// <param name="task">Task to enqueue.</param>
        /// <param name="taskInformation">Information for the task.</param>
        public void EnqueueTaskSequentially(Action<object> task, object taskInformation)
        {
            //enqueueTask(task, taskInformation);
            workers[currentTaskAllocationIndex].EnqueueTask(task, taskInformation);
            currentTaskAllocationIndex = (currentTaskAllocationIndex + 1) % workers.Count;
            //workers[currentTaskAllocationIndex].enqueueTaskSequentially(task, taskInformation);
            //currentTaskAllocationIndex = (currentTaskAllocationIndex + 1) % workers.Count;
        }

        /// <summary>
        /// Tells every thread in the thread manager to shut down and waits until completion.
        /// </summary>
        public void ShutDown()
        {
            var toJoin = new Queue<Thread>();

            for (int i = workers.Count - 1; i >= 0; i--)
            {
                lock (workers)
                {
                    toJoin.Enqueue(workers[i].Thread);
                    workers[i].EnqueueTask(null, null);
                }
            }
            while (toJoin.Count > 0)
            {
                toJoin.Dequeue().Join();
            }
        }

        private static void DoLoopSection(object o)
        {
            var data = o as LoopSection;
            int finalIndex = (data.iterationCount * (data.Index + 1)) / data.Subdivisions;
            for (int i = (data.iterationCount * data.Index) / data.Subdivisions; i < finalIndex; i++)
            {
                //do stuff
                data.loopBody(i);
            }
        }

        private void RemakeLoopSections()
        {
            taskInfos.Clear();
            int workerCount = workers.Count;
            int subdivisions = workerCount * loopTasksPerThread;
            for (int i = 0; i < workerCount; i++)
            {
                for (int j = 0; j < loopTasksPerThread; j++)
                {
                    taskInfos.Add(new LoopSection(i * loopTasksPerThread + j, subdivisions));
                }
            }
        }

        private class LoopSection
        {
            internal readonly int Index;
            internal readonly int Subdivisions;
            internal int iterationCount;
            internal Action<int> loopBody;

            internal LoopSection(int index, int subdivisions)
            {
                Index = index;
                Subdivisions = subdivisions;
            }
        }

        private class WorkerThread : IDisposable
        {
            private readonly object disposedLocker = new object();
            private readonly object initializationInformation;
            private readonly SimpleThreadManager manager;
            private readonly AutoResetEvent resetEvent = new AutoResetEvent(false);
            private readonly Queue<object> taskInformationQueue;
            private readonly Queue<Action<object>> taskQueue;
            internal readonly Thread Thread;
            private readonly Action<object> threadStart;
            private bool disposed;

            internal WorkerThread(SimpleThreadManager manager, Action<object> threadStart, object initializationInformation)
            {
                this.manager = manager;
                Thread = new Thread(ThreadExecutionLoop);
                Thread.IsBackground = true;
                taskQueue = new Queue<Action<object>>();
                taskInformationQueue = new Queue<object>();
                this.threadStart = threadStart;
                this.initializationInformation = initializationInformation;
                Thread.Start();
            }

            /// <summary>
            /// Shuts down any still living threads.
            /// </summary>
            ~WorkerThread()
            {
                Dispose();
            }

            #region IDisposable Members

            public void Dispose()
            {
                lock (disposedLocker)
                {
                    if (!disposed)
                    {
                        disposed = true;
                        ShutDownThread();
                        resetEvent.Close();
                        GC.SuppressFinalize(this);
                    }
                }
            }

            #endregion

            internal void EnqueueTask(Action<object> task, object taskInformation)
            {
                lock (taskQueue)
                {
                    Interlocked.Increment(ref manager.tasksRemaining);
                    taskQueue.Enqueue(task);
                    taskInformationQueue.Enqueue(taskInformation);
                    resetEvent.Set();
                }
            }


            private void ShutDownThread()
            {
                //Let the manager know that it is done with its 'task'!
                if (Interlocked.Decrement(ref manager.tasksRemaining) == 0)
                {
                    if (!manager.disposed) //Don't mess with the handle if it's already disposed.
                        manager.allThreadsIdleNotifier.Set();
                }
                //Dump out any remaining tasks in the queue.
                for (int i = 0; i < taskQueue.Count; i++) //This is still safe since shutDownThread is called from within a lock(taskQueue) block.
                {
                    taskQueue.Dequeue();
                    if (Interlocked.Decrement(ref manager.tasksRemaining) == 0)
                    {
                        if (!manager.disposed) //Don't mess with the handle if it's already disposed.
                            manager.allThreadsIdleNotifier.Set();
                    }
                }

                lock (manager.workers)
                    manager.workers.Remove(this);
            }

            /// <exception cref="ArithmeticException">Thrown when the thread encounters an invalid state; generally propagated float.NaN's.</exception>
            private void ThreadExecutionLoop()
            {
                //Perform any initialization requested.
                if (threadStart != null)
                    threadStart(initializationInformation);
                object information = null;

                while (true)
                {
                    Action<object> task = null;
                    lock (taskQueue)
                    {
                        if (taskQueue.Count > 0)
                        {
                            task = taskQueue.Dequeue();
                            if (task == null)
                            {
                                Dispose();
                                return;
                            }

                            information = taskInformationQueue.Dequeue();
                        }
                    }
                    if (task != null)
                    {
                        //Perform the task!
                        try
                        {
                            task(information);
                        }
                        catch (ArithmeticException arithmeticException)
                        {
                            throw new ArithmeticException(
                                "Some internal multithreaded arithmetic has encountered an invalid state.  Check for invalid entity momentums, velocities, and positions; propagating NaN's will generally trigger this exception in the getExtremePoint function.",
                                arithmeticException);
                        }
                        if (Interlocked.Decrement(ref manager.tasksRemaining) == 0)
                        {
                            manager.allThreadsIdleNotifier.Set();
                            resetEvent.WaitOne();
                        }
                    }
                    else
                        resetEvent.WaitOne();
                }
            }
        }
    }
}