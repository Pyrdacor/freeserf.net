/*
 * DataSourceAmiga.cs - Amiga data loading
 *
 * Copyright (C) 2016-2017  Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Freeserf.Data
{
    public class ADFReader
    {
        enum SectorType
        {
            Unknown,
            File,
            Directory
        }

        class Sector
        {
            public SectorType Type = SectorType.Unknown;
            public string Name = "";
            public UInt32 NextHashBlock = 0u;
            public UInt32 ParentBlock = 0u;
            public UInt32 FirstExtensionBlock = 0u;
            public UInt32 Offset = UInt32.MaxValue;
            public UInt32 Length = 0u;

            public byte[] GetData(BinaryReader reader, UInt32 fileSize = 0u)
            {
                if (Type != SectorType.File)
                    return null;

                if (fileSize == 0u)
                {
                    reader.BaseStream.Position = Offset + 512 - 188; // offset to file size

                    fileSize = reader.ReadUInt32BigEndian();
                }

                reader.BaseStream.Position = Offset + 24;

                byte[] fileData = new byte[fileSize];
                UInt32[] dataOffsets = new UInt32[72];

                for (int i = 0; i < 72; ++i)
                    dataOffsets[71 - i] = reader.ReadUInt32BigEndian();

                int offset = 0;

                for (int i = 0; i < 72; ++i)
                {
                    if (dataOffsets[i] == 0)
                        break;

                    AppendData(reader, fileData, ref offset, dataOffsets[i]);
                }

                if (FirstExtensionBlock != 0)
                {
                    var extensionSector = ReadSector(reader, FirstExtensionBlock, true);

                    if (extensionSector == null)
                        throw new IOException($"Invalid ADF file data for file \"{Name}\".");

                    var extensionData = extensionSector.GetData(reader, fileSize - (UInt32)offset);

                    if (offset + extensionData.Length != fileSize)
                        throw new IOException($"Invalid ADF file data for file \"{Name}\".");

                    System.Buffer.BlockCopy(extensionData, 0, fileData, offset, extensionData.Length);

                    offset = (int)fileSize;
                }

                if (offset != fileSize)
                    throw new IOException($"Invalid ADF file data for file \"{Name}\".");

                return fileData;
            }

            void AppendData(BinaryReader reader, byte[] buffer, ref int offset, UInt32 block)
            {
                reader.BaseStream.Position = block * 512;

                if (reader.ReadUInt32BigEndian() != 8)
                    throw new IOException("Invalid file data sector header.");

                reader.ReadBytes(8); // skip some bytes

                var size = reader.ReadUInt32BigEndian();

                if (size > 512 - 24)
                    throw new IOException("Invalid file data sector size.");

                reader.ReadBytes(8); // skip some bytes

                var data = reader.ReadBytes((int)size);

                System.Buffer.BlockCopy(data, 0, buffer, offset, data.Length);

                offset += data.Length;
            }
        }

        public static void ExtractADFs(string outputPath, params string[] adfFiles)
        {
            foreach (var adfFile in adfFiles)
            {
                var loadedFiles = LoadADF(adfFile);

                foreach (var loadedFile in loadedFiles)
                {
                    File.WriteAllBytes(Path.Combine(outputPath, loadedFile.Key), loadedFile.Value);
                }
            }
        }

        static UInt32 GetHash(string name)
        {
            UInt32 hash, l;

            l = hash = (UInt32)name.Length;

            for (int i = 0; i < l; ++i)
            {
                hash = hash * 13;
                hash = hash + (UInt32)char.ToUpper(name[i]);
                hash = hash & 0x7ff;
            }

            return hash % 72;
        }

        static Sector GetSector(BinaryReader reader, UInt32[] hashTable, string name)
        {
            var hash = GetHash(name);

            if (hash == 0)
                return null;

            name = name.ToUpper();

            var sector = ReadSector(reader, hashTable[hash]);

            if (sector == null)
                return null;

            while (sector.Name.ToUpper() != name && sector.NextHashBlock != 0)
            {
                sector = ReadSector(reader, sector.NextHashBlock);
            }

            if (sector.Name.ToUpper() != name)
                return null;

            return sector;
        }

        static Sector ReadSector(BinaryReader reader, UInt32 block, bool expectExtension = false)
        {
            if (block == 0)
                return null;

            reader.BaseStream.Position = block * 512;
            var type = reader.ReadUInt32BigEndian();

            if ((type != 2 && !expectExtension) || (type != 16 && expectExtension)) // primary type (T_HEADER or T_LIST)
                throw new IOException("Unexpected ADF sector type.");

            if (reader.ReadUInt32BigEndian() != block)
                throw new IOException("Invalid ADF sector.");

            // move pointer to file size
            reader.BaseStream.Position = block * 512 + 512 - 188;

            var sector = new Sector()
            {
                Offset = block * 512,
                Length = reader.ReadUInt32BigEndian()
            };

            // move pointer to name length
            reader.BaseStream.Position = block * 512 + 512 - 80;

            int nameLength = Math.Min(30, (int)reader.ReadByte());

            sector.Name = Encoding.GetEncoding("iso-8859-1").GetString(reader.ReadBytes(nameLength));

            // move pointer to next hash ptr
            reader.BaseStream.Position = block * 512 + 512 - 16;

            sector.NextHashBlock = reader.ReadUInt32BigEndian();
            sector.ParentBlock = reader.ReadUInt32BigEndian();
            sector.FirstExtensionBlock = reader.ReadUInt32BigEndian();

            var secondaryType = (int)reader.ReadUInt32BigEndian(); // secondary type

            if (secondaryType == -3)
                sector.Type = SectorType.File;
            else if (secondaryType == 2)
                sector.Type = SectorType.Directory;
            else
                sector.Type = SectorType.Unknown;

            return sector;
        }

        static Dictionary<string, byte[]> LoadADF(string adfFile)
        {
            using (var stream = File.OpenRead(adfFile))
            using (var reader = new BinaryReader(stream))
            {
                // Reading bootblock (sectors 1 and 2 -> byte 0 - 1023)
                byte[] header = reader.ReadBytes(4);

                if (header[0] != 'D' || header[1] != 'O' ||
                    header[2] != 'S')
                    throw new IOException("Invalid ADF file header.");

                byte flags = (byte)(header[3] & 0x07);

                if (flags != 0)
                    throw new IOException("Invalid ADF file format." + Environment.NewLine + "Supported is only AmigaDOS 1.2 format (OFS)");

                // Reading rootblock (sector 880 -> offset 0x6e000)
                reader.BaseStream.Position = 0x6e000;

                if (reader.ReadUInt32BigEndian() != 2 || // type = T_HEADER
                    reader.ReadUInt32BigEndian() != 0 || // header_key = unused
                    reader.ReadUInt32BigEndian() != 0 || // high_seq = unused
                    reader.ReadUInt32BigEndian() != 0x48 || // ht_size = 0x48
                    reader.ReadUInt32BigEndian() != 0) // first_data = unused
                    throw new IOException("Invalid ADF file format.");

                reader.ReadUInt32(); // skip checksum

                var hashTable = new UInt32[72];

                for (int i = 0; i < 72; ++i)
                    hashTable[i] = reader.ReadUInt32BigEndian();

                bool bmFlagsValid = reader.ReadUInt32() == 0xFFFFFFFF;

                var bitmapBlockPointers = new UInt32[25];

                for (int i = 0; i < 25; ++i)
                    bitmapBlockPointers[i] = reader.ReadUInt32BigEndian();

                reader.ReadUInt32(); // skip first bitmap extension block (only used for hard disks)
                reader.ReadBytes(12); // skip last root alteration date values
                reader.ReadBytes(32); // skip volume name
                reader.ReadBytes(8); // skip unused bytes
                reader.ReadBytes(12); // skip last disk alteration date values
                reader.ReadBytes(12); // skip filesystem creation date values
                reader.ReadUInt32(); // skip next hash
                reader.ReadUInt32(); // skip parent directory

                if (reader.ReadUInt32BigEndian() != 0 || // extension must be 0
                    reader.ReadUInt32BigEndian() != 1) // block secondary type = ST_ROOT (1)
                    throw new IOException("Invalid ADF file format.");

                var loadedFiles = new Dictionary<string, byte[]>();

                foreach (var file in AmigaFiles)
                {
                    var fileSector = GetSector(reader, hashTable, file);

                    if (fileSector != null)
                        loadedFiles.Add(file, fileSector.GetData(reader));
                }

                return loadedFiles;
            }
        }

        static readonly string[] AmigaFiles = new string[]
        {
            "gfxheader", "gfxfast", "gfxchip", "gfxpics", "sounds", "music"
        };
    }

    public class DataSourceAmiga : DataSourceLegacy
    {
        static readonly byte[] Palette =
        {
            0x00, 0x00, 0x00,   // 0
            0xFF, 0xAA, 0x00,   // 1
            0x00, 0x00, 0x00,   // 2
            0x00, 0xEE, 0xEE,   // 3  // 1st player normal
            0x00, 0x00, 0xBB,   // 4
            0x44, 0x44, 0xDD,   // 5
            0x88, 0x88, 0xFF,   // 6
            0x00, 0xCC, 0xBB,   // 7  // 1st player dark
            0x22, 0x44, 0x00,   // 8
            0x33, 0x55, 0x00,   // 9
            0x33, 0x66, 0x33,   // 10
            0xEE, 0x88, 0xFF,   // 11
            0x44, 0x88, 0x00,   // 12
            0x66, 0x99, 0x00,   // 13
            0x77, 0xBB, 0x44,   // 14
            0xCC, 0x77, 0xDD,   // 15
            0x44, 0x44, 0x44,   // 16
            0x99, 0x99, 0x99,   // 17
            0xFF, 0xFF, 0xFF,   // 18
            0xEE, 0x66, 0x66,   // 19
            0x22, 0x22, 0x22,   // 20
            0x66, 0x66, 0x66,   // 21
            0xCC, 0xCC, 0xCC,   // 22
            0xDD, 0x33, 0x33,   // 23
            0x55, 0x22, 0x00,   // 24
            0x77, 0x33, 0x00,   // 25
            0x99, 0x55, 0x22,   // 26
            0xFF, 0xFF, 0x99,   // 27
            0xBB, 0x88, 0x55,   // 28
            0xDD, 0xAA, 0x77,   // 29
            0xFF, 0xDD, 0xBB,   // 30
            0xDD, 0xDD, 0x00,   // 31
        };

        byte[] PaletteIntro = new byte[]
        {
            0x00, 0x00, 0x00,   // 0
            0x55, 0x22, 0x00,   // 1
            0x77, 0x33, 0x00,   // 2
            0x99, 0x55, 0x22,   // 3
            0xBB, 0x88, 0x55,   // 4
            0xDD, 0xAA, 0x77,   // 5
            0xFF, 0xDD, 0xBB,   // 6
            0x00, 0xCC, 0xBB,   // 7
            0x22, 0x44, 0x00,   // 8
            0x33, 0x55, 0x00,   // 9
            0x33, 0x66, 0x33,   // 10
            0xEE, 0x88, 0xFF,   // 11
            0x44, 0x88, 0x00,   // 12
            0x66, 0x99, 0x00,   // 13
            0x77, 0xBB, 0x44,   // 14
            0xCC, 0x77, 0xDD,   // 15
            0x44, 0x44, 0x44,   // 16
            0x99, 0x99, 0x99,   // 17
            0xFF, 0xFF, 0xFF,   // 18
            0xEE, 0x66, 0x66,   // 19
            0x22, 0x22, 0x22,   // 20
            0x66, 0x66, 0x66,   // 21
            0xCC, 0xCC, 0xCC,   // 22
            0xDD, 0x33, 0x33,   // 23
            0x55, 0x22, 0x00,   // 24
            0x77, 0x33, 0x00,   // 25
            0x99, 0x55, 0x22,   // 26
            0xFF, 0xFF, 0x99,   // 27
            0xBB, 0x88, 0x55,   // 28
            0xDD, 0xAA, 0x77,   // 29
            0xFF, 0xDD, 0xBB,   // 30
            0xDD, 0xDD, 0x00,   // 31
        };

        byte[] PaletteLogo = new byte[]
        {
            0x00, 0x00, 0x00,   // 0
            0x44, 0x66, 0xDD,   // 1
            0x00, 0x00, 0x00,   // 2
            0x00, 0xEE, 0xEE,   // 3
            0x00, 0x00, 0xBB,   // 4
            0x44, 0x44, 0xDD,   // 5
            0x88, 0x88, 0xFF,   // 6
            0x00, 0xCC, 0xBB,   // 7
            0x22, 0x44, 0x00,   // 8
            0x33, 0x55, 0x00,   // 9
            0x33, 0x66, 0x33,   // 10
            0x66, 0x88, 0xEE,   // 11
            0x44, 0x88, 0x00,   // 12
            0x66, 0x99, 0x00,   // 13
            0x77, 0xBB, 0x44,   // 14
            0x00, 0x00, 0xDD,   // 15
            0x44, 0x44, 0x44,   // 16
            0x99, 0x99, 0x99,   // 17
            0xFF, 0xFF, 0xFF,   // 18
            0xEE, 0x66, 0x66,   // 19
            0x22, 0x22, 0x22,   // 20
            0x66, 0x66, 0x66,   // 21
            0xCC, 0xCC, 0xCC,   // 22
            0xDD, 0x33, 0x33,   // 23
            0x55, 0x22, 0x00,   // 24
            0x77, 0x33, 0x00,   // 25
            0x99, 0x55, 0x22,   // 26
            0x55, 0x77, 0xDD,   // 27
            0xBB, 0x88, 0x55,   // 28
            0xDD, 0xAA, 0x77,   // 29
            0xFF, 0xDD, 0xBB,   // 30
            0xDD, 0xDD, 0x00,   // 31
        };

        byte[] PaletteSymbols = new byte[]
        {
            0x00, 0x00, 0x00,   // 0
            0xFF, 0xAA, 0x00,   // 1
            0x00, 0x00, 0x00,   // 2
            0x00, 0xEE, 0xEE,   // 3
            0x00, 0x00, 0xBB,   // 4
            0x44, 0x44, 0xDD,   // 5
            0x88, 0x88, 0xFF,   // 6
            0x00, 0xCC, 0xBB,   // 7
            0x22, 0x44, 0x00,   // 8
            0x33, 0x55, 0x00,   // 9
            0x33, 0x66, 0x33,   // 10
            0xEE, 0x88, 0xFF,   // 11
            0x44, 0x88, 0x00,   // 12
            0x66, 0x99, 0x00,   // 13
            0x77, 0xBB, 0x44,   // 14
            0xCC, 0x77, 0xDD,   // 15
            0x44, 0x44, 0x44,   // 16
            0x99, 0x99, 0x99,   // 17
            0xFF, 0xFF, 0xFF,   // 18
            0xEE, 0x66, 0x66,   // 19
            0x22, 0x22, 0x22,   // 20
            0x66, 0x66, 0x66,   // 21
            0xCC, 0xCC, 0xCC,   // 22
            0xDD, 0x33, 0x33,   // 23
            0x55, 0x22, 0x00,   // 24
            0x77, 0x33, 0x00,   // 25
            0x99, 0x55, 0x22,   // 26
            0xFF, 0xFF, 0x99,   // 27
            0xBB, 0x88, 0x55,   // 28
            0xDD, 0xAA, 0x77,   // 29
            0xFF, 0xDD, 0xBB,   // 30
            0xDD, 0xDD, 0x00,   // 31
        };

        byte[] Palette2 = new byte[]
        {
            0x00, 0x00, 0x00,  // 0
            0x44, 0x44, 0xEE,  // 1
            0x55, 0xCC, 0x00,  // 2
            0x88, 0xFF, 0x00,  // 3
            0xCC, 0x33, 0x33,  // 4
            0x44, 0x44, 0x44,  // 5
            0x99, 0x99, 0x99,  // 6
            0xFF, 0xFF, 0xFF,  // 7
            0x33, 0x33, 0x33,  // 8
            0xCC, 0xCC, 0xCC,  // 9
            0x55, 0x22, 0x00,  // 10
            0x77, 0x33, 0x11,  // 11
            0x99, 0x55, 0x22,  // 12
            0xBB, 0x88, 0x55,  // 13
            0xDD, 0xAA, 0x77,  // 14
            0xFF, 0xDD, 0xBB,  // 15
            0x00, 0x00, 0x00,  // 16 ???
            0x00, 0x00, 0x00,  // 17 ???
            0x00, 0x00, 0x00,  // 18 ???
            0x00, 0x00, 0x00,  // 19 ???
            0x00, 0x00, 0x00,  // 20 ???
            0xFF, 0xFF, 0x00,  // 21
            0xCC, 0xAA, 0x11,  // 22
            0xAA, 0x77, 0x11,  // 23
            0x00, 0x00, 0x00,  // 24 ???
            0xFF, 0xFF, 0x00,  // 25
            0xCC, 0xAA, 0x11,  // 26
            0xAA, 0x77, 0x11,  // 27
            0x00, 0x00, 0x00,  // 28 ???
            0x00, 0x00, 0x00,  // 29 ???
            0x00, 0x00, 0x00,  // 30 ???
            0x00, 0x00, 0x00,  // 31 ???
        };

        byte[] Palette3 = new byte[]
        {
            0x00, 0x00, 0x00,   // 0
            0xFF, 0xAA, 0x00,   // 1
            0x00, 0x00, 0x00,   // 2
            0xFF, 0x00, 0xEE,   // 3  // 1st player normal
            0x00, 0x00, 0xBB,   // 4
            0x44, 0x44, 0xDD,   // 5
            0x88, 0x88, 0xFF,   // 6
            0xFF, 0x00, 0xBB,   // 7  // 1st player dark
            0x22, 0x44, 0x00,   // 8
            0x33, 0x55, 0x00,   // 9
            0x33, 0x66, 0x33,   // 10
            0xEE, 0x88, 0xFF,   // 11
            0x44, 0x88, 0x00,   // 12
            0x66, 0x99, 0x00,   // 13
            0x77, 0xBB, 0x44,   // 14
            0xCC, 0x77, 0xDD,   // 15
            0x44, 0x44, 0x44,   // 16
            0x99, 0x99, 0x99,   // 17
            0xFF, 0xFF, 0xFF,   // 18
            0xEE, 0x66, 0x66,   // 19
            0x22, 0x22, 0x22,   // 20
            0x66, 0x66, 0x66,   // 21
            0xCC, 0xCC, 0xCC,   // 22
            0xDD, 0x33, 0x33,   // 23
            0x55, 0x22, 0x00,   // 24
            0x77, 0x33, 0x00,   // 25
            0x99, 0x55, 0x22,   // 26
            0xFF, 0xFF, 0x99,   // 27
            0xBB, 0x88, 0x55,   // 28
            0xDD, 0xAA, 0x77,   // 29
            0xFF, 0xDD, 0xBB,   // 30
            0xDD, 0xDD, 0x00,   // 31
        };

        public class SpriteAmiga : Sprite
        {
            public SpriteAmiga(uint width, uint height)
            {
                this.width = width;
                this.height = height;

                data = new byte[width * height * 4];
            }

            public void SetDelta(int x, int y)
            {
                deltaX = x;
                deltaY = y;
            }

            public void SetOffset(int x, int y)
            {
                offsetX = x;
                offsetY = y;
            }

            unsafe public SpriteAmiga GetAmigaMasked(Sprite mask)
            {
                if (mask.Width > width)
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Failed to apply mask");
                }

                SpriteAmiga masked = new SpriteAmiga(mask.Width, mask.Height);

                fixed (byte* pointer = data)
                fixed (byte* maskPointer = mask.GetData())
                fixed (byte* maskedPointer = masked.GetData())
                {
                    uint* pos = (uint*)maskedPointer;

                    uint* sBeg = (uint*)pointer;
                    uint* sPos = sBeg;
                    uint* sEnd = sBeg + width * height;
                    uint sDelta = width - masked.Width;

                    uint* mPos = (uint*)maskPointer;

                    for (uint y = 0; y < masked.Height; ++y)
                    {
                        for (uint x = 0; x < masked.Width; ++x)
                        {
                            if (sPos >= sEnd)
                            {
                                sPos = sBeg;
                            }

                            *pos++ = *sPos++ & *mPos++;
                        }

                        sPos += sDelta;
                    }
                }

                return masked;
            }

            public void Clear()
            {
            }

            unsafe public Color* GetWritableData()
            {
                fixed (byte* pointer = data)
                {
                    return (Color*)pointer;
                }
            }

            unsafe public void MakeTransparent(uint rc = 0)
            {
                fixed (byte* pointer = data)
                {
                    uint* p = (uint*)pointer;

                    for (uint y = 0; y < height; ++y)
                    {
                        for (uint x = 0; x < width; ++x)
                        {
                            if ((*p & 0x00FFFFFF) == rc)
                            {
                                *p = 0x00000000;
                            }

                            ++p;
                        }
                    }
                }
            }

            unsafe public SpriteAmiga MergeHorizontaly(SpriteAmiga right)
            {
                if (right.Height != height)
                {
                    return null;
                }

                SpriteAmiga result = new SpriteAmiga(width + right.Width, height);

                fixed (byte* pointer = data)
                fixed (byte* rightPointer = right.GetData())
                fixed (byte* resPointer = result.GetData())
                {
                    uint* srcLeft = (uint*)pointer;
                    uint* srcRight = (uint*)rightPointer;
                    uint* res = (uint*)resPointer;

                    for (uint y = 0; y < height; ++y)
                    {
                        for (uint x = 0; x < width; ++x)
                        {
                            *res++ = *srcLeft++;
                        }

                        for (uint x = 0; x < right.Width; ++x)
                        {
                            *res++ = *srcRight++;
                        }
                    }
                }

                return result;
            }

            unsafe public SpriteAmiga SplitHorizontaly(bool returnRight)
            {
                uint res_width = width / 2;
                SpriteAmiga s = new SpriteAmiga(res_width, height);

                fixed (byte* pointer = data)
                fixed (byte* sPointer = s.GetData())
                {
                    uint* src = (uint*)pointer;
                    uint* res = (uint*)sPointer;

                    if (returnRight)
                    {
                        src += res_width;
                    }

                    for (uint y = 0; y < height; ++y)
                    {
                        for (uint x = 0; x < res_width; ++x)
                        {
                            *res++ = *src++;
                        }

                        src += res_width;
                    }
                }

                return s;
            }
        }

        public DataSourceAmiga(string path)
            : base(path)
        {
        }

        public override string Name => "Amiga";
        public override uint Scale => 1;
        public override uint BPP => 5;

        public override bool CheckMusic()
        {
            return CheckFiles("music");
        }

        public override bool CheckSound()
        {
            return CheckFiles("sounds");
        }

        public override bool CheckGraphics()
        {
            var dataFiles = new string[]
            {
                "gfxheader",    // catalog file
                "gfxfast",      // fast graphics file
                "gfxchip"       // chip graphics file
            };

            return CheckFiles(dataFiles);
        }

        private bool CheckFiles(params string[] dataFiles)
        {
            bool found = true;

            foreach (var fileName in dataFiles)
            {
                var cp = path + '/' + fileName;

                Log.Info.Write(ErrorSystemType.Data, $"Looking for game data in '{cp}'...");

                if (!CheckFile(cp))
                {
                    Log.Info.Write(ErrorSystemType.Data, $"File '{cp}' not found.");

                    found = false;
                    break;
                }
            }

            if (!found)
            {
                var adfFiles = Directory.GetFiles(path, "*.adf");

                Log.Info.Write(ErrorSystemType.Data, $"Looking for ADF files in '{path}'...");

                if (adfFiles.Length == 0)
                {
                    Log.Info.Write(ErrorSystemType.Data, "No ADF files found.");

                    return false;
                }

                try
                {
                    Log.Info.Write(ErrorSystemType.Data, "ADF files found. Trying to extract them...");

                    ADFReader.ExtractADFs(path, adfFiles);

                    foreach (var fileName in dataFiles)
                    {
                        var cp = path + '/' + fileName;

                        Log.Info.Write(ErrorSystemType.Data, $"Looking for game data again in '{cp}'...");

                        if (!CheckFile(cp))
                        {
                            Log.Info.Write(ErrorSystemType.Data, $"File '{cp}' not found.");

                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug.Write(ErrorSystemType.Data, "Exception while extracting ADF files: " + ex.Message);

                    return false;
                }
            }

            return true;
        }

        public override bool Check()
        {
            return CheckMusic() && CheckSound() && CheckGraphics();
        }

        public override DataLoadResult Load()
        {
            bool graphicsLoadError = false;

            try
            {
                gfxFast = new Buffer(path + "/gfxfast", Endian.Endianess.Big);
                gfxFast = Decode(gfxFast);
                gfxFast = Unpack(gfxFast);

                Log.Debug.Write(ErrorSystemType.Data, $"Data file 'gfxfast' loaded (size = {gfxFast.Size})");
            }
            catch (Exception)
            {
                Log.Debug.Write(ErrorSystemType.Data, "Failed to load 'gfxfast'");
                graphicsLoadError = true;
            }

            if (!graphicsLoadError)
            {
                try
                {
                    gfxChip = new Buffer(path + "/gfxchip", Endian.Endianess.Big);
                    gfxChip = Decode(gfxChip);
                    gfxChip = Unpack(gfxChip);
                    Log.Debug.Write(ErrorSystemType.Data, $"Data file 'gfxchip' loaded (size = {gfxChip.Size})");
                }
                catch (Exception)
                {
                    Log.Debug.Write(ErrorSystemType.Data, "Failed to load 'gfxchip'");
                    graphicsLoadError = true;
                }
            }

            if (!graphicsLoadError)
            {
                Buffer gfxHeader = null;

                try
                {
                    gfxHeader = new Buffer(path + "/gfxheader", Endian.Endianess.Big);
                }
                catch (Exception)
                {
                    Log.Debug.Write(ErrorSystemType.Data, "Failed to load 'gfxheader'");
                    graphicsLoadError = true;
                }

                if (!graphicsLoadError)
                {
                    // Prepare icons catalog
                    uint iconCatalogOffset = gfxHeader.Pop<ushort>();
                    uint iconCatalogSize = gfxHeader.Pop<ushort>();
                    Buffer iconCatalogTemp = gfxFast.GetSubBuffer(iconCatalogOffset * 4, iconCatalogSize * 4);

                    for (uint i = 0; i < iconCatalogSize; ++i)
                    {
                        uint offset = iconCatalogTemp.Pop<uint>();

                        iconCatalog.Add(offset);
                    }

                    // Prepare data pointer bases
                    dataPointers[1] = gfxFast;   // Animations
                    dataPointers[2] = gfxFast;   // Ground masks catalog (4 * 81)
                    dataPointers[3] = gfxFast;   // Path sprites catalog (4 * 27)
                    dataPointers[4] = gfxFast;   // Ground sprites catalog (4 * 32)
                    dataPointers[5] = gfxChip;   // ?
                    dataPointers[6] = gfxFast;   // Map objects catalog (4 * 194)
                    dataPointers[7] = gfxFast;   // Hud multiplayer
                    dataPointers[8] = gfxChip;   // Borders
                    dataPointers[9] = gfxChip;   // Waves
                    dataPointers[10] = gfxChip;  // Popup frame horizontal
                    dataPointers[11] = gfxFast;  // Popup frame vertical
                    dataPointers[12] = gfxFast;  // Cursor
                    dataPointers[13] = gfxFast;  // Icons catalog
                    dataPointers[14] = gfxFast;  // Font data (8 * 44)
                    dataPointers[15] = gfxFast;  // Game objects catalog
                    dataPointers[16] = gfxFast;  // Panel buttons (17 images)
                    dataPointers[17] = gfxFast;  // Rotated star catalog
                    dataPointers[18] = gfxFast;  // Hud
                    dataPointers[19] = gfxFast;  // Serf torse+arms catalog
                    dataPointers[20] = gfxFast;  // Serf heads
                    dataPointers[21] = gfxChip;  // Screen top
                    dataPointers[22] = gfxChip;  // Screen sides (2 * 1864)
                    dataPointers[23] = gfxFast;  // Title (1 * 43200)

                    for (uint i = 1; i < gfxHeader.Size / 4; ++i)
                    {
                        uint blackOffset = gfxHeader.Pop<uint>();

                        // Log.Warn.Write("data", $"Block {i} : {blackOffset}");

                        dataPointers[i] = dataPointers[i].GetTail(blackOffset);
                    }
                }
            }

            var result = DataLoadResult.NothingLoaded;

            try
            {
                sound = new Buffer(path + "/sounds");
                sound = Decode(sound);
                result |= DataLoadResult.SoundLoaded;
            }
            catch (Exception)
            {
                Log.Warn.Write(ErrorSystemType.Data, "Failed to load 'sounds'");
                sound = null;
            }

            try
            {
                GetMusic(0);
                result |= DataLoadResult.MusicLoaded;
            }
            catch (Exception)
            {
                Log.Warn.Write(ErrorSystemType.Data, "Failed to load 'music'");
                music = null;
            }

            if (!graphicsLoadError)
            {
                try
                {
                    Buffer gfxPics = new Buffer(path + "/gfxpics", Endian.Endianess.Big);

                    for (uint i = 0; i < 14; ++i)
                    {
                        uint offset = gfxPics.Pop<uint>();
                        uint size = gfxPics.Pop<uint>();

                        pictures[i] = gfxPics.GetSubBuffer(28 * 4 + offset, size);
                        pictures[i] = Decode(pictures[i]);
                        pictures[i] = Unpack(pictures[i]);
                    }

                    Log.Debug.Write(ErrorSystemType.Data, $"Data file 'gfxpics' loaded (size = {gfxPics.Size})");
                }
                catch (Exception)
                {
                    Log.Warn.Write(ErrorSystemType.Data, "Failed to load 'gfxpics'");
                }

                if (LoadAnimationTable(dataPointers[1].GetSubBuffer(0, 30528)))
                    result |= DataLoadResult.GraphicsLoaded;
            }

            loaded = result != DataLoadResult.NothingLoaded;

            return result;
        }

        public override Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, uint index)
        {
            Sprite sprite = null;

            switch (resource)
            {
                case Data.Resource.ArtLandscape:
                    break;
                case Data.Resource.SerfShadow:
                    {
                        ushort[] shadow = new ushort[6]
                        {
                            Endian.Betoh<ushort>(0x01C0), Endian.Betoh<ushort>(0x07C0),
                            Endian.Betoh<ushort>(0x1F80), Endian.Betoh<ushort>(0x7F00),
                            Endian.Betoh<ushort>(0xFE00), Endian.Betoh<ushort>(0xFC00)
                        };

                        Buffer data = new Buffer(shadow);
                        SpriteAmiga m = DecodeMaskSprite(data, 16, 6);

                        m.FillMasked(new Sprite.Color { Blue = 0x00, Green = 0x00, Red = 0x00, Alpha = 0x80 });
                        m.SetDelta(2, 0);
                        m.SetOffset(-2, -7);

                        sprite = m;

                        break;
                    }
                case Data.Resource.DottedLines:
                    break;
                case Data.Resource.ArtFlag:
                    break;
                case Data.Resource.ArtBox:
                    if ((index < pictures.Length) && pictures[index] != null)
                    {
                        sprite = DecodeInterlasedSprite(pictures[index], 16, 144, 0, 0, Palette);
                    }
                    break;
                case Data.Resource.CreditsBg:
                    sprite = DecodeInterlasedSprite(dataPointers[23], 40, 200, 0, 0, PaletteIntro);
                    break;
                case Data.Resource.Logo:
                    {
                        Buffer data = gfxFast.GetTail(188356);
                        sprite = DecodeInterlasedSprite(data, 64 / 8, 96, 0, 0, PaletteLogo);
                        break;
                    }
                case Data.Resource.Symbol:
                    {
                        Buffer data = gfxFast.GetTail(178116 + (640 * index));
                        sprite = DecodeInterlasedSprite(data, 32 / 8, 32, 0, 0, PaletteSymbols);
                        break;
                    }
                case Data.Resource.MapMaskUp:
                    sprite = GetGroundMaskSprite(index);
                    break;
                case Data.Resource.MapMaskDown:
                    {
                        Sprite mask = GetGroundMaskSprite(index);

                        if (mask != null)
                        {
                            SpriteAmiga s = GetMirroredHorizontalySprite(mask);
                            s.SetOffset(0, -((int)s.Height - 1));
                            sprite = s;
                        }

                        break;
                    }
                case Data.Resource.PathMask:
                    sprite = GetPathMaskSprite(index);
                    break;
                case Data.Resource.MapGround:
                    {
                        SpriteAmiga s;

                        if (index == 32u)
                        {
                            s = new SpriteAmiga(32, 21);
                            var c = new Sprite.Color { Blue = 0xBB, Green = 0x00, Red = 0x00, Alpha = 0xFF };
                            s.Fill(c);
                        }
                        else
                        {
                            s = GetGroundSprite(index);
                        }

                        sprite = s;

                        break;
                    }
                case Data.Resource.PathGround:
                    sprite = GetGroundSprite(index);
                    break;
                case Data.Resource.GameObject:
                    sprite = GetGameObjectSprite(15, index + 1);
                    break;
                case Data.Resource.FrameTop:
                    if (index < 2)
                    {
                        sprite = DecodeAmigaSprite(dataPointers[22].GetTail(1864 * index), 2, 233, Palette);
                    }
                    else if (index == 2)
                    {
                        sprite = DecodePlannedSprite(dataPointers[21], 39, 8, 24, 24, Palette);
                    }
                    else if (index == 3)
                    {
                        SpriteAmiga left = DecodeInterlasedSprite(dataPointers[7], 2, 216, 0, 0, Palette);
                        SpriteAmiga right = DecodeInterlasedSprite(dataPointers[7].GetTail(2160), 2, 216, 0, 0, Palette);
                        sprite = left.MergeHorizontaly(right);
                    }
                    break;
                case Data.Resource.MapBorder:
                    {
                        Buffer data = dataPointers[8].GetTail(index * 120);
                        SpriteAmiga s = DecodeInterlasedSprite(data, 2, 6, 0, 0, Palette);
                        data = data.GetTail(60);
                        SpriteAmiga m = DecodeInterlasedSprite(data, 2, 6, 0, 0, Palette);
                        m.MakeTransparent();
                        sprite = s.GetMasked(m);
                        break;
                    }
                case Data.Resource.MapWaves:
                    {
                        Buffer data = dataPointers[9].GetTail(index * 228);
                        SpriteAmiga s = DecodeInterlasedSprite(data, 6, 19, 28, 5, Palette);
                        s.MakeTransparent(0x0000BB);
                        s.SetDelta(1, 0);
                        sprite = s;
                        break;
                    }
                case Data.Resource.FramePopup:
                    if (index == 0)
                    {
                        sprite = DecodeInterlasedSprite(dataPointers[10], 18, 9, 0, 0, Palette, 1);
                    }
                    else if (index == 1)
                    {
                        sprite = DecodeInterlasedSprite(dataPointers[10].GetTail(972), 18, 7, 0, 0, Palette, 1);
                    }
                    else
                    {
                        SpriteAmiga s = DecodeInterlasedSprite(dataPointers[11], 2, 144, 0, 0, Palette);
                        sprite = s.SplitHorizontaly(index == 3);
                    }
                    break;
                case Data.Resource.Indicator:
                    //      sprite = DecodePlannedSprite(reinterpret_cast<uint8_t*>(gfxfast) +
                    //                                     43076, 1, 7, 0, 0);
                    //      if (index > 3) {
                    //        index -= 4;
                    //      }
                    //    sprite = decode_amiga_sprite(reinterpret_cast<uint8_t*>(gfxfast) + 43076,
                    //                                 5, 7, palette);  // 5 indicators WTF?
                    //    10 indicators WTF?
                    sprite = DecodeAmigaSprite(gfxFast.GetTail(44676), 10, 7, Palette2);
                    break;
                case Data.Resource.Font:
                    {
                        Buffer data = dataPointers[14].GetSubBuffer(index * 8, 8);
                        SpriteAmiga s = DecodeMaskSprite(data, 8, 8);
                        return Tuple.Create<Sprite, Sprite>(s, null);
                    }
                case Data.Resource.FontShadow:
                    {
                        Buffer data = dataPointers[14].GetSubBuffer(index * 8, 8);
                        SpriteAmiga s = DecodeMaskSprite(data, 8, 8);
                        return Tuple.Create<Sprite, Sprite>(MakeShadowFromSymbol(s), null);
                    }
                case Data.Resource.Icon:
                    sprite = GetIconScprite(index);
                    break;
                case Data.Resource.MapObject:
                    if ((index >= 128) && (index <= 143))
                    {
                        // Flag sprites
                        uint flagFrame = (index - 128) % 4;
                        Sprite s1 = GetMapObjectSprite(128 + flagFrame);
                        Sprite s2 = GetMapObjectSprite(128 + flagFrame + 4);

                        return SeparateSprites(s1, s2);
                    }

                    sprite = GetMapObjectSprite(index);
                    break;
                case Data.Resource.MapShadow:
                    sprite = GetMapObjectSprite(index);
                    break;
                case Data.Resource.PanelButton:
                    if (index < 17)
                    {
                        sprite = GetMenuSprite(index, dataPointers[16], 32, 32, 16, 0);
                    }
                    else
                    {
                        index -= 17;
                        uint[] backs = new uint[] { 3, 4, 10, 12, 14, 16, 2, 8 };
                        SpriteAmiga s = GetMenuSprite(backs[index], dataPointers[16], 32, 32, 16, 0);
                        Buffer lData = GetDataFromCatalog(17, 0, gfxChip);
                        Buffer rData = GetDataFromCatalog(17, 16, gfxChip);

                        SpriteAmiga left = DecodeInterlasedSprite(lData, 2, 29, 28, 20, Palette2);
                        left.MakeTransparent();

                        SpriteAmiga right = DecodeInterlasedSprite(rData, 2, 29, 28, 20, Palette2);
                        right.MakeTransparent();

                        SpriteAmiga star = left.MergeHorizontaly(right);
                        s.Stick(star, 0, 1);

                        sprite = s;
                    }
                    break;
                case Data.Resource.FrameBottom:
                    if (index == 0)
                    {
                        SpriteAmiga s = GetHudSprite(6);
                        sprite = s.SplitHorizontaly(true);
                    }
                    else if (index == 1)
                    {
                        SpriteAmiga s = GetHudSprite(6);
                        sprite = s.SplitHorizontaly(false);
                    }
                    else if (index == 2)
                    {
                        sprite = GetHudSprite(18);
                    }
                    else if (index == 3) // this is not present in amiga data but this could be a fully transparent graphic as well
                    {
                        sprite = new Sprite(8u, 12u);
                    }
                    else if (index == 4)
                    {
                        sprite = GetHudSprite(19);
                    }
                    else if (index == 5) // this is not present in amiga data but this could be a fully transparent graphic as well
                    {
                        sprite = new Sprite(8u, 10u);
                    }
                    else if (index == 6)
                    {
                        sprite = GetHudSprite(17);
                    }
                    else if ((index > 6) && (index < 17))
                    {
                        sprite = GetHudSprite(7 + (index - 7));
                    }
                    else if (index > 19)
                    {
                        sprite = GetHudSprite(index - 20);
                    }
                    break;
                case Data.Resource.SerfTorso:
                    {
                        Sprite s1 = GetTorsoSprite(index, Palette);
                        Sprite s2 = GetTorsoSprite(index, Palette3);
                        return SeparateSprites(s1, s2);
                    }
                case Data.Resource.SerfHead:
                    sprite = GetGameObjectSprite(20, index);
                    break;
                case Data.Resource.FrameSplit:
                    break;
                case Data.Resource.Cursor:
                    {
                        Buffer data = dataPointers[12];
                        SpriteAmiga s = DecodeInterlasedSprite(data, 2, 16, 28, 16, Palette);
                        s.MakeTransparent(0x00444444);
                        sprite = s;
                        break;
                    }
                default:
                    break;
            }

            return Tuple.Create<Sprite, Sprite>(null, sprite);
        }

        struct SoundStruct
        {
            public uint Offset;
            public uint Size;

            public SoundStruct(uint offset, uint size)
            {
                Offset = offset;
                Size = size;
            }
        }

        static readonly SoundStruct[] SoundInfo = new[]
        {
            new SoundStruct(    0,     0),
            new SoundStruct(    0,  2704),
            new SoundStruct( 2704,  1134),
            new SoundStruct(    0,     0),
            new SoundStruct( 3838,  2420),
            new SoundStruct(    0,     0),
            new SoundStruct( 6258,  1014),
            new SoundStruct(    0,     0),
            new SoundStruct( 7272,    78),
            new SoundStruct(    0,     0),
            new SoundStruct( 7350,  2114),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct( 9464,  2644),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(12108,  2258),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(14366,  1426),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(15792,  4108),
            new SoundStruct(    0,     0),
            new SoundStruct(19900,   894),
            new SoundStruct(    0,     0),
            new SoundStruct(20794,  1524),
            new SoundStruct(    0,     0),
            new SoundStruct(22318,  1088),
            new SoundStruct(    0,     0),
            new SoundStruct(23406,  1774),
            new SoundStruct(    0,     0),
            new SoundStruct(25180,  1094),
            new SoundStruct(    0,     0),
            new SoundStruct(26274,   780),
            new SoundStruct(    0,     0),
            new SoundStruct(27054,   484),
            new SoundStruct(    0,     0),
            new SoundStruct(27538,  1944),
            new SoundStruct(27538,  1944),
            new SoundStruct(29482,   916),
            new SoundStruct(    0,     0),
            new SoundStruct(30398,   470),
            new SoundStruct(    0,     0),
            new SoundStruct(30868,   608),
            new SoundStruct(    0,     0),
            new SoundStruct(31476,  1894),
            new SoundStruct(    0,     0),
            new SoundStruct(33370,  1174),
            new SoundStruct(    0,     0),
            new SoundStruct(34544,   300),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(34844,   682),
            new SoundStruct(    0,     0),
            new SoundStruct(35526,  1170),
            new SoundStruct(    0,     0),
            new SoundStruct(36696,  2294),
            new SoundStruct(    0,     0),
            new SoundStruct(38990,  3388),
            new SoundStruct(    0,     0),
            new SoundStruct(42378,  2670),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(45048,  3340),
            new SoundStruct(48388,   954),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(49342,  3540),
            new SoundStruct(    0,     0),
            new SoundStruct(52882,  5868),
            new SoundStruct(    0,     0),
            new SoundStruct(58750,  4436),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(63186,  7334),
            new SoundStruct(    0,     0),
            new SoundStruct(70520, 11304),
            new SoundStruct(    0,     0),
            new SoundStruct(81824, 14874),
            new SoundStruct(    0,     0),
            new SoundStruct(96698, 15312),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0),
            new SoundStruct(    0,     0)
        };

        public override Buffer GetSound(uint index)
        {
            Buffer data = GetSoundData(index);

            if (data == null)
            {
                Log.Error.Write(ErrorSystemType.Data, $"Sound sample with index {index} not present.");
                return null;
            }

            return data;
        }

        public override Buffer GetMusic(uint index)
        {
            if (music != null)
            {
                return music;
            }

            Buffer data;

            try
            {
                data = new Buffer(path + "/music");
                data = Decode(data);
                data = Unpack(data);
            }
            catch
            {
                Log.Warn.Write(ErrorSystemType.Data, "Failed to load 'music'");
                return null;
            }

            // Amiga music file starts with music-player code, we must drop it
            string str;
            int pos = 0;

            unsafe
            {
                // it starts at 1080 or later
                str = Encoding.ASCII.GetString(data.Data + 1080, (int)data.Size - 1080);
            }

            pos = str.IndexOf("M!K!");

            if (pos == -1)
                return null;

            // as we searched from 1080 and the offset to M!K! from the beginning is also 1080
            // the correct data start offset is the position of M!K!

            return music = new Buffer(data.GetTail((uint)pos).Unfix(), Endian.Endianess.Big);
        }

        Buffer gfxFast;
        Buffer gfxChip;
        Buffer sound;
        Buffer music;
        Buffer[] dataPointers = new Buffer[24];
        Buffer[] pictures = new Buffer[14];
        List<uint> iconCatalog = new List<uint>();

        Buffer Decode(Buffer data)
        {
            MutableBuffer result = new MutableBuffer(data.Size, Endian.Endianess.Big);

            for (uint i = 0; i < data.Size; ++i)
            {
                result.Push((byte)(data.Pop<byte>() ^ (byte)i));
            }

            return result;
        }

        Buffer Unpack(Buffer data)
        {
            MutableBuffer result = new MutableBuffer(Endian.Endianess.Big);
            byte flag = data.Pop<byte>();

            while (data.Readable())
            {
                var val = data.Pop<byte>();
                uint count = 0;

                if (val == flag)
                {
                    count = data.Pop<byte>();
                    val = data.Pop<byte>();
                }

                for (uint i = 0; i < count + 1; ++i)
                {
                    result.Push(val);
                }
            }

            return result;
        }

        unsafe Buffer GetDataFromCatalog(uint catalogIndex, uint index, Buffer baseBuffer)
        {
            uint* catalog = (uint*)dataPointers[catalogIndex].Data;
            uint offset = Endian.Betoh(catalog[index]);

            if (offset == 0)
            {
                return null;
            }

            return baseBuffer.GetTail(offset);
        }

        SpriteAmiga GetMenuSprite(uint index, Buffer block,
                             uint width, uint height,
                             byte compression,
                             byte filling)
        {
            uint bpp = BitplaneCountFromCompression(compression);
            uint spriteSize = (width * height) / 8 * bpp;
            Buffer data = block.GetTail(spriteSize * index);

            return DecodeInterlasedSprite(data, width / 8, height, compression, filling, Palette2);
        }

        Sprite GetIconScprite(uint index)
        {
            Buffer data = gfxFast.GetTail(iconCatalog[(int)index]);

            ushort width = data.Pop<ushort>();
            ushort height = data.Pop<ushort>();
            byte filling = data.Pop<byte>();
            byte compression = data.Pop<byte>();

            return DecodePlannedSprite(data.PopTail(), width, height, compression, filling, Palette);
        }

        SpriteAmiga GetGroundSprite(uint index)
        {
            Buffer data = GetDataFromCatalog(4, index, gfxChip);

            if (data == null)
            {
                return null;
            }

            byte filled = data.Pop<byte>();
            byte compressed = data.Pop<byte>();

            SpriteAmiga sprite = DecodePlannedSprite(data.PopTail(), 4, 21, compressed, filled, Palette);

            if (sprite != null)
            {
                sprite.SetDelta(1, 0);
                sprite.SetOffset(0, 0);
            }

            return sprite;
        }

        unsafe Sprite GetGroundMaskSprite(uint index)
        {
            Buffer data;

            if (index == 0)
            {
                data = gfxChip.GetTail(0);
            }
            else
            {
                data = GetDataFromCatalog(2, index, gfxChip);
            }

            if (data == null)
            {
                return null;
            }

            ushort height = data.Pop<ushort>();

            SpriteAmiga sprite = new SpriteAmiga(32, height);
            sprite.SetDelta(2, 0);
            sprite.SetOffset(0, 0);

            fixed (byte* pointer = sprite.GetData())
            {
                uint* pixel = (uint*)pointer;

                for (int i = 0; i < 32 * height / 8; i++)
                {
                    byte b = data.Pop<byte>();

                    for (uint j = 0; j < 8; ++j)
                    {
                        if (((b >> (7 - (int)j)) & 0x01) == 0x01)
                        {
                            *pixel = 0xFFFFFFFF;
                        }
                        else
                        {
                            *pixel = 0x00000000;
                        }

                        ++pixel;
                    }
                }
            }

            return sprite;
        }

        unsafe SpriteAmiga GetMirroredHorizontalySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            SpriteAmiga result = new SpriteAmiga(sprite.Width, sprite.Height);
            result.SetDelta(sprite.DeltaX, sprite.DeltaY);
            result.SetOffset(sprite.OffsetX, sprite.OffsetY);

            fixed (byte* srcPointer = sprite.GetData())
            {
                byte* srcPixels = srcPointer;
                byte* dstPixels = srcPointer + result.Width * (result.Height - 1) * 4;

                for (uint i = 0; i < sprite.Height; ++i)
                {
                    System.Buffer.MemoryCopy(srcPixels, dstPixels, ulong.MaxValue, sprite.Width * 4);
                    srcPixels += sprite.Width * 4;
                    dstPixels -= sprite.Width * 4;
                }
            }

            return result;
        }

        Sprite GetPathMaskSprite(uint index)
        {
            Buffer data = GetDataFromCatalog(3, index, gfxChip);

            if (data == null)
            {
                return null;
            }

            byte n = data.Pop<byte>();
            byte k = data.Pop<byte>();

            uint width = (k == 66) ? 32u : 16u;
            uint[] heights = new uint[] { 0, 5, 9, 13, 17, 21, 25, 29, 33, 37, 41 };

            SpriteAmiga sprite = DecodeMaskSprite(data.PopTail(), width, heights[n]);
            sprite.SetDelta(2, 0);
            sprite.SetOffset(0, 0);

            return sprite;
        }

        Sprite GetGameObjectSprite(uint catalog, uint index)
        {
            Buffer data = GetDataFromCatalog(catalog, index, gfxChip);

            if (data == null)
            {
                return null;
            }

            byte h = data.Pop<byte>();
            byte offsetY = data.Pop<byte>();
            byte w = data.Pop<byte>();
            byte offsetX = data.Pop<byte>();
            byte filling = data.Pop<byte>();
            byte compression = data.Pop<byte>();

            if (w < 16)
                w = 16;

            uint width = (uint)(w + 7) / 8;

            SpriteAmiga mask = DecodeMaskSprite(data, width * 8, h);
            SpriteAmiga pixels = DecodePlannedSprite(data.PopTail(), width, h, compression, filling, Palette);

            SpriteAmiga sprite = pixels.GetAmigaMasked(mask);

            if (sprite == null)
            {
                return null;
            }

            sprite.SetDelta(1, 0);
            sprite.SetOffset(-offsetX, -offsetY);

            return sprite;
        }

        Sprite GetTorsoSprite(uint index, byte[] palette)
        {
            Buffer data = GetDataFromCatalog(19, index, gfxChip);

            if (data == null)
            {
                return null;
            }

            byte offsetX = data.Pop<byte>();
            byte w = data.Pop<byte>();
            byte offsetY = data.Pop<byte>();
            byte h = data.Pop<byte>();
            sbyte deltaY = (sbyte)data.Pop<byte>();
            sbyte deltaX = (sbyte)data.Pop<byte>();

            if (w < 16)
                w = 16;

            w = (byte)((w + 7) / 8);

            uint bps = (uint)w * h;

            SpriteAmiga mask = DecodeMaskSprite(data, (uint)w * 8, h);

            MutableBuffer buff = new MutableBuffer(Endian.Endianess.Big);

            buff.Push(data.Pop(bps * 4));
            data.Pop(bps);
            buff.Push(data.Pop(bps));

            SpriteAmiga pixels = DecodePlannedSprite(buff, w, h, 0, 0, palette);
            SpriteAmiga res = pixels.GetAmigaMasked(mask);

            res.SetDelta(deltaY, deltaX);
            res.SetOffset(-offsetX, -offsetY);

            return res;
        }

        Sprite GetMapObjectSprite(uint index)
        {
            Buffer data = GetDataFromCatalog(6, index, gfxChip);

            if (data == null)
            {
                return null;
            }

            data.Pop<ushort>();  // drop shadow_offset
            ushort bitplaneSize = data.Pop<ushort>();
            byte width = data.Pop<byte>();
            byte height = data.Pop<byte>();
            byte offsetY = data.Pop<byte>();
            byte compression = data.Pop<byte>();

            if (height == 0)
            {
                return null;
            }

            if (width == 0)
            {
                width = (byte)(bitplaneSize / height);
            }

            byte compressed = 0;
            byte filled = 0;

            for (int i = 0; i < 2; ++i)
            {
                compressed = (byte)(compressed >> 1);
                filled = (byte)(filled >> 1);

                if ((compression & 0x80) != 0)
                {
                    compressed = (byte)(compressed | 0x10);

                    if ((compression & 0x40) != 0)
                    {
                        filled = (byte)(filled | 0x10);
                    }

                    compression = (byte)(compression << 1);
                }

                compression = (byte)(compression << 1);
            }

            SpriteAmiga mask = DecodeMaskSprite(data, (uint)width * 8, height);
            SpriteAmiga pixels = DecodePlannedSprite(data.PopTail(), width, height, compressed, filled, Palette);

            SpriteAmiga sprite = pixels.GetAmigaMasked(mask);
            sprite.SetDelta(1, 0);

            if ((index >= 128) && (index <= 143))
            {
                sprite.SetOffset(0, -offsetY);
            }
            else
            {
                sprite.SetOffset(-width * 4, -offsetY);
            }

            return sprite;
        }

        Sprite GetMapObjectShadow(uint index)
        {
            Buffer data = GetDataFromCatalog(6, index, gfxChip);

            if (data == null)
            {
                return null;
            }

            ushort shadowOffset = data.Pop<ushort>();
            ushort bitplaneSize = data.Pop<ushort>();
            byte width = data.Pop<byte>();
            byte height = data.Pop<byte>();
            data.Pop<byte>();  // drop offset_y
            data.Pop<byte>();  // drop compression

            if (height == 0u)
                return null;

            if (width == 0u)
                width = (byte)(bitplaneSize / height);

            Buffer shadow = data.GetTail(shadowOffset);
            int shadowOffsetY = (sbyte)shadow.Pop<byte>();
            uint shadowHeight = shadow.Pop<byte>();
            int shadowOffsetX = (sbyte)shadow.Pop<byte>();
            uint shadowWidth = (uint)shadow.Pop<byte>() * 8;

            SpriteAmiga sprite = DecodeMaskSprite(shadow, shadowWidth, shadowHeight);
            sprite.FillMasked(new Sprite.Color { Blue = 0x00, Green = 0x00, Red = 0x00, Alpha = 0x80 });
            sprite.Clear();
            sprite.SetDelta(0, 0);
            sprite.SetOffset(shadowOffsetX * 8, -shadowOffsetY);

            return sprite;
        }

        static readonly uint[] HudOffsets =
        {
            0,    320, 2, 40,
            320,  320, 2, 40,
            640,  320, 2, 40,
            960,  320, 2, 40,
            1280, 320, 2, 40,
            1600, 320, 2, 40,
            1920, 320, 2, 40,
            2240,  64, 4,  4,
            2304,  64, 4,  4,
            2368,  64, 4,  4,
            2432,  64, 4,  4,
            2496,  64, 4,  4,
            2560,  64, 4,  4,
            2624,  64, 4,  4,
            2688,  64, 4,  4,
            2752,  64, 4,  4,
            2816,  64, 4,  4,
            2880, 800, 5, 40,
            3680,  48, 1, 12,
            3728,  40, 1, 10
        };

        SpriteAmiga GetHudSprite(uint index)
        {
            Buffer data = dataPointers[18].GetTail(HudOffsets[index * 4]);

            return DecodeInterlasedSprite(data, HudOffsets[index * 4 + 2],
                                          HudOffsets[index * 4 + 3], 16, 0, Palette2);
        }

        static byte Invert5Bit(byte src)
        {
            byte res = 0;

            for (int i = 0; i < 5; ++i)
            {
                res <<= 1;
                res = (byte)(res | (byte)(src & 0x01));
                src >>= 1;
            }

            return res;
        }

        unsafe SpriteAmiga DecodePlannedSprite(Buffer data, uint width, uint height,
                                   byte compression, byte filling,
                                   byte[] palette, bool invert = true)
        {
            SpriteAmiga sprite = new SpriteAmiga(width * 8, height);

            byte* src = data.Data;
            Sprite.Color* res = sprite.GetWritableData();

            uint bps = width * height;  // bitplane size in bytes

            for (uint i = 0; i < bps; ++i)
            {
                for (int k = 7; k >= 0; --k)
                {
                    byte color = 0;
                    int n = 0;

                    for (int b = 0; b < 5; ++b)
                    {
                        color = (byte)(color << 1);

                        if (((compression >> b) & 0x01) != 0)
                        {
                            if (((filling >> b) & 0x01) != 0)
                            {
                                color |= 0x01;
                            }
                        }
                        else
                        {
                            color |= (byte)((*(src + (n * bps)) >> k) & 0x01);
                            ++n;
                        }
                    }

                    if (invert)
                    {
                        color = Invert5Bit(color);
                    }

                    res->Red = palette[color * 3 + 0];    // R
                    res->Green = palette[color * 3 + 1];  // G
                    res->Blue = palette[color * 3 + 2];   // B
                    res->Alpha = 0xFF;                    // A
                    ++res;
                }

                ++src;
            }

            return sprite;
        }

        unsafe SpriteAmiga DecodeInterlasedSprite(Buffer data,
                                      uint width, uint height,
                                      byte compression, byte filling,
                                      byte[] palette,
                                      uint skip_lines = 0)
        {
            SpriteAmiga sprite = new SpriteAmiga(width * 8, height);

            byte* src = data.Data;
            Sprite.Color* res = sprite.GetWritableData();

            uint bpp = BitplaneCountFromCompression(compression);

            for (uint y = 0; y < height; ++y)
            {
                for (uint i = 0; i < width; ++i)
                {
                    for (int k = 7; k >= 0; --k)
                    {
                        byte color = 0;
                        int n = 0;

                        for (int b = 0; b < 5; b++)
                        {
                            color = (byte)(color << 1);

                            if (((compression >> b) & 0x01) != 0)
                            {
                                if (((filling >> b) & 0x01) != 0)
                                {
                                    color |= 0x01;
                                }
                            }
                            else
                            {
                                color |= (byte)((*(src + (n * width) + ((skip_lines * width * y))) >> k) & 0x01);
                                ++n;
                            }
                        }
                        color = Invert5Bit(color);
                        res->Red = palette[color * 3 + 0];    // R
                        res->Green = palette[color * 3 + 1];  // G
                        res->Blue = palette[color * 3 + 2];   // B
                        res->Alpha = 0xFF;                    // A
                        ++res;
                    }

                    ++src;
                }

                src += (bpp - 1) * width;
            }

            return sprite;
        }

        unsafe SpriteAmiga DecodeAmigaSprite(Buffer data, uint width, uint height, byte[] palette)
        {
            SpriteAmiga sprite = new SpriteAmiga(width * 8, height);

            byte* src1 = data.Data;
            uint bp2s = width * 2 * height;
            byte* src2 = src1 + bp2s;
            Sprite.Color* res = sprite.GetWritableData();

            for (uint y = 0; y < height; ++y)
            {
                for (uint i = 0; i < width; ++i)
                {
                    for (int k = 7; k >= 0; --k)
                    {
                        byte color = 0;
                        color |= (byte)((((*src1) >> k) & 0x01) << 0);
                        color |= (byte)((((*(src1 + width)) >> k) & 0x01) << 1);
                        color |= (byte)((((*src2) >> k) & 0x01) << 2);
                        color |= (byte)((((*(src2 + width)) >> k) & 0x01) << 3);
                        color |= 0x10;
                        res->Red = palette[color * 3 + 0];    // R
                        res->Green = palette[color * 3 + 1];  // G
                        res->Blue = palette[color * 3 + 2];   // B
                        res->Alpha = 0xFF;                    // A
                        ++res;
                    }

                    ++src1;
                    ++src2;
                }

                src1 += width;
                src2 += width;
            }

            return sprite;
        }

        unsafe SpriteAmiga DecodeMaskSprite(Buffer data, uint width, uint height)
        {
            SpriteAmiga sprite = new SpriteAmiga(width, height);

            uint size = width / 8 * height;

            fixed (byte* pointer = sprite.GetData())
            {
                uint* pixel = (uint*)pointer;

                for (uint i = 0; i < size; ++i)
                {
                    byte b = data.Pop<byte>();

                    for (uint j = 0; j < 8; ++j)
                    {
                        if (((b >> (7 - (int)j)) & 0x01) == 0x01)
                        {
                            *pixel = 0xFFFFFFFF;
                        }
                        else
                        {
                            *pixel = 0x00000000;
                        }

                        ++pixel;
                    }
                }
            }

            return sprite;
        }

        uint BitplaneCountFromCompression(byte compression)
        {
            uint bpp = 5;

            for (int i = 0; i < 5; i++)
            {
                if ((compression & 0x01) != 0)
                {
                    --bpp;
                }

                compression = (byte)(compression >> 1);
            }

            return bpp;
        }

        SpriteAmiga MakeShadowFromSymbol(SpriteAmiga symbol)
        {
            SpriteAmiga res = new SpriteAmiga(10, 10);

            res.Stick(symbol, 1, 0);
            res.Stick(symbol, 0, 1);
            res.Stick(symbol, 2, 1);
            res.Stick(symbol, 1, 2);
            res.FillMasked(new Sprite.Color { Blue = 0xFF, Green = 0xFF, Red = 0xFF, Alpha = 0xFF });

            return res;
        }

        Buffer GetSoundData(uint index)
        {
            if (sound != null)
            {
                if (index < SoundInfo.Length)
                {
                    if (SoundInfo[index].Size != 0)
                    {
                        return sound.GetSubBuffer(SoundInfo[index].Offset, SoundInfo[index].Size);
                    }
                }
            }

            return null;
        }
    }
}
