using ASCOM.DeviceInterface;

namespace ASCOM.Meade.net
{
    public class AlignmentStatus
    {
        public AlignmentModes AlignmentMode { get; set; }
        public bool Tracking { get; set; }
        public Alignment Status { get; set; }
    }
}