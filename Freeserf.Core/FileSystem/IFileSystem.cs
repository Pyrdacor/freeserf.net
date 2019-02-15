/*
 * IFileSystem.cs - File system interface
 *
 * Copyright (C) 2019 Robert Schneckenhaus<robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net.freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see<http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;

namespace Freeserf.FileSystem
{
    interface IFileSystem
    {
        bool FileExists(string path);
        Stream OpenFile(string path);
    }

    class FileSystemSorter : IComparer<VirtualFileSystem>
    {
        public int Compare(VirtualFileSystem x, VirtualFileSystem y)
        {
            // Higher date values first (= more recent filesystems first)
            return y.ReleaseDate.CompareTo(x.ReleaseDate);
        }
    }

    public static class File
    {
        static readonly PhysicalFileSystem physicalFileSystem = new PhysicalFileSystem();
        static readonly List<VirtualFileSystem> virtualFileSystems = new List<VirtualFileSystem>();

        static File()
        {
            foreach (var virtualFileSystemFile in Directory.GetFiles(Paths.GameDataFolder, "*.vfs"))
            {
                virtualFileSystems.Add(new VirtualFileSystem(virtualFileSystemFile));
            }

            virtualFileSystems.Sort(new FileSystemSorter());
        }

        public static Stream Open(string path)
        {
            if (physicalFileSystem.FileExists(path))
                return physicalFileSystem.OpenFile(path);

            foreach (var virtualFileSystem in virtualFileSystems)
            {
                if (virtualFileSystem.FileExists(path))
                    return virtualFileSystem.OpenFile(path);
            }

            return null;
        }
    }
}
