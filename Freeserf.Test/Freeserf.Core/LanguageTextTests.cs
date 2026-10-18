using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Freeserf.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    [DoNotParallelize]
    public class LanguageTextTests
    {
        string directory;
        string snapshot;
        CultureInfo originalResourceCulture;
        CultureInfo originalCulture;
        CultureInfo originalUICulture;

        [TestInitialize]
        public void SaveSettings()
        {
            directory = Path.Combine(Path.GetTempPath(), "freeserf-language-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            snapshot = Path.Combine(directory, "original.cfg");
            originalResourceCulture = Strings.Culture;
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;
            Assert.IsTrue(UserConfig.Save(snapshot));
        }

        [TestCleanup]
        public void RestoreSettings()
        {
            Assert.IsTrue(UserConfig.Load(snapshot));
            Strings.Culture = originalResourceCulture;
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
            Directory.Delete(directory, true);
        }

        [TestMethod]
        [DataRow("system")]
        [DataRow("en")]
        [DataRow("de")]
        public void Language_RoundTripsThroughConfig(string language)
        {
            string filename = Path.Combine(directory, "user.cfg");
            Assert.IsTrue(UserConfig.SaveLanguage(language, filename));
            var config = new ConfigFile();
            Assert.IsTrue(config.Load(filename));
            Assert.AreEqual(language, config.Value("game", "language", ""));

            UserConfig.Game.Language = LanguageSettings.Next(language);
            Assert.IsTrue(UserConfig.Load(filename));
            Assert.AreEqual(language, UserConfig.Game.Language);
            Assert.AreEqual(language == "system" ? LanguageSettings.SystemCulture.Name : language, Strings.Culture.Name);
        }

        [TestMethod]
        public void ExistingConfigWithoutLanguage_UsesSystemDefault()
        {
            string filename = Path.Combine(directory, "legacy.cfg");
            File.WriteAllText(filename, "[game]\noptions = 0\n");
            UserConfig.Game.Language = "de";
            Assert.IsTrue(UserConfig.Load(filename));
            Assert.AreEqual("system", UserConfig.Game.Language);
            Assert.AreEqual(LanguageSettings.SystemCulture, Strings.Culture);
        }

        [TestMethod]
        public void MissingConfig_CreatesPersistedSystemDefault()
        {
            string filename = Path.Combine(directory, "new.cfg");
            UserConfig.Game.Language = "de";
            Assert.IsFalse(UserConfig.Load(filename));
            Assert.AreEqual("system", UserConfig.Game.Language);
            Assert.IsTrue(File.Exists(filename));
            Assert.IsTrue(UserConfig.Load(filename));
            Assert.AreEqual("system", UserConfig.Game.Language);
        }

        [TestMethod]
        [DataRow("unknown")]
        [DataRow("de-DE")]
        [DataRow("")]
        public void UnsupportedConfigLanguage_UsesLoggedSystemFallback(string language)
        {
            string filename = Path.Combine(directory, "invalid.cfg");
            File.WriteAllText(filename, "[game]\nlanguage = " + language + "\n");
            UserConfig.Game.Language = "de";
            Assert.IsTrue(UserConfig.Load(filename));
            Assert.AreEqual("system", UserConfig.Game.Language);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("fr")]
        public void InvalidProgrammaticPreference_IsRejected(string language)
        {
            UserConfig.Game.Language = "en";
            Assert.ThrowsExactly<ArgumentException>(() => UserConfig.Game.Language = language);
            Assert.AreEqual("en", UserConfig.Game.Language);
        }

        [TestMethod]
        public void SaveFailure_RestoresPreviousLanguage()
        {
            UserConfig.Game.Language = "en";
            Assert.IsFalse(UserConfig.SaveLanguage("de", directory));
            Assert.AreEqual("en", UserConfig.Game.Language);
            Assert.AreEqual("Loading…", Strings.Android_Loading);
        }

        [TestMethod]
        public async Task ExplicitLanguage_OverridesCultureOnExistingAndNewThreads()
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            UserConfig.Game.Language = "en";
            Assert.AreEqual("Loading…", Strings.Android_Loading);
            Assert.AreEqual("Loading…", await Task.Run(() => Strings.Android_Loading));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            UserConfig.Game.Language = "de";
            Assert.AreEqual("Lädt…", Strings.Android_Loading);
            Assert.AreEqual("Lädt…", await Task.Run(() => Strings.Android_Loading));
            Assert.AreEqual("Ihre Siedlung wird angegriffen", Strings.Notification_UnderAttack);
            Assert.AreEqual("fr-FR", CultureInfo.CurrentCulture.Name);
        }

        [TestMethod]
        public void SwitchingLanguage_RefreshesResourcesAndRestoresSystemChoice()
        {
            UserConfig.Game.Language = "en";
            int revision = LanguageSettings.Revision;
            UserConfig.Game.Language = "de";
            Assert.IsGreaterThan(revision, LanguageSettings.Revision);
            Assert.AreEqual("Sprache", Strings.Popup_Language);
            Assert.AreEqual("Deutsch", LanguageSettings.DisplayName);

            UserConfig.Game.Language = "en";
            Assert.AreEqual("Language", Strings.Popup_Language);
            Assert.AreEqual("English", LanguageSettings.DisplayName);
            UserConfig.Game.Language = "system";
            Assert.AreEqual(LanguageSettings.SystemCulture, Strings.Culture);

            revision = LanguageSettings.Revision;
            UserConfig.Game.Language = "system";
            Assert.AreEqual(revision, LanguageSettings.Revision);
        }

        [TestMethod]
        [DataRow("system", "en")]
        [DataRow("en", "de")]
        [DataRow("de", "system")]
        public void Selector_CyclesAllSupportedPreferences(string current, string next)
        {
            Assert.AreEqual(next, LanguageSettings.Next(current));
            Assert.AreEqual(current, LanguageSettings.Previous(next));
        }

        [TestMethod]
        public void Selector_RejectsUnsupportedPreferencesInBothDirections()
        {
            Assert.ThrowsExactly<ArgumentException>(() => LanguageSettings.Next("fr"));
            Assert.ThrowsExactly<ArgumentException>(() => LanguageSettings.Previous("fr"));
        }

        [TestMethod]
        [DataRow("en")]
        [DataRow("de")]
        [DataRow("system")]
        public void SelectorLabels_FitBeforeTheButton(string language)
        {
            UserConfig.Game.Language = language;
            Assert.IsTrue(16 + Strings.Popup_Language.Length * 8 <= 136);
            Assert.IsTrue(16 + LanguageSettings.DisplayName.Length * 8 <= 96);
        }
    }
}
