namespace ASCOM.MeadeAutostar497.Controller
{
    enum FirmwareVersion
    {
        autostar497_30eb,
        autostar497_30ed,
        autostar497_30ee,
        autostar497_31ee,
        autostar497_32ea,
        //PEC added for Polar mounted scopes

        autostar497_32ee,
        autostar497_32eh,
        //Some serial strings fixed.

        autostar497_32ei,
        autostar497_33ef,
        //Some serial strings fixed.

        autostar497_33el,
        autostar497_40eb,
        autostar497_40ee,
        autostar497_40ef,
        autostar497_41ec,
        autostar497_42ed,
        //Get serial command for daylight savings (:GH# returns 0 for disabled 1 for enabled)
        //Set serial command for daylight savings (:SH0# disables, :SH1# enables)

        autostar497_43ea,
        autostar497_43ed,
        autostar497_43eg
        //Added :GW#, :AL#, :AA#, & :AP#
    }
}