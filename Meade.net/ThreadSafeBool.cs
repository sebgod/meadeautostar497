using System.Threading;

namespace ASCOM.Meade.net
{
    public class ThreadSafeBool
    {
        private object _value;

        public ThreadSafeBool(in bool value) => _value = value;

        public void Set(in bool value) => Interlocked.Exchange(ref _value, value);

        public static implicit operator ThreadSafeBool(in bool value) => new ThreadSafeBool(value);

        public static implicit operator bool(ThreadSafeBool @this) => (bool)@this._value;
    }
}