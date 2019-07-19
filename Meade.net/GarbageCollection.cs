using System;
using System.Threading;

namespace ASCOM.Meade.net
{
    /// <summary>
    /// Summary description for GarbageCollection.
    /// </summary>
    class GarbageCollection
    {
        protected bool MBContinueThread;
        protected bool MGcWatchStopped;
        protected int MIInterval;
        protected ManualResetEvent MEventThreadEnded;

        public GarbageCollection(int iInterval)
        {
            MBContinueThread = true;
            MGcWatchStopped = false;
            MIInterval = iInterval;
            MEventThreadEnded = new ManualResetEvent(false);
        }

        public void GcWatch()
        {
            // Pause for a moment to provide a delay to make threads more apparent.
            while (ContinueThread())
            {
                GC.Collect();
                Thread.Sleep(MIInterval);
            }
            MEventThreadEnded.Set();
        }

        protected bool ContinueThread()
        {
            lock (this)
            {
                return MBContinueThread;
            }
        }

        public void StopThread()
        {
            lock (this)
            {
                MBContinueThread = false;
            }
        }

        public void WaitForThreadToStop()
        {
            MEventThreadEnded.WaitOne();
            MEventThreadEnded.Reset();
        }
    }
}
