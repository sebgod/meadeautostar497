using System;
using System.Reflection;

namespace ASCOM.Meade.net
{
    public class AssemblyInfo
    {
        // The assembly information values.
        //public readonly string Title = string.Empty;
        //public readonly string Description = string.Empty;
        //public readonly string Company = string.Empty;
        public readonly string Product = string.Empty;
        //public readonly string Copyright = string.Empty;
        //public readonly string Trademark = string.Empty;
        public readonly string AssemblyVersion;
        //public readonly string FileVersion = string.Empty;
        //public readonly string Guid = string.Empty;
        //public readonly string NeutralLanguage = string.Empty;
        //public readonly bool IsComVisible;

        // Return a particular assembly attribute value.
        private T GetAssemblyAttribute<T>(Assembly assembly)
            where T : Attribute
        {
            // Get attributes of this type.
            object[] attributes = assembly.GetCustomAttributes(typeof(T), true);

            // If we didn't get anything, return null.
            if (attributes.Length == 0)
                return null;

            // Convert the first attribute value into
            // the desired type and return it.
            return (T)attributes[0];
        }

        // Constructors.
        public AssemblyInfo()
            : this(Assembly.GetExecutingAssembly())
        {
        }

        private AssemblyInfo(Assembly assembly)
        {
            // Get values from the assembly.
            //var titleAttr = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly);
            //if (titleAttr != null)
            //    Title = titleAttr.Title;

            //var assemblyAttr = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly);
            //if (assemblyAttr != null)
            //    Description = assemblyAttr.Description;

            //var companyAttr =GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly);
            //if (companyAttr != null)
            //    Company = companyAttr.Company;

            var productAttr = GetAssemblyAttribute<AssemblyProductAttribute>(assembly);
            if (productAttr != null)
                Product = productAttr.Product;

            //var copyrightAttr = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly);
            //if (copyrightAttr != null)
            //    Copyright = copyrightAttr.Copyright;

            //var trademarkAttr = GetAssemblyAttribute<AssemblyTrademarkAttribute>(assembly);
            //if (trademarkAttr != null)
            //    Trademark = trademarkAttr.Trademark;

            var version = assembly.GetName().Version;
            AssemblyVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";


            //var fileVersionAttr = GetAssemblyAttribute<AssemblyFileVersionAttribute>(assembly);
            //if (fileVersionAttr != null) FileVersion =
            //    fileVersionAttr.Version;

            //var guidAttr = GetAssemblyAttribute<GuidAttribute>(assembly);
            //if (guidAttr != null)
            //    Guid = guidAttr.Value;

            //var languageAttr = GetAssemblyAttribute<NeutralResourcesLanguageAttribute>(assembly);
            //if (languageAttr != null)
            //    NeutralLanguage = languageAttr.CultureName;

            //var comAttr = GetAssemblyAttribute<ComVisibleAttribute>(assembly);
            //if (comAttr != null)
            //    IsComVisible = comAttr.Value;
        }
    }
}
