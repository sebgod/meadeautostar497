using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace ASCOM.Meade.net
{
    public static class TelescopeList
    {
        #region Autostar 497/Audiostar

        public static readonly string Autostar497 = "Autostar";

        //Autostar/Audiostar firmware revisions
        public static readonly string Autostar497_30Ee = "30Ee";
        public static readonly string Autostar497_31Ee = "31Ee";
        public static readonly string Autostar497_43Eg = "43Eg";

        #endregion

        #region LX200GPS

        public static readonly string LX200GPS = "LX2001";

        public static readonly string LX200GPS_42G = "4.2G";

        #endregion
    }
}
