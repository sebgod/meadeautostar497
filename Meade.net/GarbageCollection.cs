using System;
using System.Threading;

namespace ASCOM.Meade.net
{
    /// <summary>
    /// Summary description for GarbageCollection.
    /// </summary>
    class GarbageCollection
    {
        private bool _mbContinueThread;
        private readonly int _miInterval;
        private readonly ManualResetEvent _mEventThreadEnded;

        public GarbageCollection(int iInterval)
        {
            _mbContinueThread = true;
            _miInterval = iInterval;
            _mEventThreadEnded = new ManualResetEvent(false);
        }

        public void GcWatch()
        {
            // Pause for a moment to provide a delay to make threads more apparent.
            while (ContinueThread())
            {
                GC.Collect();
                Thread.Sleep(_miInterval);
            }
            _mEventThreadEnded.Set();
        }

        private bool ContinueThread()
        {
            lock (this)
            {
                return _mbContinueThread;
            }
        }

        public void StopThread()
        {
            lock (this)
            {
                _mbContinueThread = false;
            }
        }

        public void WaitForThreadToStop()
        {
            _mEventThreadEnded.WaitOne();
            _mEventThreadEnded.Reset();
        }
    }
}
