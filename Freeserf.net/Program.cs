using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Freeserf
{
    public static class Program
    {
        public static readonly string UpdateUri = "https://github.com/Pyrdacor/freeserf.net/raw/master/updates/Windows/";

        public static string ExecutablePath
        {
            get;
            private set;
        }

        static Program()
        {
            var assemblyPath = Assembly.GetEntryAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

            if (assemblyDirectory.EndsWith(@"\Debug") || assemblyDirectory.EndsWith(@"\Release"))
            {
                string projectFile = Path.GetFileNameWithoutExtension(assemblyPath) + ".csproj";

                var root = new DirectoryInfo(assemblyDirectory);

                while (root.Parent != null)
                {
                    if (File.Exists(Path.Combine(root.FullName, projectFile)))
                        break;

                    root = root.Parent;

                    if (root.Parent == null) // we could not find it (should not happen)
                        ExecutablePath = assemblyDirectory;
                }

                ExecutablePath = root.FullName;
            }
            else
            {
                ExecutablePath = assemblyDirectory;
            }
        }

        static bool CheckForUpdates(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "--no-updates")
                    return false;
            }

            string patcherPath = Path.Combine(ExecutablePath, "FreeserfPatcher.exe");

            if (!File.Exists(patcherPath))
                return false;

            // TODO: connection timeout / async
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(UpdateUri + "recent.txt");
            httpRequest.Timeout = 1300;
            httpRequest.Method = WebRequestMethods.Http.Get;

            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                var responseStream = httpResponse.GetResponseStream();

                byte[] buffer = new byte[24];

                int length = responseStream.Read(buffer, 0, 24);

                if (length == 0)
                    return false;

                string version = Encoding.UTF8.GetString(buffer, 0, length);
                Regex versionRegex = new Regex(@"([0-9]+)\.([0-9]+)\.([0-9]+)", RegexOptions.Compiled);
                var match = versionRegex.Match(version);

                if (match.Success && match.Length == version.Length && match.Groups.Count >= 4)
                {
                    int major = int.Parse(match.Groups[1].Value);
                    int minor = int.Parse(match.Groups[2].Value);
                    int patch = int.Parse(match.Groups[3].Value);

                    var currentVersion = Assembly.GetEntryAssembly().GetName().Version;

                    if (major < currentVersion.Major)
                        return false;

                    if (major == currentVersion.Major)
                    {
                        if (minor < currentVersion.Minor)
                            return false;

                        if (minor == currentVersion.Minor)
                        {
                            if (patch <= currentVersion.Build)
                                return false;
                        }
                    }

                    string patchersArguments = "\"" + Assembly.GetEntryAssembly().Location + "\"" + " " +
                        $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}" + " " +
                        $"{major}.{minor}.{patch}" + " " +
                        string.Join(" ", args);

                    Process.Start(patcherPath, patchersArguments);

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (CheckForUpdates(args))
            {
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FreeserfForm(args));
            }
            catch (Exception ex)
            {
                Log.Error.Write("main", "Exception: " + ex.Message);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                Log.Error.Write("unhandled", "Exception: " + (e.ExceptionObject as Exception).Message);
            }
            else
            {
                Log.Error.Write("unhandled", "Unknown exception type");
            }
        }
    }
}
