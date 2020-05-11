/*
 * DataSourceDos.cs - DOS game resources file functions
 *
 * Copyright (C) 2014-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.Data
{
    using MaskImage = Tuple<Sprite, Sprite>;

    public class TPWMException : ExceptionFreeserf
    {
        public TPWMException(ErrorSystemType type, string message)
            : base(type, message)
        {

        }
    }

    class UnpackerTPWM : Converter
    {
        public UnpackerTPWM(Buffer buffer)
            : base(buffer)
        {
            if (buffer.Size < 8)
            {
                throw new TPWMException(ErrorSystemType.Data, "Data is not a TPWM archive");
            }

            Buffer id = buffer.Pop(4);

            if (id.ToString(4) != "TPWM")
            {
                throw new TPWMException(ErrorSystemType.Data, "Data is not a TPWM archive");
            }
        }

        public override Buffer Convert()
        {
            uint resultSize = buffer.Pop<ushort>();
            MutableBuffer result = new MutableBuffer(Endian.Endianess.Big);

            try
            {
                while (buffer.Readable())
                {
                    uint flag = buffer.Pop<byte>();

                    for (int i = 0; i < 8; ++i)
                    {
                        flag <<= 1;

                        if ((flag & ~0xFF) != 0)
                        {
                            flag &= 0xFF;
                            uint temp = buffer.Pop<byte>();
                            uint stampSize = (temp & 0x0F) + 3;
                            uint stampOffset = buffer.Pop<byte>();
                            stampOffset |= ((temp << 4) & 0x0F00);
                            Buffer stamp = result.GetSubBuffer(result.Size - stampOffset, stampSize);
                            result.Push(stamp);
                        }
                        else
                        {
                            result.Push<byte>(buffer.Pop<byte>());
                        }
                    }
                }
            }
            catch
            {
                throw new ExceptionFreeserf(ErrorSystemType.Data, "TPWM source data corrupted");
            }

            if (result.Size != resultSize)
            {
                throw new ExceptionFreeserf(ErrorSystemType.Data, "TPWM source data corrupted");
            }

            return result;
        }
    }

    public class DataSourceDos : DataSourceLegacy
    {
        const int DATA_SERF_ANIMATION_TABLE = 2;
        const int DATA_SERF_ARMS = 1850;  // 3, dos_sprite_type_transparent
        const int DATA_SFX_BASE = 3900;  // SFX sounds (index 0 is undefined)
        const int DATA_MUSIC_GAME = 3990;  // XMI music
        const int DATA_MUSIC_ENDING = 3992;  // XMI music

        // There are different types of sprites:
        // - Non-packed, rectangular sprites: These are simple called sprites here.
        // - Transparent sprites, "transp": These are e.g. buldings/serfs.
        // The transparent regions are RLE encoded.
        // - Bitmap sprites: Conceptually these contain either 0 or 1 at each pixel.
        // This is used to either modify the alpha level of another sprite (shadows)
        // or mask parts of other sprites completely (mask sprites).

        Resource[] DosResources = new Resource[]
        {
            new Resource(   0, 0,    SpriteType.Unknown     ),  // none
            new Resource(   1, 3997, SpriteType.Solid       ),  // art_landscape
            new Resource(   2, 0,    SpriteType.Unknown     ),  // animation
            new Resource(   4, 3,    SpriteType.Overlay     ),  // serf_shadow
            new Resource(   5, 3,    SpriteType.Solid       ),  // dotted_lines
            new Resource(  15, 3997, SpriteType.Solid       ),  // art_flag
            new Resource(  25, 3,    SpriteType.Solid       ),  // art_box
            new Resource(  40, 3998, SpriteType.Solid       ),  // credits_bg
            new Resource(  41, 3998, SpriteType.Solid       ),  // logo
            new Resource(  42, 3,    SpriteType.Solid       ),  // symbol
            new Resource(  60, 3,    SpriteType.Mask        ),  // map_mask_up
            new Resource( 141, 3,    SpriteType.Mask        ),  // map_mask_down
            new Resource( 230, 3,    SpriteType.Mask        ),  // path_mask
            new Resource( 260, 3,    SpriteType.Solid       ),  // map_ground
            new Resource( 300, 3,    SpriteType.Solid       ),  // path_ground
            new Resource( 321, 3,    SpriteType.Transparent ),  // game_object
            new Resource( 600, 3,    SpriteType.Solid       ),  // frame_top
            new Resource( 610, 3,    SpriteType.Transparent ),  // map_border
            new Resource( 630, 3,    SpriteType.Transparent ),  // map_waves
            new Resource( 660, 3,    SpriteType.Solid       ),  // frame_popup
            new Resource( 670, 3,    SpriteType.Solid       ),  // indicator
            new Resource( 750, 3,    SpriteType.Transparent ),  // font
            new Resource( 810, 3,    SpriteType.Transparent ),  // font_shadow
            new Resource( 870, 3,    SpriteType.Solid       ),  // icon
            new Resource(1250, 3,    SpriteType.Transparent ),  // map_object
            new Resource(1500, 3,    SpriteType.Overlay     ),  // map_shadow
            new Resource(1750, 3,    SpriteType.Solid       ),  // panel_button
            new Resource(1780, 3,    SpriteType.Solid       ),  // frame_bottom
            new Resource(2500, 3,    SpriteType.Transparent ),  // serf_torso
            new Resource(3150, 3,    SpriteType.Transparent ),  // serf_head
            new Resource(3880, 3,    SpriteType.Solid       ),  // frame_split
            new Resource(3900, 0,    SpriteType.Unknown     ),  // sound
            new Resource(3990, 0,    SpriteType.Unknown     ),  // music
            new Resource(3999, 3,    SpriteType.Transparent ),  // cursor
            new Resource(   3, 0,    SpriteType.Unknown     )   // palette
        };

        enum SpriteType
        {
            Unknown = 0,
            Solid,
            Transparent,
            Overlay,
            Mask
        }

        struct Resource
        {
            public Resource(uint index, uint dosPalette, SpriteType spriteType)
            {
                Index = index;
                DosPalette = dosPalette;
                SpriteType = spriteType;
            }

            public uint Index;
            public uint DosPalette;
            public SpriteType SpriteType;
        }

        struct ColorDos
        {
            public byte R;
            public byte G;
            public byte B;
        }

        class SpriteBaseDos : Sprite
        {
            public SpriteBaseDos(Buffer data)
            {
                if (data.Size < 10)
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Failed to extract DOS sprite");
                }

                deltaX = data.Pop<sbyte>();
                deltaY = data.Pop<sbyte>();
                width = data.Pop<ushort>();
                height = data.Pop<ushort>();
                offsetX = data.Pop<short>();
                offsetY = data.Pop<short>();
            }
        }

        class SpriteDosSolid : SpriteBaseDos
        {
            public SpriteDosSolid(Buffer data, ColorDos[] palette)
                : base(data)
            {
                uint size = data.Size;

                if (size != (width * height + 10))
                {
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Failed to extract DOS solid sprite");
                }

                MutableBuffer result = new MutableBuffer(Endian.Endianess.Big);

                while (data.Readable())
                {
                    ColorDos color = palette[data.Pop<byte>()];
                    result.Push<byte>(color.B);  // Blue
                    result.Push<byte>(color.G);  // Green
                    result.Push<byte>(color.R);  // Red
                    result.Push<byte>(0xff);     // Alpha
                }

                this.data = result.Unfix();
            }
        }

        class SpriteDosTransparent : SpriteBaseDos
        {
            public SpriteDosTransparent(Buffer data, ColorDos[] palette, byte colorOffset = 0)
                : base(data)
            {
                MutableBuffer result = new MutableBuffer(Endian.Endianess.Big);

                while (data.Readable())
                {
                    uint drop = data.Pop<byte>();
                    result.Push(0x00000000u, drop);

                    uint fill = data.Pop<byte>();

                    for (uint i = 0; i < fill; ++i)
                    {
                        uint palIndex = (uint)(data.Pop<byte>() + colorOffset);
                        ColorDos color = palette[palIndex];
                        result.Push<byte>(color.B);  // Blue
                        result.Push<byte>(color.G);  // Green
                        result.Push<byte>(color.R);  // Red
                        result.Push<byte>(0xFF);     // Alpha
                    }
                }

                this.data = result.Unfix();
            }
        }

        class SpriteDosOverlay : SpriteBaseDos
        {
            public SpriteDosOverlay(Buffer data, ColorDos[] palette, byte value)
                : base(data)
            {
                MutableBuffer result = new MutableBuffer(Endian.Endianess.Big);

                while (data.Readable())
                {
                    uint drop = data.Pop<byte>();
                    result.Push(0x00000000u, drop);

                    uint fill = data.Pop<byte>();

                    for (uint i = 0; i < fill; ++i)
                    {
                        ColorDos color = palette[value];
                        result.Push<byte>(color.B);  // Blue
                        result.Push<byte>(color.G);  // Green
                        result.Push<byte>(color.R);  // Red
                        result.Push<byte>(value);    // Alpha
                    }
                }

                this.data = result.Unfix();
            }
        }

        class SpriteDosMask : SpriteBaseDos
        {
            public SpriteDosMask(Buffer data)
                : base(data)
            {
                MutableBuffer result = new MutableBuffer(Endian.Endianess.Big);

                while (data.Readable())
                {
                    uint drop = data.Pop<byte>();
                    result.Push(0x00000000u, drop);

                    uint fill = data.Pop<byte>();
                    result.Push(0xFFFFFFFFu, fill);
                }

                this.data = result.Unfix();
            }
        }

        // These entries follow the 8 byte header of the data file.
        struct DataEntry
        {
            public uint Offset;
            public uint Size;
        }

        Buffer spae = null;
        List<DataEntry> entries = new List<DataEntry>();

        public DataSourceDos(string path)
            : base(path)
        {
        }

        public override string Name => "DOS";
        public override uint Scale => 1;
        public override uint BPP => 8;

        static readonly string[] DefaultFileNames = new string[]
        {
            "SPAE.PA",  // English
            "SPAF.PA",  // French
            "SPAD.PA",  // German
            "SPAU.PA"   // Engish (US)
        };

        public override bool CheckMusic() => Check();
        public override bool CheckSound() => Check();
        public override bool CheckGraphics() => Check();

        public override bool Check()
        {
            if (CheckFile(path))
                return true;

            foreach (var fileName in DefaultFileNames)
            {
                string filePath = path + '/' + fileName;

                Log.Info.Write(ErrorSystemType.Data, $"Looking for game data in '{filePath}'...");

                if (CheckFile(filePath))
                {
                    path = filePath;
                    return true;
                }
            }

            return false;
        }

        public override DataLoadResult Load()
        {
            if (!Check())
                return DataLoadResult.NothingLoaded;

            try
            {
                spae = new Buffer(path);
            }
            catch
            {
                return DataLoadResult.NothingLoaded;
            }

            // Check that data file is decompressed
            try
            {
                UnpackerTPWM unpacker = new UnpackerTPWM(spae);
                spae = unpacker.Convert();
                Log.Verbose.Write(ErrorSystemType.Data, "Data file is compressed");
            }
            catch
            {
                Log.Verbose.Write(ErrorSystemType.Data, "Data file is not compressed");
            }

            // Read the number of entries in the index table.
            // Some entries are undefined (size and offset are zero).
            uint entryCount = spae.Pop<uint>();
            entries.Add(new DataEntry() { Offset = 0, Size = 0 });  // first entry is whole file itself, drop it

            for (uint i = 0; i < entryCount; ++i)
            {
                DataEntry entry = new DataEntry();
                entry.Size = spae.Pop<uint>();
                entry.Offset = spae.Pop<uint>();
                entries.Add(entry);
            }

            FixUp();

            // The first uint32 is the byte length of the rest
            // of the table in big endian order.
            Buffer anim = GetObject(DATA_SERF_ANIMATION_TABLE);
            anim.SetEndianess(Endian.Endianess.Big);
            uint size = anim.Size;

            if (size != anim.Pop<uint>())
            {
                Log.Error.Write(ErrorSystemType.Data, "Could not extract animation table.");
                return DataLoadResult.NothingLoaded;
            }

            anim = anim.PopTail();

            loaded = LoadAnimationTable(anim);

            return loaded ? DataLoadResult.AllLoaded : DataLoadResult.NothingLoaded;
        }

        // Create sprite object
        public override Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, uint index)
        {
            if (index >= Data.GetResourceCount(resource))
            {
                return Tuple.Create<Sprite, Sprite>(null, null);
            }

            Resource dosRes = DosResources[(int)resource];
            ColorDos[] palette = GetDosPalette(dosRes.DosPalette);
            Buffer data;

            if (palette == null)
            {
                return Tuple.Create<Sprite, Sprite>(null, null);
            }

            if (resource == Data.Resource.SerfTorso)
            {
                data = GetObject(dosRes.Index + index);

                if (data == null)
                {
                    return Tuple.Create<Sprite, Sprite>(null, null);
                }

                Sprite torso = new SpriteDosTransparent(data, palette, 64);

                data = GetObject(dosRes.Index + index);

                if (data == null)
                {
                    return Tuple.Create<Sprite, Sprite>(null, null);
                }

                Sprite torso2 = new SpriteDosTransparent(data, palette, 72);

                MaskImage maskImage = SeparateSprites(torso, torso2);

                data = GetObject(DATA_SERF_ARMS + index);

                Sprite arms = new SpriteDosTransparent(data, palette);

                torso.Stick(arms, 0, 0);

                return maskImage;
            }
            else if (resource == Data.Resource.MapObject)
            {
                if ((index >= 128) && (index <= 143))
                {
                    // Flag sprites
                    uint flagFrame = (index - 128) % 4;
                    data = GetObject(dosRes.Index + 128 + flagFrame);

                    if (data == null)
                    {
                        return Tuple.Create<Sprite, Sprite>(null, null);
                    }

                    Sprite s1 = new SpriteDosTransparent(data, palette);

                    data = GetObject(dosRes.Index + 128 + 4 + flagFrame);

                    if (data == null)
                    {
                        return Tuple.Create<Sprite, Sprite>(null, null);
                    }

                    Sprite s2 = new SpriteDosTransparent(data, palette);

                    return SeparateSprites(s1, s2);
                }
            }
            else if (resource == Data.Resource.Font || resource == Data.Resource.FontShadow)
            {
                data = GetObject(dosRes.Index + index);

                if (data == null)
                {
                    return Tuple.Create<Sprite, Sprite>(null, null);
                }

                return Tuple.Create<Sprite, Sprite>(new SpriteDosTransparent(data, palette), null);
            }

            data = GetObject(dosRes.Index + index);

            if (data == null)
            {
                return Tuple.Create<Sprite, Sprite>(null, null);
            }

            Sprite sprite;

            switch (dosRes.SpriteType)
            {
                case SpriteType.Solid:
                    {
                        sprite = new SpriteDosSolid(data, palette);
                        break;
                    }
                case SpriteType.Transparent:
                    {
                        sprite = new SpriteDosTransparent(data, palette);
                        break;
                    }
                case SpriteType.Overlay:
                    {
                        sprite = new SpriteDosOverlay(data, palette, 0x80);
                        break;
                    }
                case SpriteType.Mask:
                    {
                        sprite = new SpriteDosMask(data);
                        break;
                    }
                default:
                    return Tuple.Create<Sprite, Sprite>(null, null);
            }

            return Tuple.Create<Sprite, Sprite>(null, sprite);
        }

        public override Buffer GetSound(uint index)
        {
            Buffer data = GetObject(DATA_SFX_BASE + index);

            if (data == null)
            {
                Log.Error.Write(ErrorSystemType.Data, $"Could not extract SFX clip: #{index}.");
                return null;
            }

            return data;
        }

        public override Buffer GetMusic(uint index)
        {
            Buffer data = GetObject(DATA_MUSIC_GAME + index);

            if (data == null)
            {
                Log.Error.Write(ErrorSystemType.Data, $"Could not extract XMI clip: #{index}.");
                return null;
            }

            return data;
        }

        // Return buffer with object at index
        Buffer GetObject(uint index)
        {
            if (index >= entries.Count)
            {
                return null;
            }

            if (entries[(int)index].Offset == 0)
            {
                return null;
            }

            return spae.GetSubBuffer(entries[(int)index].Offset, entries[(int)index].Size);
        }

        // Perform various fixups of the data file entries
        void FixUp()
        {
            // Fill out some undefined spaces in the index from other
            // places in the data file index.

            for (int i = 0; i < 48; ++i)
            {
                for (int j = 1; j < 6; j++)
                {
                    entries[3450 + 6 * i + j] = entries[3450 + 6 * i];
                }
            }

            for (int i = 0; i < 3; ++i)
            {
                entries[3765 + i] = entries[3762 + i];
            }

            for (int i = 0; i < 6; ++i)
            {
                entries[1363 + i] = entries[1352];
                entries[1613 + i] = entries[1602];
            }
        }

        ColorDos[] GetDosPalette(uint index)
        {
            Buffer data = GetObject(index);

            if (data == null || (data.Size != 3 * 256)) // sizeof(ColorDos) = 3
            {
                return null;
            }

            ColorDos[] palette = new ColorDos[256];
            var array = data.ReinterpretAsArray(256 * 3);

            for (int i = 0; i < 256; ++i)
            {
                palette[i] = new ColorDos()
                {
                    R = array[i * 3 + 0],
                    G = array[i * 3 + 1],
                    B = array[i * 3 + 2]
                };
            }

            return palette;
        }
    }
}
