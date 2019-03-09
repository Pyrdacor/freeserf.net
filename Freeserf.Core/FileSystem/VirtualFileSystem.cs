/*
 * VirtualFileSystem.cs - A virtual filesystem represented by a file
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

using System;
using System.Collections.Generic;
using System.IO;

/*
 * The virtual file system starts with a 12 byte header:
 * 
 * 3 bytes header: 'V', 'F', 'S'
 * 1 byte file version: 0 - 255
 * 4 bytes release date:
 *      highest 12 bits (0 - 4096): year from 2000 to 6095
 *      next 4 bits: month (1 - 12)
 *      next 5 bits: day of month (1 - 31)
 *      next 5 bits: hour of day (0 - 23)
 *      next 6 bits: minute of hour (0 - 59)
 *  4 bytes file count
 *  
 *  After the header there is the file dictionary that
 *  contains <file count> entries of 8 bytes each.
 *  
 *  An entry contains the offset (4 bytes) from the
 *  start of the file data and the file length (4 bytes).
 *  The most significant bit of the file length indicates
 *  if the file data is compressed (the remaining 31 bits
 *  are the real file size). The file size DOES include
 *  the path data.
 *  
 *  After that the file data begins. Each file data entry
 *  may either contain the raw file data or the compressed
 *  data. Compression algorithm is always deflate.
 *  
 *  But before the actual file data there is the path name
 *  of that file. Starting with an encoded int that stores
 *  the length and than the characters.
 */

using System.Text;

namespace Freeserf.FileSystem
{
    class VirtualFileSystem : IFileSystem, IDisposable
    {
        const byte CurrentFileVersion = 0;
        Stream fileStream = null;
        readonly Dictionary<string, Stream> files = new Dictionary<string, Stream>();

        public VirtualFileSystem(string path)
        {
            fileStream = System.IO.File.OpenRead(path);

            using (BinaryReader reader = new BinaryReader(fileStream, Encoding.UTF8, true))
            {
                if (reader.ReadByte() != 0x56 || // 'V'
                    reader.ReadByte() != 0x46 || // 'F'
                    reader.ReadByte() != 0x53)   // 'S'
                    throw new ExceptionFreeserf("data", "Invalid virtual file system.");

                byte version = reader.ReadByte();

                if (version > CurrentFileVersion)
                    throw new ExceptionFreeserf("data", "Virtual file system version is not supported.");

                ReleaseDate = DecodeDate(reader);

                uint fileCount = reader.ReadUInt32();
                List<Tuple<uint, uint, bool>> fileEntries = new List<Tuple<uint, uint, bool>>();

                for (long i = 0; i < fileCount; ++i)
                {
                    uint offset = reader.ReadUInt32();
                    uint length = reader.ReadUInt32();
                    bool compressed = (length & 0x80000000) != 0;
                    length &= 0x7fffffff;

                    fileEntries.Add(Tuple.Create(offset, length, compressed));
                }

                long fileDataOffset = fileStream.Position;

                foreach (var entry in fileEntries)
                {
                    long offset = fileStream.Position = fileDataOffset + entry.Item1;

                    string filePath = reader.ReadString();

                    long length = entry.Item2 - (fileStream.Position - offset);

                    var subStream = new Data.SubStream(fileStream, fileStream.Position, length);

                    if (entry.Item3) // compressed
                    {
                        files.Add(filePath, new System.IO.Compression.DeflateStream(subStream, System.IO.Compression.CompressionMode.Decompress));
                    }
                    else
                    {
                        files.Add(filePath, subStream);
                    }
                }
            }
        }

        public bool FileExists(string path)
        {
            return files.ContainsKey(path);
        }

        public Stream OpenFile(string path)
        {
            return files[path];
        }

        public DateTime ReleaseDate
        {
            get;
            set;
        }

        static DateTime DecodeDate(BinaryReader reader)
        {
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;

            byte b = reader.ReadByte();

            year = b;
            year <<= 4;

            b = reader.ReadByte();

            year |= (b >> 4);
            month = b & 0xf;

            b = reader.ReadByte();

            day = b >> 3;
            hour = b & 0x7;
            hour <<= 2;

            b = reader.ReadByte();

            hour |= (b >> 6);
            minute = b & 0x3f;

            try
            {
                return new DateTime(year + 2000, month, day, hour, minute, 0);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ExceptionFreeserf("data", "Invalid release date in virtual file system.");
            }
        }


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    fileStream?.Close();
                    fileStream = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}
