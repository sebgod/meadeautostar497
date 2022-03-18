using ASCOM.DeviceInterface;

namespace ASCOM.Meade.net
{
    public class AlignmentStatus
    {
        public AlignmentModes AlignmentMode { get; set; }
        public bool Tracking { get; set; }
        public Alignment Status { get; set; }

        public override string ToString()
        {
            return $"AlignmentStatus AlignmentMode={AlignmentMode};Tracking={Tracking};Status={Status}";
        }
    }
}