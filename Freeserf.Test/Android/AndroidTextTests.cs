using System.Globalization;
using Freeserf.Android;
using Freeserf.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Android
{
    [TestClass]
    [DoNotParallelize]
    public class AndroidTextTests
    {
        CultureInfo originalCulture;
        CultureInfo originalUICulture;

        [TestInitialize]
        public void SaveCulture()
        {
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;
        }

        [TestCleanup]
        public void RestoreCulture()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        [TestMethod]
        [DataRow("en-US", false)]
        [DataRow("de-DE", true)]
        [DataRow("de-AT", true)]
        [DataRow("de-CH", true)]
        [DataRow("fr-FR", false)]
        public void FormattedMessages_UseGermanTranslationsOrEnglishFallback(string culture, bool german)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            Assert.AreEqual(german ? "Initialisierung fehlgeschlagen: detail" : "Initialization failed: detail", AndroidText.InitializationFailed("detail"));
            Assert.AreEqual(german ? "Eine Datei namens island.save existiert bereits." : "A file named island.save already exists.",
                AndroidText.FileAlreadyExists("island.save"));
            Assert.AreEqual(german ? "Login fehlgeschlagen (HTTP 401)." : "Login failed (HTTP 401).", AndroidText.LoginFailed(401));
            Assert.AreEqual(german ? "Netzwerkfehler: detail" : "Network error: detail", AndroidText.NetworkError("detail"));
            Assert.AreEqual(german ? "Konnte Verbindung zu 'download_service' nicht öffnen." : "Could not open a connection to 'download_service'.",
                AndroidText.ServiceConnectionFailed("download_service"));
            Assert.AreEqual(german ? "Die Datei SPAE.PA wurde im Spielmanifest nicht gefunden." : "File SPAE.PA was not found in the game manifest.",
                AndroidText.ManifestFileNotFound("SPAE.PA"));
            Assert.AreEqual(german ? "Lade SPAE.PA herunter (2/17)…" : "Downloading SPAE.PA (2/17)…", AndroidText.DownloadingFile("SPAE.PA", 2, 17));
            Assert.AreEqual(german ? "SPAE.PA erfolgreich heruntergeladen." : "SPAE.PA downloaded successfully.", AndroidText.DownloadSucceeded("SPAE.PA"));
            Assert.AreEqual(german ? "Download fehlgeschlagen: detail" : "Download failed: detail", AndroidText.DownloadFailed("detail"));
            Assert.AreEqual(german ? "Unbekannte Kompressionsmethode: xyz" : "Unknown compression method: xyz", AndroidText.UnknownCompression("xyz"));
        }

        [TestMethod]
        [DataRow("en-US", false)]
        [DataRow("de-DE", true)]
        [DataRow("fr-FR", false)]
        public void Labels_UseGermanTranslationsOrEnglishFallback(string culture, bool german)
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            Assert.AreEqual("Freeserf", Strings.Android_AppName);
            Assert.AreEqual(german ? "Lädt…" : "Loading…", Strings.Android_Loading);
            Assert.AreEqual(german ? "Spieldaten bereitstellen" : "Provide game data", Strings.Android_ImportTitle);
            Assert.AreEqual(german ? "Datei auswählen" : "Select file", Strings.Android_SelectFile);
            Assert.AreEqual(german ? "Von Ubisoft Connect herunterladen" : "Download from Ubisoft Connect", Strings.Android_DownloadFromUbisoft);
            Assert.AreEqual(german ? "Alte Spielstände" : "Old saves", Strings.Android_OldSaves);
            Assert.AreEqual("OK", Strings.Android_OK);
            Assert.AreEqual(german ? "unbekannter Fehler" : "unknown error", Strings.Android_UnknownError);
        }

        [TestMethod]
        public void ErrorDetails_AreNotInterpretedAsFormatStrings()
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            const string detail = "{0}\n<error> 'quoted'";

            Assert.AreEqual("Initialization failed: " + detail, AndroidText.InitializationFailed(detail));
            Assert.AreEqual("Network error: " + detail, AndroidText.NetworkError(detail));
            Assert.AreEqual("Download failed: " + detail, AndroidText.DownloadFailed(detail));
        }

        [TestMethod]
        public void Numbers_UseCurrentFormattingCulture()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.AreEqual("Login failed (HTTP ~1).", AndroidText.LoginFailed(-1));
            Assert.AreEqual("Downloading SPAE.PA (~1/2)…", AndroidText.DownloadingFile("SPAE.PA", -1, 2));
        }
    }
}
