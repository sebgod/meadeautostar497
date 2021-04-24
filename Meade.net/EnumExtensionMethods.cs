using System;
using System.Linq;
using System.Reflection;

namespace ASCOM.Meade.net
{
    public static class EnumExtensionMethods
    {
        public static string GetDescription(this Enum GenericEnum)
        {
            Type genericEnumType = GenericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0]
                    .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute) _Attribs.ElementAt(0)).Description;
                }
            }

            return GenericEnum.ToString();
        }

        public static T GetValueFromDescription<T>( string description) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (value.GetDescription() == description)
                {
                    return value;
                }
            }

            return default;
        }
    }
}