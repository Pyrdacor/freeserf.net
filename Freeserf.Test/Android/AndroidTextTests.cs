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
        [DataRow("en-US")]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        public void FormattedMessages_PreserveExistingGermanAndEnglish(string culture)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

            Assert.AreEqual("Initialisierung fehlgeschlagen: detail", AndroidText.InitializationFailed("detail"));
            Assert.AreEqual("A file named island.save already exists.", AndroidText.FileAlreadyExists("island.save"));
            Assert.AreEqual("Login fehlgeschlagen (HTTP 401).", AndroidText.LoginFailed(401));
            Assert.AreEqual("Netzwerkfehler: detail", AndroidText.NetworkError("detail"));
            Assert.AreEqual("Konnte Verbindung zu 'download_service' nicht öffnen.",
                AndroidText.ServiceConnectionFailed("download_service"));
            Assert.AreEqual("Die Datei SPAE.PA wurde im Spielmanifest nicht gefunden.",
                AndroidText.ManifestFileNotFound("SPAE.PA"));
            Assert.AreEqual("Lade SPAE.PA herunter (2/17)…", AndroidText.DownloadingFile("SPAE.PA", 2, 17));
            Assert.AreEqual("SPAE.PA erfolgreich heruntergeladen.", AndroidText.DownloadSucceeded("SPAE.PA"));
            Assert.AreEqual("Download fehlgeschlagen: detail", AndroidText.DownloadFailed("detail"));
            Assert.AreEqual("Unbekannte Kompressionsmethode: xyz", AndroidText.UnknownCompression("xyz"));
        }

        [TestMethod]
        public void Labels_PreserveExistingGermanAndEnglish()
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.AreEqual("Freeserf", Strings.Android_AppName);
            Assert.AreEqual("Lädt…", Strings.Android_Loading);
            Assert.AreEqual("Spieldaten bereitstellen", Strings.Android_ImportTitle);
            Assert.AreEqual("Datei auswählen", Strings.Android_SelectFile);
            Assert.AreEqual("Von Ubisoft Connect herunterladen", Strings.Android_DownloadFromUbisoft);
            Assert.AreEqual("Old saves", Strings.Android_OldSaves);
            Assert.AreEqual("OK", Strings.Android_OK);
            Assert.AreEqual("unbekannter Fehler", Strings.Android_UnknownError);
        }

        [TestMethod]
        public void ErrorDetails_AreNotInterpretedAsFormatStrings()
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            const string detail = "{0}\n<error> 'quoted'";

            Assert.AreEqual("Initialisierung fehlgeschlagen: " + detail, AndroidText.InitializationFailed(detail));
            Assert.AreEqual("Netzwerkfehler: " + detail, AndroidText.NetworkError(detail));
            Assert.AreEqual("Download fehlgeschlagen: " + detail, AndroidText.DownloadFailed(detail));
        }

        [TestMethod]
        public void Numbers_UseCurrentFormattingCulture()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.AreEqual("Login fehlgeschlagen (HTTP ~1).", AndroidText.LoginFailed(-1));
            Assert.AreEqual("Lade SPAE.PA herunter (~1/2)…", AndroidText.DownloadingFile("SPAE.PA", -1, 2));
        }
    }
}
