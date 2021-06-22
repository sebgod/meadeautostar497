using System;
using System.Threading;

namespace ASCOM.Meade.net
{
    public class ThreadSafeEnum<T>
        where T: struct, Enum
    {
        private long _value;

        public ThreadSafeEnum(T value) => _value = EnumToLong(value);

        public void Set(T value) => Interlocked.Exchange(ref _value, EnumToLong(value));

        private static long EnumToLong(T value) => Convert.ToInt64(value);

        public static implicit operator ThreadSafeEnum<T>(T value) => new ThreadSafeEnum<T>(value);

        public static implicit operator T(ThreadSafeEnum<T> @this) => (T) Enum.ToObject(typeof(T), Interlocked.Read(ref @this._value));
    }
}