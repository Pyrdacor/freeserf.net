using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
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

            }

            public void set_delta(int x, int y) { delta_x = x; delta_y = y; }
            public void set_offset(int x, int y) { offset_x = x; offset_y = y; }
            public SpriteAmiga get_amiga_masked(PSprite mask)
            {

            }

            public void clear()
            {
            }

            public Sprite.Color* get_writable_data()
            {
                return reinterpret_cast<Sprite.Color*>(data);
            }

            public void make_transparent(uint rc = 0)
            {

            }

            public SpriteAmiga merge_horizontaly(SpriteAmiga right)
            {

            }

            public SpriteAmiga split_horizontaly(bool return_right)
            {

            }
        }

        public DataSourceAmiga(string path)
            : base(path)
        {
        }

        public override string Name => "Amiga";
        public override uint Scale => 1;
        public override uint BPP => 5;

        public override bool Check()
        {
            var dataFiles = new string[]
            {
                "gfxheader",    // catalog file
                "gfxfast",      // fast graphics file
                "gfxchip"       // chip graphics file
            };

            foreach (var fileName in dataFiles)
            {
                var cp = path + '/' + fileName;

                Log.Info.Write("data", $"Looking for game data in '{cp}'...");

                if (!CheckFile(cp))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Load()
        {
            try
            {
                gfxFast = new Buffer(path + "/gfxfast", Endian.Endianess.Big);
                gfxFast = Decode(gfxFast);
                gfxFast = Unpack(gfxFast);

                Log.Debug.Write("data", $"Data file 'gfxfast' loaded (size = {gfxFast.Size})");
            }
            catch (Exception)
            {
                Log.Debug.Write("data", "Failed to load 'gfxfast'");
                return false;
            }

            try
            {
                gfxChip = new Buffer(path + "/gfxchip", Endian.Endianess.Big);
                gfxChip = Decode(gfxChip);
                gfxChip = Unpack(gfxChip);
                Log.Debug.Write("data", $"Data file 'gfxchip' loaded (size = {gfxChip.Size})");
            }
            catch (Exception)
            {
                Log.Debug.Write("data", "Failed to load 'gfxchip'");
                return false;
            }

            Buffer gfxHeader;

            try
            {
                gfxHeader = new Buffer(path + "/gfxheader", Endian.Endianess.Big);
            }
            catch (Exception)
            {
                Log.Debug.Write("data", "Failed to load 'gfxheader'");
                return false;
            }

            // Prepare icons catalog
            uint icon_catalog_offset = gfxHeader.Pop<ushort>();
            uint icon_catalog_size = gfxHeader.Pop<ushort>();
            Buffer icon_catalog_tmp = gfxFast.GetSubBuffer(icon_catalog_offset * 4,
                                                              icon_catalog_size * 4);
            for (uint i = 0; i < icon_catalog_size; ++i)
            {
                uint offset = icon_catalog_tmp.Pop<uint>();

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

            try
            {
                sound = new Buffer(path + "/sounds");
                sound = Decode(sound);
            }
            catch (Exception)
            {
                Log.Warn.Write("data", "Failed to load 'sounds'");
                sound = null;
            }

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

                Log.Debug.Write("data", $"Data file 'gfxpics' loaded (size = {gfxPics.Size})");
            }
            catch (Exception)
            {
                Log.Warn.Write("data", "Failed to load 'gfxpics'");
            }

            return LoadAnimationTable(dataPointers[1].GetSubBuffer(0, 30528));
        }

        public override Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, int index)
        {
            throw new NotImplementedException();
        }

        public override Buffer GetSound(uint index)
        {
            throw new NotImplementedException();
        }

        public override Buffer GetMusic(uint index)
        {
            throw new NotImplementedException();
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

        }

        Buffer Unpack(Buffer data)
        {

        }

        Buffer GetDataFromCatalog(uint catalog, uint index, Buffer baseBuffer)
        {

        }

        SpriteAmiga GetMenuSprite(uint index, Buffer block,
                             uint width, uint height,
                             byte compression,
                             byte filling)
        {

        }

        Sprite GetIconScprite(uint index)
        {

        }

        SpriteAmiga GetGroundSprite(uint index)
        {

        }

        Sprite GetGroundMaskSprite(uint index)
        {

        }

        SpriteAmiga GetMirroredHorizontalySprite(Sprite sprite)
        {

        }

        Sprite GetPathMaskSprite(uint index)
        {

        }

        Sprite GetGameObjectSprite(uint catalog, uint index)
        {

        }

        Sprite GetTorsoSprite(uint index, byte* palette)
        {

        }

        Sprite GetMapObjectSprite(uint index)
        {

        }

        Sprite GetMapObjectShadow(uint index)
        {

        }

        SpriteAmiga GetHudSprite(uint index)
        {

        }

        SpriteAmiga DecodePlannedSprite(Buffer data, uint width, uint height,
                                   byte compression, byte filling,
                                   byte* palette, bool invert = true)
        {

        }

        SpriteAmiga DecodeInterlasedSprite(Buffer data,
                                      uint width, uint height,
                                      byte compression, byte filling,
                                      byte* palette,
                                      uint skip_lines = 0)
        {

        }

        SpriteAmiga DecodeAmigaSprite(Buffer data, uint width, uint height, byte* palette)
        {

        }

        SpriteAmiga DecodeMaskSprite(Buffer data, uint width, uint height)
        {

        }

        uint BitplaneCountFromCompression(byte compression)
        {

        }

        SpriteAmiga MakeShadowFromSymbol(SpriteAmiga symbol)
        {

        }

        Buffer GetSoundData(uint index)
        {

        }
    }
}
