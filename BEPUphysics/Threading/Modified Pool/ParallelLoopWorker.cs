using System;
using System.Threading;

namespace BEPUphysics.Threading
{
    internal class ParallelLoopWorker : IDisposable
    {
        private readonly ParallelLoopManager manager;
        internal bool disposed;
        internal object disposedLocker = new object();
        internal int finalIndex;

        internal AutoResetEvent getToWork;

        private object initializationInformation;

        internal int iterationsPerSteal;
        private Thread thread;
        private Action<object> threadStart;

        internal ParallelLoopWorker(ParallelLoopManager manager, Action<object> threadStart, object initializationInformation)
        {
            this.manager = manager;
            this.threadStart = threadStart;
            this.initializationInformation = initializationInformation;

            getToWork = new AutoResetEvent(false);

            thread = new Thread(Work) {IsBackground = true};
            thread.Start();
        }


        /// <summary>
        /// Releases resources used by the object.
        /// </summary>
        ~ParallelLoopWorker()
        {
            Dispose();
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the worker.
        /// </summary>
        public void Dispose()
        {
            lock (disposedLocker)
            {
                if (!disposed)
                {
                    disposed = true;
                    getToWork.Close();
                    getToWork = null;
                    thread = null;
                    GC.SuppressFinalize(this);
                }
            }
        }

        #endregion

        internal void Work()
        {
            if (threadStart != null)
            {
                threadStart(initializationInformation);
            }
            threadStart = null;
            initializationInformation = null;

            while (true)
            {
                //When ThreadManager sees a loop available, it set it up and then wake me up.
                getToWork.WaitOne();
                if (manager.currentLoopBody == null)
                {
                    //Woops, looks like it's time for me to die.
                    manager.OnWorkerFinish();
                    return;
                }

                while (manager.jobIndex <= manager.maxJobIndex)
                {
                    //Claim a piece of job.
                    int jobIndex = Interlocked.Increment(ref manager.jobIndex);
                    //The job interval.
                    int endIndex = jobIndex * iterationsPerSteal;
                    int beginIndex = endIndex - iterationsPerSteal;

                    //Do the job piece.  Make sure you don't do more than exists in the list itself.
                    for (int i = beginIndex; i < endIndex && i < finalIndex; i++)
                    {
                        manager.currentLoopBody(i);
                    }
                } //this is not 'thread safe' but the result of the unsafety is a quick fail in the worst case.

                manager.OnWorkerFinish();
            }
        }
    }
}