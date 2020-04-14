using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Freeserf
{
    public static class DynamicLibrary
    {
        const int RtldNow = 0x0002;
        const int RtldGlobal = 0x0100;

        [DllImport("libdl.so", EntryPoint = "dlopen")]
        static extern IntPtr LoadLinux(string FileName, int Flags = RtldNow | RtldGlobal);

        static string GetPath(string fileName, string folder)
        {
            return !string.IsNullOrWhiteSpace(folder) ? Path.Combine(folder, fileName) : fileName;
        }

        public static IntPtr Load(string dllName, string folder = null)
        {
            return LoadLinux(GetPath($"lib{dllName}.so", folder));
        }
    }
}