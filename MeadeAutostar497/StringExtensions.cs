using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.MeadeAutostar497
{
    public static class StringExtensions
    {
        public static int ToInteger(this string str)
        {
            return int.Parse(str);
        }
    }
}
