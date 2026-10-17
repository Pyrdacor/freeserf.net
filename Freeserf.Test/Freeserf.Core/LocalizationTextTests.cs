using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
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
        public void GermanCatalog_ValidatesTemplatesAndResolvesRegionalCultures(string cultureName)
        {
            var resources = new ResourceManager(Strings.ResourceManager.BaseName, typeof(Strings).Assembly);
            try
            {
                var german = resources.GetResourceSet(CultureInfo.GetCultureInfo("de"), true, false);
                var english = resources.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, true);
                var culture = CultureInfo.GetCultureInfo(cultureName);

                Assert.IsNotNull(german, "The German satellite resource must be packaged.");
                Assert.IsNotNull(english);

                foreach (DictionaryEntry entry in german)
                {
                    string key = (string)entry.Key;
                    string source = english.GetString(key);
                    Assert.IsNotNull(source, $"Unknown German key: {key}");
                    string translation = (string)entry.Value;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(translation), $"Empty German translation: {key}");
                    Assert.AreEqual(CompositeFormat.Parse(source).MinimumArgumentCount,
                        CompositeFormat.Parse(translation).MinimumArgumentCount, key);
                    CollectionAssert.AreEquivalent(
                        Regex.Matches(source, @"\{(\d+)[,:}]").Select(match => match.Groups[1].Value).ToArray(),
                        Regex.Matches(translation, @"\{(\d+)[,:}]").Select(match => match.Groups[1].Value).ToArray(), key);
                }

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
