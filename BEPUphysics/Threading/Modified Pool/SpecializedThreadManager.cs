using System;

namespace BEPUphysics.Threading
{
    /// <summary>
    /// Manages the engine's threads.
    /// </summary>
    /// <remarks>
    /// Separates the management of ThreadTasks and loops
    /// into specialized systems.  Should have generally higher
    /// performance than the SimpleThreadManager.
    /// </remarks>
    public class SpecializedThreadManager : IThreadManager
    {
        private readonly object disposedLocker = new object();
        private bool disposed;
        private ParallelLoopManager loopManager;
        private ThreadTaskManager taskManager;

        /// <summary>
        /// Constructs a new specialized thread manager
        /// that manages loops and tasks separately.
        /// </summary>
        public SpecializedThreadManager()
        {
            taskManager = new ThreadTaskManager();
            loopManager = new ParallelLoopManager();
        }

        /// <summary>
        /// Releases resources used by the object.
        /// </summary>
        ~SpecializedThreadManager()
        {
            Dispose();
        }

        /// <summary>
        /// Gets or sets the loop manager used by this threading system.
        /// The loop manager is used to specifically parallelize forloops.
        /// </summary>
        public ParallelLoopManager LoopManager
        {
            get { return loopManager; }
            set { loopManager = value; }
        }

        /// <summary>
        /// Gets or sets the task manager used by this threading system.
        /// The task manager is used for anything that isn't strictly a 
        /// for loop.
        /// </summary>
        public ThreadTaskManager TaskManager
        {
            get { return taskManager; }
            set { taskManager = value; }
        }

        #region IThreadManager Members

        /// <summary>
        /// Gets the number of threads in use by the manager.
        /// </summary>
        public int ThreadCount
        {
            get { return taskManager.ThreadCount; }
        }

        /// <summary>
        /// Adds a new worker thread to the engine.
        /// </summary>
        public void AddThread()
        {
            taskManager.AddThread();
            loopManager.AddThread();
        }

        /// <summary>
        /// Adds a new worker thread to the engine.
        /// </summary>
        /// <param name="initialization">Function that each of the new threads will call before entering its work loop.  Note that this type of thread manager spawns two worker threads for each given thread;
        /// the initializer will run twice.</param>
        /// <param name="initializationInformation">Data to give the initializer.</param>
        public void AddThread(Action<object> initialization, object initializationInformation)
        {
            taskManager.AddThread(initialization, initializationInformation);
            loopManager.AddThread(initialization, initializationInformation);
        }

        /// <summary>
        /// Removes a worker thread from the engine.
        /// </summary>
        public void RemoveThread()
        {
            taskManager.RemoveThread();
            loopManager.RemoveThread();
        }

        /// <summary>
        /// Enqueues a task to the thread manager.
        /// This should be safe to call from multiple threads and from other tasks.
        /// </summary>
        /// <param name="taskBody">Method to run.</param>
        /// <param name="taskInformation">Data to give to the task.</param>
        public void EnqueueTask(Action<object> taskBody, object taskInformation)
        {
            taskManager.EnqueueTask(taskBody, taskInformation);
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
            loopManager.ForLoop(startIndex, endIndex, loopBody);
        }

        /// <summary>
        /// Waits until all tasks enqueued using enqueueTask are complete.
        /// </summary>
        public void WaitForTaskCompletion()
        {
            taskManager.WaitForTaskCompletion();
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
                    taskManager.Dispose();
                    loopManager.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
        }

        #endregion
    }
}