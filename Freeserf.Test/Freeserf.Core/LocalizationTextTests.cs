using System.Collections;
using System.Globalization;
using System.Resources;
using Freeserf.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class LocalizationTextTests
    {
        [TestMethod]
        [DataRow("de")]
        [DataRow("de-DE")]
        [DataRow("de-AT")]
        [DataRow("de-CH")]
        public void GermanCatalog_UsesEnglishForMissingEntries(string cultureName)
        {
            var resources = new ResourceManager(Strings.ResourceManager.BaseName, typeof(Strings).Assembly);
            try
            {
                var german = resources.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false);
                var english = resources.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, true);
                var culture = CultureInfo.GetCultureInfo(cultureName);

                Assert.IsNotNull(german, "The German satellite resource must be packaged even before translations are added.");
                Assert.IsNotNull(english);

                foreach (DictionaryEntry entry in english)
                {
                    string key = (string)entry.Key;
                    string expected = german.GetString(key) ?? (string)entry.Value;
                    Assert.AreEqual(expected, resources.GetString(key, culture), key);
                }
            }
            finally
            {
                resources.ReleaseAllResources();
            }
        }
    }
}
