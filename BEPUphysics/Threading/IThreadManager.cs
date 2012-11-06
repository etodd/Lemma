using System;

namespace BEPUphysics.Threading
{

    /// <summary>
    /// Manages the engine's threads.
    /// </summary>
    /// <remarks>
    /// The thread manager is constructed with certain access assumptions in mind.
    /// When implementing custom thread managers, ensure that the requirements are met
    /// or exceeded with regard to concurrent access.
    /// </remarks>
    public interface IThreadManager : IDisposable
    {
        /// <summary>
        /// Gets the number of threads currently managed by the thread manager.
        /// </summary>
        int ThreadCount { get; }

        /// <summary>
        /// Adds a new worker thread to the engine.
        /// </summary>
        void AddThread();

        /// <summary>
        /// Adds a new worker thread to the engine.
        /// </summary>
        /// <param name="initialization">Function that the new thread will call before entering its work loop.</param>
        /// <param name="initializationInformation">Data to give the initializer.</param>
        void AddThread(Action<object> initialization, object initializationInformation);

        /// <summary>
        /// Enqueues a task to the thread manager.
        /// This should be safe to call from multiple threads and from other tasks.
        /// </summary>
        /// <param name="taskBody">Method to run.</param>
        /// <param name="taskInformation">Data to give to the task.</param>
        void EnqueueTask(Action<object> taskBody, object taskInformation);

        /// <summary>
        /// Loops from the starting index (inclusive) to the ending index (exclusive), calling the loopBody at each iteration.
        /// The forLoop function will not return until all iterations are complete.
        /// This is meant to be used in a 'fork-join' model; only a single thread should be running a forLoop
        /// at any time.
        /// </summary>
        /// <param name="startIndex">Inclusive starting index.</param>
        /// <param name="endIndex">Exclusive ending index.</param>
        /// <param name="loopBody">Function that handles an individual iteration of the loop.</param>
        void ForLoop(int startIndex, int endIndex, Action<int> loopBody);

        /// <summary>
        /// Removes a worker thread from the engine.
        /// </summary>
        void RemoveThread();

        /// <summary>
        /// Waits until all tasks enqueued using enqueueTask are complete.
        /// </summary>
        void WaitForTaskCompletion();

        //optional; if non-setting enqueue is added, doTasks is needed.
        //non-setting enqueue
        //doTasks
    }
}