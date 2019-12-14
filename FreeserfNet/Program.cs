using System;
using System.Diagnostics;
using System.IO;

namespace Freeserf
{
    static class Program
    {
        public static string ExecutablePath
        {
            get;
            private set;
        }

        static Program()
        {
            var assemblyPath = Process.GetCurrentProcess().MainModule.FileName;
            var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

            if (FileSystem.Paths.IsWindows())
            {
                if (assemblyDirectory.EndsWith(@"Debug") || assemblyDirectory.EndsWith(@"Release"))
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
            else if (string.IsNullOrWhiteSpace(ExecutablePath)) // this could be caused by mono mkbundle
            {
                ExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.RelativeSearchPath ?? "");
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (var mainWindow = new MainWindow(args))
                {
                    mainWindow.Run();
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Exception: " + ex.Message);
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
