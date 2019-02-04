using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

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

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FreeserfForm(args));
            }
            catch (FileNotFoundException ex)
            {
                Log.Error.Write("main", "Exception: " + ex.Message);

                if (ex.StackTrace.Contains("System.Reflection.Assembly.Load") && ex.FileName.Contains("netstandard"))
                {
                    try
                    {
                        MessageBox.Show(".NET Standard 2.0 is missing. Please install it.", ".NET Standard missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch
                    {
                        // ignore
                    }
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error.Write("main", "Exception: " + ex.Message);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
