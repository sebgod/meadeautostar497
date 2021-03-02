using System;

namespace ASCOM.Meade.net
{
    public class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}