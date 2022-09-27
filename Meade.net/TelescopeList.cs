namespace ASCOM.Meade.net
{
    public static class TelescopeList
    {
        #region Autostar 497/Audiostar

        public const string Audiostar = "Audiostar"; //This is a synonym for Autostar which can be returned by some of the Audiostar firmware revisions A1f7
        public const string Autostar497 = "Autostar";

        //Autostar/Audiostar firmware revisions
        // ReSharper disable once InconsistentNaming
        public const string Autostar497_30Ee = "30Ee";
        // ReSharper disable once InconsistentNaming
        public const string Autostar497_31Ee = "31Ee";
        // ReSharper disable once InconsistentNaming
        public const string Autostar497_43Eg = "43Eg";

        // ReSharper disable once InconsistentNaming
        public const string AudioStar_A1F7 = "A1F7";
        // ReSharper disable once InconsistentNaming
        public const string AudioStar_A4S4 = "A4S4";
        #endregion

        #region LX200GPS

        // ReSharper disable once InconsistentNaming
        public const string LX200GPS = "LX2001";

        public const string LX200GPS_42F = "4.2f";
        // ReSharper disable once InconsistentNaming
        public const string LX200GPS_42G = "4.2g";
        #endregion

        #region LX200EMC
        // ReSharper disable once InconsistentNaming
        public const string LX200CLASSIC = "LX200 Classic"; //GVP command is not supported!
        #endregion

        #region RCX400
        // ReSharper disable once InconsistentNaming
        public const string RCX400 = "RCX400";

        public const string RCX400_22I = "2.2i";

        #endregion
    }
}
