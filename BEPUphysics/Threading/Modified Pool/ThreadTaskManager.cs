using System;
using System.Collections.Generic;
using System.Threading;

namespace BEPUphysics.Threading
{
    /// <summary>
    /// Keeps track of the threads currently available to the physics engine.
    /// </summary>
    public class ThreadTaskManager : IThreadManager
    {
        private readonly object disposedLocker = new object();
        private readonly Action<object> doLoopSectionDelegate;
        private readonly List<LoopSection> taskInfos = new List<LoopSection>();
        private readonly List<WorkerThread> workers = new List<WorkerThread>();

        private ManualResetEvent allThreadsIdleNotifier = new ManualResetEvent(false);

        /// <summary>
        /// Index into the thread loop lists, incremented after each task allocation.
        /// </summary>
        private int currentTaskAllocationIndex;

        private bool disposed;

        private int loopTasksPerThread;
        private int tasksRemaining = 1;

        /// <summary>
        /// Constructs a new thread task manager.
        /// </summary>
        public ThreadTaskManager()
        {
            LoopTasksPerThread = 1;
            doLoopSectionDelegate = new Action<object>(DoLoopSection);
        }

        /// <summary>
        /// Releases resources used by the object.
        /// </summary>
        ~ThreadTaskManager()
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
            //TODO: Try a WAITALL version of this
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
                var worker = new WorkerThread(workers.Count, this, initialization, initializationInformation);
                workers.Add(worker);
                RemakeLoopSections();
            }
        }


        /// <summary>
        /// Removes a thread from the manager.
        /// </summary>
        public void RemoveThread()
        {
            if (workers.Count > 0)
            {
                workers[0].EnqueueTask(null, null);
                WaitForTaskCompletion();
                workers[0].Dispose();
            }
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
                currentTaskAllocationIndex = (currentTaskAllocationIndex + 1) % workers.Count;
                workers[currentTaskAllocationIndex].EnqueueTask(task, taskInformation);
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
        /// Releases resources used by the object.
        /// </summary>
        public void Dispose()
        {
            lock (disposedLocker)
            {
                if (!disposed)
                {
                    disposed = true;
                    while (workers.Count > 0)
                    {
                        RemoveThread();
                    }
                    allThreadsIdleNotifier.Close();
                    allThreadsIdleNotifier = null;
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


        private struct TaskEntry
        {
            internal readonly object Info;
            internal readonly Action<object> Task;

            internal TaskEntry(Action<object> task, object info)
            {
                Task = task;
                Info = info;
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
            private readonly ThreadTaskManager manager;
            private readonly ConcurrentDeque<TaskEntry> taskData;
            private readonly Thread thread;
            private readonly Action<object> threadStart;
            private bool disposed;
            private int index;
            private AutoResetEvent resetEvent = new AutoResetEvent(false);

            internal WorkerThread(int index, ThreadTaskManager manager, Action<object> threadStart, object initializationInformation)
            {
                this.manager = manager;
                thread = new Thread(ThreadExecutionLoop);
                thread.IsBackground = true;
                taskData = new ConcurrentDeque<TaskEntry>();
                this.threadStart = threadStart;
                this.initializationInformation = initializationInformation;
                UpdateIndex(index);
                //#if WINDOWS
                //                ResourcePool.addThreadID(thread.ManagedThreadId);
                //#endif
                thread.Start();
            }

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
                        resetEvent.Close();
                        resetEvent = null;
                        manager.workers.Remove(this);
                        GC.SuppressFinalize(this);
                    }
                }
            }

            #endregion

            internal void EnqueueTask(Action<object> task, object taskInformation)
            {
                Interlocked.Increment(ref manager.tasksRemaining);
                taskData.Enqueue(new TaskEntry(task, taskInformation));
                resetEvent.Set();
            }

            private bool TrySteal(int victim, out TaskEntry task)
            {
                return manager.workers[victim].taskData.TryDequeueLast(out task);
            }

            /// <exception cref="ArithmeticException">Thrown when the thread encounters an invalid state; generally propagated float.NaN's.</exception>
            private void ThreadExecutionLoop()
            {
                //Perform any initialization requested.
                if (threadStart != null)
                    threadStart(initializationInformation);
                TaskEntry task;

                while (true)
                {
                    resetEvent.WaitOne();
                    while (true)
                    {
                        if (!taskData.TryDequeueFirst(out task))
                        {
                            bool gotSomething = false;
                            for (int i = 1; i < manager.workers.Count; i++)
                            {
                                if (TrySteal((index + i) % manager.workers.Count, out task))
                                {
                                    gotSomething = true;
                                    break;
                                }
                            }
                            if (!gotSomething)
                                break; //Nothing to steal and I'm broke! Guess I'll mosey on out
                        }
                        try
                        {
                            if (task.Task != null)
                                task.Task(task.Info);
                            if (Interlocked.Decrement(ref manager.tasksRemaining) == 0)
                            {
                                manager.allThreadsIdleNotifier.Set();
                            }
                            if (task.Task == null)
                                return;
                        }
                        catch (ArithmeticException arithmeticException)
                        {
                            throw new ArithmeticException(
                                "Some internal multithreaded arithmetic has encountered an invalid state.  Check for invalid entity momentums, velocities, and positions; propagating NaN's will generally trigger this exception in the getExtremePoint function.",
                                arithmeticException);
                        }
                    }
                }
            }

            private void UpdateIndex(int newIndex)
            {
                index = newIndex;
            }
        }
    }
}