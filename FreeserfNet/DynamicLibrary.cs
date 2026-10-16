using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Freeserf
{
    public static class DynamicLibrary
    {
        const int RtldNow = 0x0002;
        const int RtldGlobal = 0x0100;

        // The libraries must be loaded with RTLD_GLOBAL (see Program.Main) which
        // NativeLibrary.Load can't do. So dlopen is used. Its library has different
        // names on different distributions: libdl.so is often only installed with the
        // development packages, libdl.so.2 exists with every glibc and since glibc 2.34
        // dlopen is also part of libc itself (#87).
        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        static extern IntPtr LoadLinuxDl2(string fileName, int flags);

        [DllImport("libdl.so", EntryPoint = "dlopen")]
        static extern IntPtr LoadLinuxDl(string fileName, int flags);

        [DllImport("libc.so.6", EntryPoint = "dlopen")]
        static extern IntPtr LoadLinuxLibc(string fileName, int flags);

        static readonly Func<string, int, IntPtr>[] loaders = [LoadLinuxDl2, LoadLinuxDl, LoadLinuxLibc];
        static int loaderIndex = 0;

        static string GetPath(string fileName, string folder)
        {
            return !string.IsNullOrWhiteSpace(folder) ? Path.Combine(folder, fileName) : fileName;
        }

        static IntPtr LoadLinux(string fileName)
        {
            while (true)
            {
                try
                {
                    return loaders[loaderIndex](fileName, RtldNow | RtldGlobal);
                }
                catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) && loaderIndex < loaders.Length - 1)
                {
                    ++loaderIndex;
                }
            }
        }

        public static IntPtr Load(string dllName, string folder = null)
        {
            return LoadLinux(GetPath($"lib{dllName}.so", folder));
        }
    }
}
