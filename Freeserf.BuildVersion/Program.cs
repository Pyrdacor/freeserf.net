using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Freeserf.BuildVersion
{
    class Program
    {
        const string VERSION_PLACEHOLDER = "{BUILD_VERSION}";

        // Usage: Freeserf.BuildVersion <path_to_assembly_with_version> <template_directory_path> <target_path> <configuration_name>
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: Freeserf.BuildVersion <path_to_assembly_with_version> <template_directory_path> <target_path> <configuration_name>");
                Environment.Exit(1);
                return;
            }

            if (args[3] != "Release" && args[3] != "WindowsRelease")
            {
                Console.WriteLine("Skipping debug build version.");
                Environment.Exit(0);
                return;
            }

            string version = AssemblyName.GetAssemblyName(args[0]).Version.ToString();
            version = Regex.Match(version, "^[0-9]+\\.[0-9]+\\.[0-9]+").Value;

            Console.WriteLine($"New version is {version}");

            string targetPath = args[2].TrimEnd('/', '\\');

            foreach (var file in Directory.GetFiles(args[1]))
            {
                Console.Write($"Replacing version in '{Path.GetFileName(file)}' ... ");

                try
                {
                    ReplaceVersion(file, version, targetPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("failed");
                    Console.WriteLine("Reason: " + ex.Message);
                    if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                        Console.WriteLine("StackTrace: " + ex.StackTrace);
                }

                Console.WriteLine("ok");
            }
        }

        static void ReplaceVersion(string file, string version, string targetPath)
        {
            string outputFile = Path.Combine(targetPath, Path.GetFileName(file));

            File.WriteAllText(outputFile, File.ReadAllText(file).Replace(VERSION_PLACEHOLDER, version));
        }
    }
}
