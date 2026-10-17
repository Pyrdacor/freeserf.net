using Freeserf.Localization;
using System.Globalization;

namespace Freeserf.Android
{
    internal static class AndroidText
    {
        public static string InitializationFailed(string message) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_InitializationFailed, message);

        public static string FileAlreadyExists(string name) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_FileAlreadyExists, name);

        public static string LoginFailed(int statusCode) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_LoginFailed, statusCode);

        public static string NetworkError(string message) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_NetworkError, message);

        public static string ServiceConnectionFailed(string serviceName) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_ServiceConnectionFailed, serviceName);

        public static string ManifestFileNotFound(string fileName) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_ManifestFileNotFound, fileName);

        public static string DownloadingFile(string fileName, int slice, int totalSlices) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_DownloadingFile, fileName, slice, totalSlices);

        public static string DownloadSucceeded(string fileName) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_DownloadSucceeded, fileName);

        public static string DownloadFailed(string message) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_DownloadFailed, message);

        public static string UnknownCompression(string method) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Android_UnknownCompression, method);
    }
}
