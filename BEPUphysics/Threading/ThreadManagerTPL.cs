#if WINDOWS
using System;
using System.Threading.Tasks;

namespace BEPUphysics.Threading
{
    /// <summary>
    /// Uses the .NET Task Parallel library to manage the engine's threads.
    /// </summary>
    public class ThreadManagerTPL : IThreadManager
    {
        /// <summary>
        /// Gets or sets the task manager used to supplement the TPL.
        /// </summary>
        public IThreadManager TaskManager { get; set; }

        /// <summary>
        /// Gets the number of threads that the threadpool is targeting.
        /// </summary>
        public int ThreadCount
        {
            get { return TaskManager.ThreadCount; }
        }

        /// <summary>
        /// Constructs the TPL thread manager.
        /// </summary>
        public ThreadManagerTPL()
        {
            TaskManager = new ThreadTaskManager();
        }

        /// <summary>
        /// Notifies the thread manager that it should use another thread.
        /// </summary>
        public void AddThread()
        {
            TaskManager.AddThread();
        }

        /// <summary>
        /// Notifies the thread manager that it should use another thread.
        /// </summary>
        /// <param name="initialization">Function to use to initialize the thread.</param>
        /// <param name="initializationInformation">Information to provide to the initializer.</param>
        public void AddThread(Action<object> initialization, object initializationInformation)
        {
            TaskManager.AddThread(initialization, initializationInformation);
        }

        /// <summary>
        /// Notifies the thread manager that it should decrease the number of threads used.
        /// </summary>
        public void RemoveThread()
        {
            TaskManager.RemoveThread();
        }


        /// <summary>
        /// Enqueues a task to the thread manager.
        /// This should be safe to call from multiple threads and from other tasks.
        /// </summary>
        /// <param name="taskBody">Method to run.</param>
        /// <param name="taskInformation">Data to give to the task.</param>
        public void EnqueueTask(Action<object> taskBody, object taskInformation)
        {
            TaskManager.EnqueueTask(taskBody, taskInformation);
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
            Parallel.For(startIndex, endIndex, loopBody);
        }

        /// <summary>
        /// Waits until all tasks enqueued using enqueueTask are complete.
        /// </summary>
        public void WaitForTaskCompletion()
        {
            TaskManager.WaitForTaskCompletion();
        }


        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            TaskManager.Dispose();
        }

        #endregion
    }
}

#endif