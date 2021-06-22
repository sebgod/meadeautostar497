using System;
using System.Threading;

namespace ASCOM.Meade.net
{
    public class ThreadSafeDateTime
    {
        private long _value;

        public ThreadSafeDateTime(in DateTime value) => _value = DateTimeToLong(value);

        public void Set(in DateTime value) => Interlocked.Exchange(ref _value, DateTimeToLong(value));

        private static long DateTimeToLong(in DateTime value) => value.ToUniversalTime().Ticks;

        public static implicit operator ThreadSafeDateTime(in DateTime value) => new ThreadSafeDateTime(value);

        public static implicit operator DateTime(ThreadSafeDateTime @this) => new DateTime(Interlocked.Read(ref @this._value), DateTimeKind.Utc);
    }
}