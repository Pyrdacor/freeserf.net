using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
#if LINUX
using System.Runtime.InteropServices;
#endif

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

            if (assemblyPath.EndsWith("dotnet"))
            {
                assemblyPath = Assembly.GetExecutingAssembly().Location;
            }

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
            else
            {
                ExecutablePath = assemblyDirectory;
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
#if LINUX
            // This is needed on linux as it will load the libs with
            // RTLD_GLOBAL instead ot RTLD_LOCAL. Otherwise bassmidi won't
            // work on linux unfortunately.
            // Moreover as we publish freeserf as a single file it will
            // be extracted to a temporary directory. So we will ensure
            // that the audio library is able to find bass at the right spot.
            DllImportResolver resolver = (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
                DynamicLibrary.Load(libraryName, Program.ExecutablePath);
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string audioDllPath = Path.Combine(path, "Freeserf.Audio.dll");
            // Set the resolver for our audio DLL
            NativeLibrary.SetDllImportResolver(Assembly.LoadFrom(audioDllPath), resolver);
#endif

            try
            {
                using (var mainWindow = MainWindow.Create(args))
                {
                    if (mainWindow != null)
                        mainWindow.Run();
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, "Exception: " + ex.Message);
            }
        }
    }
}
