using System.ComponentModel;

namespace ASCOM.Meade.net
{
    public enum ParkedBehaviour
    {
        [Description("No Coordinates")]
        NoCoordinates,
        [Description("Last Good Position")]
        LastGoodPosition,
        [Description("Report coordinates as")]
        ReportCoordinates
    }
}