using System;

namespace ASCOM.Meade.net
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}