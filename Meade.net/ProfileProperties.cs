namespace ASCOM.Meade.net
{
    public class ProfileProperties
    {
        // properies that are part of the profile
        public string ComPort { get; set; }
        public bool TraceLogger { get; set; }
        public double GuideRateArcSecondsPerSecond { get; set; }
        public string Precision { get; set; }
    }
}