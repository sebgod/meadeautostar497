namespace ASCOM.Meade.net
{
    public class ProfileProperties
    {
        // properies that are part of the profile
        public string ComPort { get; set; }
        public bool TraceLogger { get; set; }
        public double GuideRateArcSecondsPerSecond { get; set; }
        public string Precision { get; set; }
        public string GuidingStyle { get; set; }
        public int BacklashCompensation { get; set; }
        public bool ReverseFocusDirection { get; set; }
        public bool DynamicBreaking { get; set; }
        public bool RtsDtrEnabled { get; set; }
        public double SiteElevation { get; set; }
        public short SettleTime { get; set; }
        public int DataBits { get; set; }
        public string StopBits { get; set; }
        public string Parity { get; set; }
        public int Speed { get; set; }
        public string Handshake { get; set; }
        public bool SendDateTime { get; set; }
        public bool SkipPrompts { get; set; }
    }
}