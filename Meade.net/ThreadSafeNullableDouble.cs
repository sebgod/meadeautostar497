using System;
using System.Threading;

namespace ASCOM.Meade.net
{
    public class ThreadSafeNullableDouble
    {
        private long _value;

        public ThreadSafeNullableDouble(in double? value) => _value = NullableDoubleToLong(value);

        public void Set(in double? value) => Interlocked.Exchange(ref _value, NullableDoubleToLong(value));

        private static long NullableDoubleToLong(in double? value) => BitConverter.DoubleToInt64Bits(value ?? double.NaN);

        public static implicit operator ThreadSafeNullableDouble(in double? value) => new ThreadSafeNullableDouble(value);

        public static implicit operator double?(ThreadSafeNullableDouble @this)
        {
            var doubleValue = BitConverter.Int64BitsToDouble(Interlocked.Read(ref @this._value));
            return double.IsNaN(doubleValue) ? null as double? : doubleValue;
        }
    }
}