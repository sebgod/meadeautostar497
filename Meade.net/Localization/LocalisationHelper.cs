using System.Globalization;
using System.Threading;
using System.Resources;
using System.Reflection;

namespace ASCOM.Meade.net.Localization
{
    internal class LocalisationHelper
    {
        private const string LocalizationNamespace = "LocalisationTest.Localization.Resources.Localization";
        private readonly ResourceManager _resourceManager;

        public LocalisationHelper()
        {
            _resourceManager = new ResourceManager(LocalizationNamespace, Assembly.GetExecutingAssembly());

            SetLocalisation(CultureInfo.CurrentCulture.Name);
        }

        internal void SetLocalisation(string name)
        {
            var cultureInfo = new CultureInfo(name);

            //CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            //CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }

        internal string GetString(string key)
        {
            return _resourceManager.GetString(key);
        }
    }
}
