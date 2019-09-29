using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

namespace ASCOM.Meade.net
{
    public class AssemblyInfo
    {
        // The assembly information values.
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string Company = string.Empty;
        public string Product = string.Empty;
        public string Copyright = string.Empty;
        public string Trademark = string.Empty;
        public string AssemblyVersion;
        public string FileVersion = string.Empty;
        public string Guid = string.Empty;
        public string NeutralLanguage = string.Empty;
        public bool IsComVisible;

        // Return a particular assembly attribute value.
        public static T GetAssemblyAttribute<T>(Assembly assembly)
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

        public AssemblyInfo(Assembly assembly)
        {
            // Get values from the assembly.
            var titleAttr = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly);
            if (titleAttr != null)
                Title = titleAttr.Title;

            var assemblyAttr = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly);
            if (assemblyAttr != null)
                Description = assemblyAttr.Description;

            var companyAttr =GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly);
            if (companyAttr != null)
                Company = companyAttr.Company;

            var productAttr = GetAssemblyAttribute<AssemblyProductAttribute>(assembly);
            if (productAttr != null)
                Product = productAttr.Product;

            var copyrightAttr = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly);
            if (copyrightAttr != null)
                Copyright = copyrightAttr.Copyright;

            var trademarkAttr = GetAssemblyAttribute<AssemblyTrademarkAttribute>(assembly);
            if (trademarkAttr != null)
                Trademark = trademarkAttr.Trademark;

            var version = assembly.GetName().Version;
            AssemblyVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";


            var fileVersionAttr = GetAssemblyAttribute<AssemblyFileVersionAttribute>(assembly);
            if (fileVersionAttr != null) FileVersion =
                fileVersionAttr.Version;

            var guidAttr = GetAssemblyAttribute<GuidAttribute>(assembly);
            if (guidAttr != null)
                Guid = guidAttr.Value;

            var languageAttr = GetAssemblyAttribute<NeutralResourcesLanguageAttribute>(assembly);
            if (languageAttr != null)
                NeutralLanguage = languageAttr.CultureName;

            var comAttr = GetAssemblyAttribute<ComVisibleAttribute>(assembly);
            if (comAttr != null)
                IsComVisible = comAttr.Value;
        }
    }
}
