/*
 * PhysicalFileSystem.cs - The real filesystem of the OS
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

using System.IO;

namespace Freeserf.FileSystem
{
    class PhysicalFileSystem : IFileSystem
    {
        public bool FileExists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public Stream OpenFile(string path)
        {
            return System.IO.File.OpenRead(path);
        }
    }
}
