/*
 * RenderSerf.cs - Handles serf rendering
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System.Collections.Generic;

namespace Freeserf.Render
{
    // TODO: The baseline offset is bad when a serf walks through resources at a flag.
    //       The serf is always on top of the resources. Maybe fix later somehow.
    // TODO: Shadow
    internal class RenderSerf : RenderObject
    {
        static readonly int[] AppearanceIndex1 = new int[]
        {
            0, 0, 48, 6, 96, -1, 48, 24,
            240, -1, 48, 30, 248, -1, 48, 12,
            48, 18, 96, 306, 96, 300, 48, 54,
            48, 72, 48, 36, 0, 48, 272, -1,
            48, 60, 264, -1, 48, 42, 280, -1,
            48, 66, 96, 312, 500, 600, 48, 318,
            48, 78, 0, 84, 48, 90, 48, 96,
            48, 102, 48, 108, 48, 114, 96, 324,
            96, 330, 96, 336, 96, 342, 96, 348,
            48, 354, 48, 360, 48, 366, 48, 372,
            48, 378, 48, 384, 504, 604, 509, -1,
            48, 120, 288, -1, 288, 420, 48, 126,
            48, 132, 96, 426, 0, 138, 304, -1,
            48, 390, 48, 144, 96, 432, 48, 198,
            510, 608, 48, 204, 48, 402, 48, 150,
            96, 438, 48, 156, 312, -1, 320, -1,
            48, 162, 48, 168, 96, 444, 0, 174,
            513, -1, 48, 408, 48, 180, 96, 450,
            0, 186, 520, -1, 48, 414, 48, 192,
            96, 456, 328, -1, 48, 210, 344, -1,
            48, 6, 48, 6, 48, 216, 528, -1,
            48, 534, 48, 528, 48, 288, 48, 282,
            48, 222, 533, -1, 48, 540, 48, 546,
            48, 552, 48, 558, 48, 564, 96, 468,
            96, 462, 48, 570, 48, 576, 48, 582,
            48, 396, 48, 228, 48, 234, 48, 240,
            48, 246, 48, 252, 48, 258, 48, 264,
            48, 270, 48, 276, 96, 474, 96, 480,
            96, 486, 96, 492, 96, 498, 96, 504,
            96, 510, 96, 516, 96, 522, 96, 612,
            144, 294, 144, 588, 144, 594, 144, 618,
            144, 624, 401, 294, 352, 297, 401, 588,
            352, 591, 401, 594, 352, 597, 401, 618,
            352, 621, 401, 624, 352, 627, 450, -1,
            192, -1
        };

        static readonly int[] AppearanceIndex2 = new int[]
        {
            0, 0, 1, 0, 2, 0, 3, 0,
            4, 0, 5, 0, 6, 0, 7, 0,
            8, 1, 9, 1, 10, 1, 11, 1,
            12, 1, 13, 1, 14, 1, 15, 1,
            16, 2, 17, 2, 18, 2, 19, 2,
            20, 2, 21, 2, 22, 2, 23, 2,
            24, 3, 25, 3, 26, 3, 27, 3,
            28, 3, 29, 3, 30, 3, 31, 3,
            32, 4, 33, 4, 34, 4, 35, 4,
            36, 4, 37, 4, 38, 4, 39, 4,
            40, 5, 41, 5, 42, 5, 43, 5,
            44, 5, 45, 5, 46, 5, 47, 5,
            0, 0, 1, 0, 2, 0, 3, 0,
            4, 0, 5, 0, 6, 0, 2, 0,
            0, 1, 1, 1, 2, 1, 3, 1,
            4, 1, 5, 1, 6, 1, 2, 1,
            0, 2, 1, 2, 2, 2, 3, 2,
            4, 2, 5, 2, 6, 2, 2, 2,
            0, 3, 1, 3, 2, 3, 3, 3,
            4, 3, 5, 3, 6, 3, 2, 3,
            0, 0, 1, 0, 2, 0, 3, 0,
            4, 0, 5, 0, 6, 0, 7, 0,
            8, 0, 9, 0, 10, 0, 11, 0,
            12, 0, 13, 0, 14, 0, 15, 0,
            16, 0, 17, 0, 18, 0, 19, 0,
            20, 0, 21, 0, 22, 0, 23, 0,
            24, 0, 25, 0, 26, 0, 27, 0,
            28, 0, 29, 0, 30, 0, 31, 0,
            32, 0, 33, 0, 34, 0, 35, 0,
            36, 0, 37, 0, 38, 0, 39, 0,
            40, 0, 41, 0, 42, 0, 43, 0,
            44, 0, 45, 0, 46, 0, 47, 0,
            48, 0, 49, 0, 50, 0, 51, 0,
            52, 0, 53, 0, 54, 0, 55, 0,
            56, 0, 57, 0, 58, 0, 59, 0,
            60, 0, 61, 0, 62, 0, 63, 0,
            64, 0
        };

        static readonly int[] IdleAnimations1 = new int[]
        {
            0x240, 0x40, 0x380, 0x140, 0x300, 0x80, 0x180, 0x200,
            0, 0x340, 0x280, 0x100, 0x1c0, 0x2c0, 0x3c0, 0xc0
        };

        static readonly int[] IdleAnimations2 = new int[]
        {
            0x8800, 0x8800, 0x8800, 0x8800, 0x8801, 0x8802, 0x8803, 0x8804,
            0x8804, 0x8804, 0x8804, 0x8804, 0x8803, 0x8802, 0x8801, 0x8800,
            0x8800, 0x8800, 0x8800, 0x8800, 0x8801, 0x8802, 0x8803, 0x8804,
            0x8805, 0x8806, 0x8807, 0x8808, 0x8809, 0x8808, 0x8809, 0x8808,
            0x8809, 0x8807, 0x8806, 0x8805, 0x8804, 0x8804, 0x8804, 0x8804,
            0x28, 0x29, 0x2a, 0x2b, 0x4, 0x5, 0xe, 0xf,
            0x10, 0x11, 0x1a, 0x1b, 0x23, 0x25, 0x26, 0x27,
            0x8800, 0x8800, 0x8800, 0x8800, 0x8801, 0x8802, 0x8803, 0x8804,
            0x8803, 0x8802, 0x8801, 0x8800, 0x8800, 0x8800, 0x8800, 0x8800,
            0x8801, 0x8802, 0x8803, 0x8804, 0x8804, 0x8804, 0x8804, 0x8804,
            0x8805, 0x8806, 0x8807, 0x8808, 0x8809, 0x8807, 0x8806, 0x8805,
            0x8804, 0x8803, 0x8802, 0x8802, 0x8802, 0x8802, 0x8801, 0x8800,
            0x8800, 0x8800, 0x8800, 0x8801, 0x8802, 0x8803, 0x8803, 0x8803,
            0x8803, 0x8804, 0x8804, 0x8804, 0x8805, 0x8806, 0x8807, 0x8808,
            0x8809, 0x8808, 0x8809, 0x8808, 0x8809, 0x8807, 0x8806, 0x8805,
            0x8803, 0x8803, 0x8803, 0x8802, 0x8802, 0x8801, 0x8801, 0x8801
        };

        static readonly int[] IdleAnimations3 = new int[]
        {
            0, 0, 0, 0, 0, 0, -2, 1, 0, 0, 2, 2, 0, 5, 0, 0,
            0, 0, 0, 3, -2, 2, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0,
            0, 0, -1, 2, -2, 1, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0,
            1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, -1, 2, -2, 1, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0,
            1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        static readonly int[] FightingFlashOffsets = new int[]
        {
             9, 5,
            10, 7,
            10, 2,
             8, 6,
            11, 8,
             9, 6,
             9, 8,
             0, 0,
             0, 0,
             0, 0,
             5, 5,
             4, 7,
             4, 2,
             7, 5,
             3, 8,
             5, 6,
             5, 8,
             0, 0,
             0, 0,
             0, 0
        };

        static readonly int[] TransporterType = new int[]
        {
            0, 0x3000, 0x3500, 0x3b00, 0x4100, 0x4600, 0x4b00, 0x1400,
            0x700, 0x5100, 0x800, 0x1c00, 0x1d00, 0x1e00, 0x1a00, 0x1b00,
            0x6800, 0x6d00, 0x6500, 0x6700, 0x6b00, 0x6a00, 0x6600, 0x6900,
            0x6c00, 0x5700, 0x5600, 0, 0, 0, 0, 0
        };

        static readonly int[] SailorType = new int[]
        {
            0, 0x3100, 0x3600, 0x3c00, 0x4200, 0x4700, 0x4c00, 0x1500,
            0x900, 0x7700, 0xa00, 0x2100, 0x2200, 0x2300, 0x1f00, 0x2000,
            0x6e00, 0x6f00, 0x7000, 0x7100, 0x7200, 0x7300, 0x7400, 0x7500,
            0x7600, 0x5f00, 0x6000, 0, 0, 0, 0, 0
        };

        static readonly int[] BuildingBaseLineOffsetLeft = new int[]
        {
            0, 12, 3, 15, 6,
            10, 14, 12, 12,
            4, 20, 10, 18, 3, 18, 6,
            10, 12, 7, 11, 3, 8, 17, 8, 20
        };

        static readonly int[] BuildingBaseLineOffsetRight = new int[]
        {
            0, 3, 3, 6, 2,
            3, 7, 5, 5,
            0, 20, 2, 20, 0, 16, 6,
            0, 11, 0, 0, 0, 0, 15, 0, 20
        };

        Serf serf = null;
        const uint HeadOffset = 0u;
        const uint TorsoOffset = 1000u;
        const uint TorsoPlayerOffset = 1000u;
        const uint ShadowIndex = 5000u;
        static readonly Dictionary<uint, Position> torsoDeltas = new Dictionary<uint, Position>();
        static readonly Dictionary<uint, Rect> headSpriteInfos = new Dictionary<uint, Rect>();
        static readonly Dictionary<uint, Rect> torsoSpriteInfos = new Dictionary<uint, Rect>();
        static Rect shadowSpriteInfo = null;
        ISprite headSprite = null;
        DataSource dataSource = null;
        Audio audio = null;
        ISprite fightingFlash = null;
        readonly Serf parentSerf = null;
        readonly RenderSerf parentRenderSerf = null;
        RenderSerf fightingEnemy = null;
        readonly ISpriteFactory spriteFactory = null;

        // Note: The base sprite used in RenderObject will be the torso sprite.

        public RenderSerf(Serf serf, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource, Audio audio)
            : this(null, null, serf, renderLayer, spriteFactory, dataSource, audio)
        {
        }

        // if parent is not null this is the fight enemy
        RenderSerf(Serf parentSerf, RenderSerf parentRenderSerf, Serf serf, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource, Audio audio)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.serf = serf;
            this.spriteFactory = spriteFactory;
            this.dataSource = dataSource;
            this.audio = audio;

            Initialize();

            headSprite.Layer = sprite.Layer;

            InitOffsets(dataSource);

            this.parentSerf = parentSerf;
            this.parentRenderSerf = parentRenderSerf;
        }

        public override bool Visible
        {
            get => sprite.Visible;
            set
            {
                base.Visible = value;

                if (headSprite != null)
                    headSprite.Visible = value;
            }
        }

        public override void Delete()
        {
            base.Delete();

            if (headSprite != null)
            {
                headSprite.Delete();
                headSprite = null;
            }

            if (fightingEnemy != null)
            {
                fightingEnemy.Delete();
                fightingEnemy = null;
            }

            if (fightingFlash != null)
            {
                fightingFlash.Delete();
                fightingFlash = null;
            }
        }

        static void InitOffsets(DataSource dataSource)
        {
            if (shadowSpriteInfo != null)
                return;

            SpriteInfo spriteInfo;

            for (uint i = 0u; i <= 629u; ++i)
            {
                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.SerfHead, i);

                if (spriteInfo != null)
                    headSpriteInfos.Add(HeadOffset + i, new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height));
            }

            for (uint i = 0u; i <= 540u; ++i)
            {
                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.SerfTorso, i);

                if (spriteInfo != null)
                {
                    torsoSpriteInfos.Add(TorsoOffset + i, new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height));
                    torsoDeltas.Add(TorsoOffset + i, new Position(spriteInfo.DeltaX, spriteInfo.DeltaY));
                }
            }

            spriteInfo = dataSource.GetSpriteInfo(Data.Resource.SerfShadow, 0u);
            shadowSpriteInfo = new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height);
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            sprite = spriteFactory.Create(0, 0, 0, 0, false, false);
            shadowSprite = spriteFactory.Create(0, 0, 0, 0, false, false);
            headSprite = spriteFactory.Create(0, 0, 0, 0, false, false);
        }

        public int GetHeadSprite(ref int body)
        {
            int hi = ((body >> 8) & 0xff) * 2;
            int lo = (body & 0xff) * 2;

            body = AppearanceIndex1[hi];
            int head = AppearanceIndex1[hi + 1];

            if (head < 0)
            {
                if (lo >= AppearanceIndex2.Length)
                {
                    Log.Error.Write("rendering", "Invalid serf body sprite index");
                    return -1;
                }

                body += AppearanceIndex2[lo];
            }
            else
            {
                if (lo >= AppearanceIndex2.Length - 1)
                {
                    Log.Error.Write("rendering", "Invalid serf body sprite index");
                    return -1;
                }

                body += AppearanceIndex2[lo];
                head += AppearanceIndex2[lo + 1];
            }

            return head;
        }

        static int GetBuildingBaseLine(uint mapPos, Game game, DataSource dataSource, Map map)
        {
            return GetBuildingBaseLine(game.GetBuildingAtPos(mapPos), dataSource, map);
        }

        static int GetBuildingBaseLine(Building building, DataSource dataSource, Map map)
        {
            if (building == null)
                return -1;

            uint sprite = 0;

            if (building.IsDone() || building.GetProgress() > 0xffff)
            {
                sprite = RenderBuilding.MapBuildingSprite[(int)building.BuildingType];
            }
            else
            {
                sprite = RenderBuilding.MapBuildingFrameSprite[(int)building.BuildingType];
            }

            var info = dataSource.GetSpriteInfo(Data.Resource.MapObject, sprite);

            return map.RenderMap.GetScreenPosition(building.Position).Y + info.OffsetY + info.Height;
        }

        static bool InFrontOfBuilding(int x, int width, Building building, DataSource dataSource, Map map)
        {
            if (building == null)
                return false;

            uint sprite = 0;

            if (building.IsDone() || building.GetProgress() > 0xffff)
            {
                sprite = RenderBuilding.MapBuildingSprite[(int)building.BuildingType];
            }
            else
            {
                sprite = RenderBuilding.MapBuildingFrameSprite[(int)building.BuildingType];
            }

            var info = dataSource.GetSpriteInfo(Data.Resource.MapObject, sprite);

            int buildingLeft = map.RenderMap.GetScreenPosition(building.Position).X + info.OffsetX;
            int buildingRight = buildingLeft + info.Width;

            int serfLeft = x;
            int serfRight = x + width;

            if (serfRight <= buildingLeft || serfLeft >= buildingRight)
                return false;

            return true;
        }

        void SetBaseLineOffset(int baseLineOffset)
        {
            sprite.BaseLineOffset = System.Math.Max(sprite.BaseLineOffset, baseLineOffset);

            if (headSprite != null)
            {
                int bodyBaseLine = sprite.Y + sprite.Height;
                int headBaseLine = headSprite.Y + headSprite.Height;

                headSprite.BaseLineOffset = sprite.BaseLineOffset + (bodyBaseLine - headBaseLine); // so we have the same baseline as the body
            }
        }

        void SetBaseLineOffset(Building building, int relativeOffset, Map map)
        {
            if (building == null)
                return;

            uint sprite = 0;

            if (building.IsDone() || building.GetProgress() > 0xffff)
            {
                sprite = RenderBuilding.MapBuildingSprite[(int)building.BuildingType];
            }
            else
            {
                sprite = RenderBuilding.MapBuildingFrameSprite[(int)building.BuildingType];
            }

            var info = dataSource.GetSpriteInfo(Data.Resource.MapObject, sprite);
            int buildingBaseLine = map.RenderMap.GetScreenPosition(building.Position).Y + info.OffsetY + info.Height;

            int serfBaseLine = this.sprite.Y + this.sprite.Height;

            if (serfBaseLine <= buildingBaseLine && serfBaseLine > buildingBaseLine - relativeOffset)
                SetBaseLine(buildingBaseLine + 1);
        }

        void SetBaseLine(int baseLine)
        {
            SetBaseLineOffset(System.Math.Max(0, 1 + baseLine - (sprite.Y + sprite.Height)));
        }

        public void Update(Game game, DataSource dataSource, int tick, Map map, uint pos)
        {
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Serfs);
            var renderPosition = map.RenderMap.GetScreenPosition(pos);
            int x = 0;
            int y = 0;
            int body = -1;
            int head = -1;
            // The baseline decides render order. The serfs should be in front of buildings and objects
            // in most cases even if their baseline is lower. So we add a small offset to the serf baseline.
            int baseLineOffset = 2;

            if (parentSerf != null)
            {
                // this is the fighting enemy
                int index = parentSerf.GetAttackingDefIndex();

                if (index != 0)
                {
                    Serf defSerf = game.GetSerf((uint)index);

                    if (defSerf.Animation < 0 || defSerf.Animation > 199 || defSerf.Counter < 0)
                    {
                        Log.Error.Write("viewport", $"bad animation for serf #{defSerf.Index} ({Serf.GetStateName(defSerf.SerfState)}): {defSerf.Animation},{defSerf.Counter}");
                        return;
                    }

                    body = GetActiveSerfBody();

                    Animation animation = dataSource.GetAnimation(defSerf.Animation, defSerf.Counter);
                    x = renderPosition.X + animation.X;
                    y = renderPosition.Y + animation.Y;

                    if (parentRenderSerf.sprite.BaseLineOffset > baseLineOffset)
                        baseLineOffset = parentRenderSerf.sprite.BaseLineOffset;
                }
            }
            else
            {
                if (map.HasSerf(pos) && serf.SerfState != Serf.State.IdleOnPath) // active serf
                {
                    if (serf.SerfState == Serf.State.Mining &&
                        (serf.GetMiningSubstate() == 3 ||
                        serf.GetMiningSubstate() == 4 ||
                        serf.GetMiningSubstate() == 9 ||
                        serf.GetMiningSubstate() == 10))
                    {
                        sprite.Visible = false;
                        headSprite.Visible = false;
                        shadowSprite.Visible = false;

                        return;
                    }

                    if (serf.Animation < 0 || serf.Animation > 199 || serf.Counter < 0)
                    {
                        Log.Error.Write("viewport", $"bad animation for serf #{serf.Index} ({Serf.GetStateName(serf.SerfState)}): {serf.Animation},{serf.Counter}");
                        return;
                    }

                    body = GetActiveSerfBody();

                    Animation animation = dataSource.GetAnimation(serf.Animation, serf.Counter);
                    x = renderPosition.X + animation.X;
                    y = renderPosition.Y + animation.Y;

                    DrawAdditionalSerf(game, tick, map, animation, renderPosition);
                }
                else if(fightingEnemy != null)
                {
                    fightingEnemy.Delete();
                    fightingEnemy = null;
                }

                if (map.GetIdleSerf(pos) && serf.SerfState == Serf.State.IdleOnPath) // idle serf
                {
                    body = GetIdleSerfBody(map, pos, out Position offset, tick);

                    x = renderPosition.X + offset.X;
                    y = renderPosition.Y + offset.Y;
                }
            }

            head = (body == -1) ? -1 : GetHeadSprite(ref body);

            if (body >= 0)
            {
                uint torsoSpriteIndexWithoutColor = TorsoOffset + (uint)body;
                uint torsoSpriteIndex = torsoSpriteIndexWithoutColor + serf.Player * TorsoPlayerOffset;

                var torsoSpriteInfo = torsoSpriteInfos[torsoSpriteIndexWithoutColor];

                sprite.X = x + torsoSpriteInfo.Position.X;
                sprite.Y = y + torsoSpriteInfo.Position.Y;
                sprite.Resize(torsoSpriteInfo.Size.Width, torsoSpriteInfo.Size.Height);
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(torsoSpriteIndex);
                sprite.BaseLineOffset = baseLineOffset;

                sprite.Visible = true;
                shadowSprite.Visible = true; // TODO
            }
            else
            {
                sprite.Visible = false;
                shadowSprite.Visible = false; // TODO
            }

            if (head >= 0)
            {
                uint headSpriteIndex = HeadOffset + (uint)head;
                uint torsoSpriteIndexWithoutColor = TorsoOffset + (uint)body;
                var headSpriteInfo = headSpriteInfos[headSpriteIndex];
                var torsoDelta = torsoDeltas[torsoSpriteIndexWithoutColor];

                headSprite.X = x + torsoDelta.X + headSpriteInfo.Position.X;
                headSprite.Y = y + torsoDelta.Y + headSpriteInfo.Position.Y;
                headSprite.Resize(headSpriteInfo.Size.Width, headSpriteInfo.Size.Height);
                headSprite.TextureAtlasOffset = textureAtlas.GetOffset(headSpriteIndex);

                int bodyBaseLine = sprite.Y + sprite.Height;
                int headBaseLine = headSprite.Y + headSprite.Height;

                headSprite.BaseLineOffset = baseLineOffset + (bodyBaseLine - headBaseLine); // so we have the same baseline as the body

                headSprite.Visible = true;
            }
            else
            {
                headSprite.Visible = false;
            }

            // adjust baseline
            if (sprite != null)
            {
                var building = serf.GetBuilding();

                if (building != null)
                {
                    int baseLine = GetBuildingBaseLine(building, dataSource, map);

                    if (baseLine != -1)
                    {
                        SetBaseLine(baseLine);
                    }
                }
                else if (serf.SerfState == Serf.State.StoneCutting)
                {
                    var info = dataSource.GetSpriteInfo(Data.Resource.MapObject, 65); // biggest rock
                    int rockBaseLine = map.RenderMap.GetScreenPosition(serf.Position).Y + info.OffsetY + info.Height;

                    SetBaseLine(rockBaseLine + 1);
                }
                else if (serf.SerfState == Serf.State.Logging)
                {
                    SetBaseLineOffset(12);
                }
                else
                {
                    // adjust baseline when walking around a building
                    if (map.GetObject(map.MoveLeft(pos)) > Map.Object.Flag && map.GetObject(map.MoveLeft(pos)) <= Map.Object.Castle)
                    {
                        building = game.GetBuildingAtPos(map.MoveLeft(pos));

                        if (InFrontOfBuilding(sprite.X, sprite.Width, building, dataSource, map))
                            SetBaseLineOffset(building, 1 + BuildingBaseLineOffsetRight[(int)building.BuildingType], map);
                    }
                    if (map.GetObject(map.MoveUpLeft(pos)) > Map.Object.Flag && map.GetObject(map.MoveUpLeft(pos)) <= Map.Object.Castle)
                    {
                        building = game.GetBuildingAtPos(map.MoveUpLeft(pos));

                        if (InFrontOfBuilding(sprite.X, sprite.Width, building, dataSource, map))
                            SetBaseLineOffset(building, 1 + BuildingBaseLineOffsetRight[(int)building.BuildingType], map);
                    }
                    if (map.GetObject(map.MoveRight(pos)) > Map.Object.Flag && map.GetObject(map.MoveRight(pos)) <= Map.Object.Castle)
                    {
                        building = game.GetBuildingAtPos(map.MoveRight(pos));

                        if (InFrontOfBuilding(sprite.X, sprite.Width, building, dataSource, map))
                            SetBaseLineOffset(building, 1 + BuildingBaseLineOffsetLeft[(int)building.BuildingType], map);
                    }
                    if (map.GetObject(map.MoveUp(pos)) > Map.Object.Flag && map.GetObject(map.MoveUp(pos)) <= Map.Object.Castle)
                    {
                        building = game.GetBuildingAtPos(map.MoveUp(pos));

                        if (InFrontOfBuilding(sprite.X, sprite.Width, building, dataSource, map))
                            SetBaseLineOffset(building, 1 + BuildingBaseLineOffsetLeft[(int)building.BuildingType], map);
                    }
                }
            }
        }

        void DrawAdditionalSerf(Game game, int tick, Map map, Animation animation, Position renderPosition)
        {
            if (serf.SerfState == Serf.State.KnightEngagingBuilding ||
                serf.SerfState == Serf.State.KnightPrepareAttacking ||
                serf.SerfState == Serf.State.KnightAttacking ||
                serf.SerfState == Serf.State.KnightPrepareAttackingFree ||
                serf.SerfState == Serf.State.KnightAttackingFree ||
                serf.SerfState == Serf.State.KnightAttackingVictoryFree ||
                serf.SerfState == Serf.State.KnightAttackingDefeatFree)
            {
                int index = serf.GetAttackingDefIndex();

                if (index != 0)
                {
                    Serf defSerf = game.GetSerf((uint)index);

                    if (fightingEnemy == null)
                    {
                        fightingEnemy = new RenderSerf(serf, this, defSerf, sprite.Layer, spriteFactory, dataSource, audio);
                    }

                    fightingEnemy.Update(game, dataSource, tick, map, defSerf.Position);
                }
            }
            else if (fightingEnemy != null)
            {
                fightingEnemy.Delete();
                fightingEnemy = null;
            }

            bool fightingFlashVisible = false;

            // Draw extra objects for fight
            if ((serf.SerfState == Serf.State.KnightAttacking || serf.SerfState == Serf.State.KnightAttackingFree) &&
                animation.Sprite >= 0x80 && animation.Sprite < 0xc0)
            {
                int index = serf.GetAttackingDefIndex();

                if (index != 0)
                {
                    Serf defSerf = game.GetSerf((uint)index);

                    if (serf.Animation >= 146 && serf.Animation < 156)
                    {
                        if ((serf.GetAttackingFieldD() == 0 || serf.GetAttackingFieldD() == 4) && serf.Counter < 32)
                        {
                            int anim = -1;

                            if (serf.GetAttackingFieldD() == 0)
                            {
                                anim = serf.Animation - 147;
                            }
                            else
                            {
                                anim = defSerf.Animation - 147;
                            }

                            uint sprite = (uint)(197 + ((serf.Counter >> 3) ^ 3));

                            if (fightingFlash == null)
                            {
                                fightingFlash = spriteFactory.Create(5, 5, 0, 0, false, false);
                                fightingFlash.Layer = this.sprite.Layer;
                            }

                            var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, sprite);
                            var offset = TextureAtlasManager.Instance.GetOrCreate(Layer.Serfs).GetOffset(5000u + sprite);

                            fightingFlashVisible = true;
                            fightingFlash.X = renderPosition.X + FightingFlashOffsets[2 * anim];
                            fightingFlash.Y = renderPosition.Y - FightingFlashOffsets[2 * anim + 1];
                            fightingFlash.Resize(info.Width, info.Height);
                            fightingFlash.TextureAtlasOffset = offset;
                            fightingFlash.BaseLineOffset = 1 + this.sprite.Y + this.sprite.Height - fightingFlash.Y - fightingFlash.Height;
                        }
                    }
                }
            }

            if (fightingFlash != null)
                fightingFlash.Visible = fightingFlashVisible;
        }

        int GetIdleSerfBody(Map map, uint pos, out Position offset, int tick)
        {
            int x = 0;
            int y = 0;
            int body = 0;

            if (map.IsInWater(pos))
            { 
                /* Sailor */
                x = 0;
                y = 0;
                body = 0x203;
            }
            else
            {
                /* Transporter */
                x = IdleAnimations3[2 * map.Paths(pos)];
                y = IdleAnimations3[2 * map.Paths(pos) + 1];
                body = IdleAnimations2[((tick + IdleAnimations1[pos & 0xf]) >> 3) & 0x7f];
            }

            offset = new Position(x, y);

            return body;
        }

        void PlaySound(Audio.TypeSfx type)
        {
            Audio.Player player = audio?.GetSoundPlayer();

            if (player != null)
            {
                player.PlayTrack((int)type);
            }
        }

        /* Extracted from obsolete update_map_serf_rows(). */
        /* Translate serf type into the corresponding sprite code. */
        int GetActiveSerfBody()
        {
            Animation animation = dataSource.GetAnimation(serf.Animation, serf.Counter);
            int t = animation.Sprite;

            switch (serf.GetSerfType())
            {
                case Serf.Type.Transporter:
                case Serf.Type.Generic:
                    if (serf.SerfState == Serf.State.IdleOnPath)
                    {
                        return -1;
                    }
                    else if ((serf.SerfState == Serf.State.Transporting ||
                             serf.SerfState == Serf.State.Delivering) &&
                             serf.GetDelivery() != 0)
                    {
                        t += TransporterType[serf.GetDelivery()];
                    }
                    break;
                case Serf.Type.Sailor:
                    if (serf.SerfState == Serf.State.Transporting && t < 0x80)
                    {
                        if (((t & 7) == 4 && !serf.PlayingSfx()) || (t & 7) == 3)
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.Rowing);
                        }
                        else
                        {
                            serf.StopPlayingSfx();
                        }
                    }

                    if ((serf.SerfState == Serf.State.Transporting &&
                         serf.GetDelivery() == 0) ||
                        serf.SerfState == Serf.State.LostSailor ||
                        serf.SerfState == Serf.State.FreeSailing)
                    {
                        if (t < 0x80)
                        {
                            if (((t & 7) == 4 && !serf.PlayingSfx()) || (t & 7) == 3)
                            {
                                serf.StartPlayingSfx();
                                PlaySound(Audio.TypeSfx.Rowing);
                            }
                            else
                            {
                                serf.StopPlayingSfx();
                            }
                        }

                        t += 0x200;
                    }
                    else if (serf.SerfState == Serf.State.Transporting)
                    {
                        t += SailorType[serf.GetDelivery()];
                    }
                    else
                    {
                        t += 0x100;
                    }
                    break;
                case Serf.Type.Digger:
                    if (t < 0x80)
                    {
                        t += 0x300;
                    }
                    else if (t == 0x83 || t == 0x84)
                    {
                        if (t == 0x83 || !serf.PlayingSfx())
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.Digging);
                        }

                        t += 0x380;
                    }
                    else
                    {
                        serf.StopPlayingSfx();
                        t += 0x380;
                    }
                    break;
                case Serf.Type.Builder:
                    if (t < 0x80)
                    {
                        t += 0x500;
                    }
                    else if ((t & 7) == 4 || (t & 7) == 5)
                    {
                        if ((t & 7) == 4 || !serf.PlayingSfx())
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.HammerBlow);
                        }

                        t += 0x580;
                    }
                    else
                    {
                        serf.StopPlayingSfx();
                        t += 0x580;
                    }
                    break;
                case Serf.Type.TransporterInventory:
                    if (serf.SerfState == Serf.State.BuildingCastle)
                    {
                        return -1;
                    }
                    else
                    {
                        int res = serf.GetDelivery();
                        t += TransporterType[res];
                    }
                    break;
                case Serf.Type.Lumberjack:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.FreeWalking &&
                            serf.GetFreeWalkingNegDist1() == -128 &&
                            serf.GetFreeWalkingNegDist2() == 1)
                        {
                            t += 0x1000;
                        }
                        else
                        {
                            t += 0xb00;
                        }
                    }
                    else if ((t == 0x86 && !serf.PlayingSfx()) ||
                       t == 0x85)
                    {
                        serf.StartPlayingSfx();
                        PlaySound(Audio.TypeSfx.AxBlow);
                        /* TODO Dangerous reference to unknown state vars.
                           It is probably free walking. */
                        if (serf.GetFreeWalkingNegDist2() == 0 &&
                            serf.Counter < 64)
                        {
                            PlaySound(Audio.TypeSfx.TreeFall);
                        }

                        t += 0xe80;
                    }
                    else if (t != 0x86)
                    {
                        serf.StopPlayingSfx();
                        t += 0xe80;
                    }
                    break;
                case Serf.Type.Sawmiller:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding &&
                            serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            t += 0x1700;
                        }
                        else
                        {
                            t += 0xc00;
                        }
                    }
                    else
                    {
                        /* player_num += 4; ??? */
                        if (t == 0xb3 || t == 0xbb || t == 0xc3 || t == 0xcb || (!serf.PlayingSfx() && (t == 0xb7 || t == 0xbf || t == 0xc7 || t == 0xcf)))
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.Sawing);
                        }
                        else if (t != 0xb7 && t != 0xbf && t != 0xc7 && t != 0xcf)
                        {
                            serf.StopPlayingSfx();
                        }

                        t += 0x1580;
                    }
                    break;
                case Serf.Type.Stonecutter:
                    if (t < 0x80)
                    {
                        if ((serf.SerfState == Serf.State.FreeWalking &&
                             serf.GetFreeWalkingNegDist1() == -128 &&
                             serf.GetFreeWalkingNegDist2() == 1) ||
                            (serf.SerfState == Serf.State.StoneCutting &&
                             serf.GetFreeWalkingNegDist1() == 2))
                        {
                            t += 0x1200;
                        }
                        else
                        {
                            t += 0xd00;
                        }
                    }
                    else if (t == 0x85 || (t == 0x86 && !serf.PlayingSfx()))
                    {
                        serf.StartPlayingSfx();
                        PlaySound(Audio.TypeSfx.PickBlow);
                        t += 0x1280;
                    }
                    else if (t != 0x86)
                    {
                        serf.StopPlayingSfx();
                        t += 0x1280;
                    }
                    break;
                case Serf.Type.Forester:
                    if (t < 0x80)
                    {
                        t += 0xe00;
                    }
                    else if (t == 0x86 || (t == 0x87 && !serf.PlayingSfx()))
                    {
                        serf.StartPlayingSfx();
                        PlaySound(Audio.TypeSfx.Planting);
                        t += 0x1080;
                    }
                    else if (t != 0x87)
                    {
                        serf.StopPlayingSfx();
                        t += 0x1080;
                    }
                    break;
                case Serf.Type.Miner:
                    if (t < 0x80)
                    {
                        if ((serf.SerfState != Serf.State.Mining ||
                             serf.GetMiningRes() == 0) &&
                            (serf.SerfState != Serf.State.LeavingBuilding ||
                             serf.GetLeavingBuildingNextState() !=
                             Serf.State.DropResourceOut))
                        {
                            t += 0x1800;
                        }
                        else
                        {
                            Resource.Type res = Resource.Type.None;

                            switch (serf.SerfState)
                            {
                                case Serf.State.Mining:
                                    res = (Resource.Type)(serf.GetMiningRes() - 1);
                                    break;
                                case Serf.State.LeavingBuilding:
                                    res = (Resource.Type)(serf.GetLeavingBuildingFieldB() - 1);
                                    break;
                                default:
                                    Debug.NotReached();
                                    break;
                            }

                            switch (res)
                            {
                                case Resource.Type.Stone: t += 0x2700; break;
                                case Resource.Type.IronOre: t += 0x2500; break;
                                case Resource.Type.Coal: t += 0x2600; break;
                                case Resource.Type.GoldOre: t += 0x2400; break;
                                default: Debug.NotReached(); break;
                            }
                        }
                    }
                    else
                    {
                        t += 0x2a80;
                    }
                    break;
                case Serf.Type.Smelter:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            if (serf.GetLeavingBuildingFieldB() == 1 + (int)Resource.Type.Steel)
                            {
                                t += 0x2900;
                            }
                            else
                            {
                                t += 0x2800;
                            }
                        }
                        else
                        {
                            t += 0x1900;
                        }
                    }
                    else
                    {
                        /* edi10 += 4; */
                        t += 0x2980;
                    }
                    break;
                case Serf.Type.Fisher:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.FreeWalking &&
                            serf.GetFreeWalkingNegDist1() == -128 &&
                            serf.GetFreeWalkingNegDist2() == 1)
                        {
                            t += 0x2f00;
                        }
                        else
                        {
                            t += 0x2c00;
                        }
                    }
                    else
                    {
                        if (t != 0x80 && t != 0x87 && t != 0x88 && t != 0x8f)
                        {
                            PlaySound(Audio.TypeSfx.FishingRodReel);
                        }

                        /* TODO no check for state */
                        if (serf.GetFreeWalkingNegDist2() == 1)
                        {
                            t += 0x2d80;
                        }
                        else
                        {
                            t += 0x2c80;
                        }
                    }
                    break;
                case Serf.Type.PigFarmer:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            t += 0x3400;
                        }
                        else
                        {
                            t += 0x3200;
                        }
                    }
                    else
                    {
                        t += 0x3280;
                    }
                    break;
                case Serf.Type.Butcher:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            t += 0x3a00;
                        }
                        else
                        {
                            t += 0x3700;
                        }
                    }
                    else
                    {
                        /* edi10 += 4; */
                        if ((t == 0xb2 || t == 0xba || t == 0xc2 || t == 0xca) && !serf.PlayingSfx())
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.BackswordBlow);
                        }
                        else if (t != 0xb2 && t != 0xba && t != 0xc2 && t != 0xca)
                        {
                            serf.StopPlayingSfx();
                        }

                        t += 0x3780;
                    }
                    break;
                case Serf.Type.Farmer:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.FreeWalking &&
                            serf.GetFreeWalkingNegDist1() == -128 &&
                            serf.GetFreeWalkingNegDist2() == 1)
                        {
                            t += 0x4000;
                        }
                        else
                        {
                            t += 0x3d00;
                        }
                    }
                    else
                    {
                        /* TODO access to state without state check */
                        if (serf.GetFreeWalkingNegDist1() == 0)
                        {
                            t += 0x3d80;
                        }
                        else if (t == 0x83 || (t == 0x84 && !serf.PlayingSfx()))
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.Mowing);
                            t += 0x3e80;
                        }
                        else if (t != 0x83 && t != 0x84)
                        {
                            serf.StopPlayingSfx();
                            t += 0x3e80;
                        }
                    }
                    break;
                case Serf.Type.Miller:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            t += 0x4500;
                        }
                        else
                        {
                            t += 0x4300;
                        }
                    }
                    else
                    {
                        /* edi10 += 4; */
                        t += 0x4380;
                    }
                    break;
                case Serf.Type.Baker:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            t += 0x4a00;
                        }
                        else
                        {
                            t += 0x4800;
                        }
                    }
                    else
                    {
                        /* edi10 += 4; */
                        t += 0x4880;
                    }
                    break;
                case Serf.Type.BoatBuilder:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            t += 0x5000;
                        }
                        else
                        {
                            t += 0x4e00;
                        }
                    }
                    else if (t == 0x84 || t == 0x85)
                    {
                        if (t == 0x84 || !serf.PlayingSfx())
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.WoodHammering);
                        }

                        t += 0x4e80;
                    }
                    else
                    {
                        serf.StopPlayingSfx();
                        t += 0x4e80;
                    }
                    break;
                case Serf.Type.Toolmaker:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            switch ((Resource.Type)(serf.GetLeavingBuildingFieldB() - 1))
                            {
                                case Resource.Type.Shovel: t += 0x5a00; break;
                                case Resource.Type.Hammer: t += 0x5b00; break;
                                case Resource.Type.Rod: t += 0x5c00; break;
                                case Resource.Type.Cleaver: t += 0x5d00; break;
                                case Resource.Type.Scythe: t += 0x5e00; break;
                                case Resource.Type.Axe: t += 0x6100; break;
                                case Resource.Type.Saw: t += 0x6200; break;
                                case Resource.Type.Pick: t += 0x6300; break;
                                case Resource.Type.Pincer: t += 0x6400; break;
                                default: Debug.NotReached(); break;
                            }
                        }
                        else
                        {
                            t += 0x5800;
                        }
                    }
                    else
                    {
                        /* edi10 += 4; */
                        if (t == 0x83 || (t == 0xb2 && !serf.PlayingSfx()))
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.Sawing);
                        }
                        else if (t == 0x87 || (t == 0xb6 && !serf.PlayingSfx()))
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.WoodHammering);
                        }
                        else if (t != 0xb2 && t != 0xb6)
                        {
                            serf.StopPlayingSfx();
                        }

                        t += 0x5880;
                    }
                    break;
                case Serf.Type.WeaponSmith:
                    if (t < 0x80)
                    {
                        if (serf.SerfState == Serf.State.LeavingBuilding && serf.GetLeavingBuildingNextState() == Serf.State.DropResourceOut)
                        {
                            if (serf.GetLeavingBuildingFieldB() == 1 + (int)Resource.Type.Sword)
                            {
                                t += 0x5500;
                            }
                            else
                            {
                                t += 0x5400;
                            }
                        }
                        else
                        {
                            t += 0x5200;
                        }
                    }
                    else
                    {
                        /* edi10 += 4; */
                        if (t == 0x83 || (t == 0x84 && !serf.PlayingSfx()))
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.MetalHammering);
                        }
                        else if (t != 0x84)
                        {
                            serf.StopPlayingSfx();
                        }

                        t += 0x5280;
                    }
                    break;
                case Serf.Type.Geologist:
                    if (t < 0x80)
                    {
                        t += 0x3900;
                    }
                    else if (t == 0x83 || t == 0x84 || t == 0x86)
                    {
                        if (t == 0x83 || !serf.PlayingSfx())
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.GeologistSampling);
                        }

                        t += 0x4c80;
                    }
                    else if (t == 0x8c || t == 0x8d)
                    {
                        if (t == 0x8c || !serf.PlayingSfx())
                        {
                            serf.StartPlayingSfx();
                            PlaySound(Audio.TypeSfx.ResourceFound);
                        }

                        t += 0x4c80;
                    }
                    else
                    {
                        serf.StopPlayingSfx();
                        t += 0x4c80;
                    }
                    break;
                case Serf.Type.Knight0:
                case Serf.Type.Knight1:
                case Serf.Type.Knight2:
                case Serf.Type.Knight3:
                case Serf.Type.Knight4:
                    {
                        int k = serf.GetSerfType() - Serf.Type.Knight0;

                        if (t < 0x80)
                        {
                            t += 0x7800 + 0x100 * k;
                        }
                        else if (t < 0xc0)
                        {
                            if (serf.SerfState == Serf.State.KnightAttacking ||
                                serf.SerfState == Serf.State.KnightAttackingFree)
                            {
                                if (serf.Counter >= 24 || serf.Counter < 8)
                                {
                                    serf.StopPlayingSfx();
                                }
                                else if (!serf.PlayingSfx())
                                {
                                    serf.StartPlayingSfx();

                                    if (serf.GetAttackingFieldD() == 0 || serf.GetAttackingFieldD() == 4)
                                    {
                                        PlaySound(Audio.TypeSfx.Fight01);
                                    }
                                    else if (serf.GetAttackingFieldD() == 2)
                                    {
                                        /* TODO when is TypeSfxFight02 played? */
                                        PlaySound(Audio.TypeSfx.Fight03);
                                    }
                                    else
                                    {
                                        PlaySound(Audio.TypeSfx.Fight04);
                                    }
                                }
                            }

                            t += 0x7cd0 + 0x200 * k;
                        }
                        else
                        {
                            t += 0x7d90 + 0x200 * k;
                        }
                    }
                    break;
                case Serf.Type.Dead:
                    if ((!serf.PlayingSfx() && (t == 2 || t == 5)) || t == 1 || t == 4)
                    {
                        serf.StartPlayingSfx();
                        PlaySound(Audio.TypeSfx.SerfDying);
                    }
                    else
                    {
                        serf.StopPlayingSfx();
                    }
                    t += 0x8700;
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            return t;
        }
    }
}
