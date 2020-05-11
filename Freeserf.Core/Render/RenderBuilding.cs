/*
 * RenderBuilding.cs - Handles building rendering
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

using Freeserf.Data;
using System;
using System.Collections.Generic;

namespace Freeserf.Render
{
    using Data = Data.Data;
    using MapPos = UInt32;

    internal class RenderBuilding : RenderObject
    {
        internal static readonly uint[] MapBuildingFrameSprite = new uint[]
        {
            0, 0xba, 0xba, 0xba, 0xba,
            0xb9, 0xb9, 0xb9, 0xb9,
            0xba, 0xc1, 0xba, 0xb1, 0xb8, 0xb1, 0xbb,
            0xb7, 0xb5, 0xb6, 0xb0, 0xb8, 0xb3, 0xaf, 0xb4
        };

        internal static readonly uint[] MapBuildingSprite = new uint[]
        {
            0, 0xa7, 0xa8, 0xae, 0xa9,
            0xa3, 0xa4, 0xa5, 0xa6,
            0xaa, 0xc0, 0xab, 0x9a, 0x9c, 0x9b, 0xbc,
            0xa2, 0xa0, 0xa1, 0x99, 0x9d, 0x9e, 0x98, 0x9f, 0xb2
        };

        static readonly int[] BuildingAnimationOffsetFromType = new int[]
        {
            0, 10, 26, 39, 49, 62, 78, 97, 97, 116,
            129, 157, 167, 198, 211, 236, 255, 277, 305, 324,
            349, 362, 381, 418, 446
        };

        static readonly int[] BuildingBurnAnimation = new int[]
        {
            // Unfinished 
            0, -8, 2,
            8, 0, 4,
            0, 8, 2, -1,

            // Fisher 
            0, -8, -10,
            0, 4, -12,
            8, -8, 4,
            8, 7, 4,
            0, -2, 8, -1,

            // Lumberjack 
            0, 3, -13,
            0, -8, -10,
            8, 9, 3,
            8, -6, 3, -1,

            // Boat builder 
            0, -1, -12,
            8, -8, 11,
            8, 7, 5, -1,

            // Stone cutter 
            0, 6, -14,
            0, -9, -11,
            8, -8, 5,
            8, 6, 5, -1,

            // Stone mine 
            8, -4, -40,
            8, -8, -15,
            8, 3, -14,
            8, -9, 4,
            8, 6, 5, -1,

            // Coal mine 
            8, -4, -40,
            8, -1, -18,
            8, -8, -15,
            8, 6, 2,
            8, 0, 8,
            8, -8, 9, -1,

            // Iron mine, gold mine 
            8, -4, -40,
            8, -2, -19,
            8, -9, -14,
            8, -8, 2,
            8, 6, 2,
            0, -4, 8, -1,

            // Forester 
            0, 8, -11,
            0, -6, -8,
            8, -8, 4,
            8, 6, 4, -1,

            // Stock 
            0, -2, -25,
            0, 6, -17,
            0, -9, -16,
            8, -21, 1,
            8, 21, 2,
            0, 15, 18,
            0, -16, 10,
            8, -8, 15,
            8, 5, 15, -1,

            // Hut 
            0, 0, -11,
            8, -8, 5,
            8, 8, 5, -1,

            // Farm 
            8, 22, -2,
            8, 7, -5,
            8, -3, -1,
            8, -23, 0,
            8, -12, 4,
            0, 25, 5,
            0, 21, 13,
            0, -17, 8,
            0, -10, 15,
            0, -2, 15, -1,

            // Butcher 
            8, -15, 3,
            8, 20, 3,
            8, 7, 3,
            8, -4, 3, -1,

            // Pig farm 
            8, 0, -2,
            8, 22, 1,
            8, 15, 5,
            8, -20, -1,
            8, -11, 3,
            0, 20, 12,
            0, -16, 7,
            0, -12, 14, -1,

            // Mill 
            0, 7, -33,
            0, 5, -20,
            8, -2, -24,
            8, -6, 1,
            8, 4, 2,
            0, -3, 6, -1,

            // Baker 
            0, -15, -16,
            0, -4, -19,
            0, 3, -16,
            8, -13, 2,
            8, -9, 7,
            8, 6, 7,
            0, 17, 1, -1,

            // Saw mill 
            0, 7, -19,
            0, -1, -14,
            0, 16, -13,
            0, 5, -8,
            8, 14, 4,
            0, 10, 9,
            0, -17, 8,
            8, -11, 10,
            8, -1, 12, -1,

            // Steel smelter 
            0, 5, -19,
            0, 16, -16,
            8, -14, 2,
            8, -10, 5,
            8, 15, 5,
            8, 2, 5, -1,

            // Tool maker 
            8, 7, -19,
            0, -11, -17,
            0, -4, -11,
            0, 12, -10,
            8, -20, 0,
            8, -15, 7,
            8, 1, 7,
            8, 15, 7, -1,

            // Weapon smith 
            8, -15, 1,
            8, -10, 3,
            8, 20, 3,
            8, 5, 3, -1,

            // Tower 
            0, -6, -30,
            0, 7, -14,
            8, -11, -3,
            0, -8, 4,
            8, 9, 5,
            8, -4, 5, -1,

            // Fortress 
            0, -3, -30,
            0, -15, -26,
            0, 21, -29,
            0, -13, -17,
            8, 4, -11,
            8, -2, -6,
            8, -22, 0,
            8, -17, 8,
            8, 20, 1,
            8, 10, 8,
            8, 4, 13,
            8, -11, 15, -1,

            // Gold smelter 
            0, -15, -20,
            0, 10, -22,
            0, -3, -25,
            0, -8, -10,
            0, 7, -10,
            0, -13, 2,
            8, -8, 5,
            8, 6, 5,
            0, 16, 6, -1,

            // Castle 
            0, 11, -46,
            0, -19, -42,
            8, 1, -27,
            8, 10, -13,
            0, -7, -24,
            8, -16, -6,
            0, -23, 4,
            8, -2, 0,
            8, 12, 12,
            8, -14, 17,
            8, -4, 19,
            0, 13, 19, -1
        };

        const uint CrossSprite = 0x90;
        const uint CornerStoneSprite = 0x91;
        const uint ShadowOffset = 1000u;
        const uint BuildProgressMaskIndex = 5000u;

        Building building = null;
        IMaskedSprite frameSprite = null;
        IMaskedSprite frameShadowSprite = null;
        IMaskedSprite crossOrStoneSprite = null;
        IMaskedSprite[] burningSprites = null;
        List<ISprite> stones = new List<ISprite>(5);
        List<ISprite> planks = new List<ISprite>(5);
        readonly ISpriteFactory spriteFactory = null;
        readonly DataSource dataSource = null;
        readonly IRenderLayer materialLayer = null;
        readonly List<ISprite> additionalSprites = new List<ISprite>();

        static readonly Dictionary<uint, Position> spriteOffsets = new Dictionary<uint, Position>();
        static readonly Dictionary<uint, Position> shadowSpriteOffsets = new Dictionary<uint, Position>();
        static readonly Dictionary<uint, Position> frameSpriteOffsets = new Dictionary<uint, Position>();
        static readonly Dictionary<uint, Position> frameShadowSpriteOffsets = new Dictionary<uint, Position>();
        static Position crossOrStoneSpriteOffset = null;
        static Rect[] materialSpriteInfos = null;
        static Position[] materialTextureOffsets = null;
        static Rect[] burningSpriteInfos = null;

        Audio.Audio audio = null;

        public RenderBuilding(Building building, IRenderLayer renderLayer, IRenderLayer materialLayer, ISpriteFactory spriteFactory, DataSource dataSource, Audio.Audio audio)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.building = building;
            this.spriteFactory = spriteFactory;
            this.dataSource = dataSource;
            this.materialLayer = materialLayer;
            this.audio = audio;

            Initialize();

            if (frameSprite != null)
            {
                frameSprite.Layer = renderLayer;
                frameSprite.BaseLineOffset = -4;
            }

            if (frameShadowSprite != null)
            {
                frameShadowSprite.Layer = renderLayer;
                frameShadowSprite.BaseLineOffset = -4;
            }

            if (crossOrStoneSprite != null)
            {
                crossOrStoneSprite.Layer = renderLayer;
                crossOrStoneSprite.BaseLineOffset = 0;
            }

            if (burningSprites != null)
            {
                for (int i = 0; i < burningSprites.Length; ++i)
                    burningSprites[i].Layer = renderLayer;
            }

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Buildings);

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(MapBuildingSprite[(int)building.BuildingType]);
            (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);
            shadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(ShadowOffset + MapBuildingSprite[(int)building.BuildingType]);
            (shadowSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

            if (frameSprite != null)
                frameSprite.TextureAtlasOffset = textureAtlas.GetOffset(MapBuildingFrameSprite[(int)building.BuildingType]);

            if (frameShadowSprite != null)
                frameShadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(ShadowOffset + MapBuildingFrameSprite[(int)building.BuildingType]);

            if (crossOrStoneSprite != null)
            {
                var offset = textureAtlas.GetOffset(BuildProgressMaskIndex);
                offset.Y += 100; // make it full visible

                crossOrStoneSprite.MaskTextureAtlasOffset = offset;
            }

            if (burningSprites != null)
            {
                var offset = textureAtlas.GetOffset(BuildProgressMaskIndex);
                offset.Y += 100; // make it full visible

                for (int i = 0; i < burningSprites.Length; ++i)
                    burningSprites[i].MaskTextureAtlasOffset = offset;
            }

            InitOffsets(dataSource);

            CreateSpecialBuildingSprites();
        }

        public override bool Visible
        {
            get => sprite.Visible;
            set
            {
                base.Visible = value;

                if (frameSprite != null)
                    frameSprite.Visible = value;

                if (frameShadowSprite != null)
                    frameShadowSprite.Visible = value;

                if (crossOrStoneSprite != null)
                    crossOrStoneSprite.Visible = value;

                if (burningSprites != null)
                {
                    for (int i = 0; i < burningSprites.Length; ++i)
                        burningSprites[i].Visible = value;
                }
            }
        }

        void InitOffsets(DataSource dataSource)
        {
            SpriteInfo spriteInfo;

            if (crossOrStoneSpriteOffset == null)
            {
                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, CrossSprite);
                crossOrStoneSpriteOffset = new Position(spriteInfo.OffsetX, spriteInfo.OffsetY);
            }

            uint spriteIndex = MapBuildingSprite[(int)building.BuildingType];

            if (!spriteOffsets.ContainsKey(spriteIndex))
            {
                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);
                spriteOffsets.Add(spriteIndex, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));

                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, spriteIndex);
                shadowSpriteOffsets.Add(spriteIndex, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));

                // the mill has 3 additional sprites
                if (building.BuildingType == Building.Type.Mill)
                {
                    for (uint i = 1; i <= 3; ++i)
                    {
                        spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, spriteIndex + i);
                        spriteOffsets.Add(spriteIndex + i, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));

                        spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, spriteIndex + i);
                        shadowSpriteOffsets.Add(spriteIndex + i, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));
                    }
                }
            }

            if (building.BuildingType != Building.Type.Castle)
            {
                uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

                if (!frameSpriteOffsets.ContainsKey(frameSpriteIndex))
                {
                    spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, frameSpriteIndex);
                    frameSpriteOffsets.Add(frameSpriteIndex, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));

                    spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, frameSpriteIndex);
                    frameShadowSpriteOffsets.Add(frameSpriteIndex, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));
                }
            }

            if (materialSpriteInfos == null)
            {
                materialSpriteInfos = new Rect[2];

                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.GameObject, (uint)Resource.Type.Stone);
                materialSpriteInfos[0] = new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height);

                spriteInfo = dataSource.GetSpriteInfo(Data.Resource.GameObject, (uint)Resource.Type.Plank);
                materialSpriteInfos[1] = new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height);
            }

            if (materialTextureOffsets == null)
            {
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Objects);
                materialTextureOffsets = new Position[2];

                materialTextureOffsets[0] = textureAtlas.GetOffset(2000u + (uint)Resource.Type.Stone);
                materialTextureOffsets[1] = textureAtlas.GetOffset(2000u + (uint)Resource.Type.Plank);
            }

            if (burningSpriteInfos == null)
            {
                burningSpriteInfos = new Rect[16];

                for (uint i = 0; i < 16; ++i)
                {
                    spriteInfo = dataSource.GetSpriteInfo(Data.Resource.GameObject, 135u + i);
                    burningSpriteInfos[i] = new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height);
                }
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            uint spriteIndex = MapBuildingSprite[(int)building.BuildingType];

            var spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);
            var spriteShadowInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, spriteIndex);

            sprite = spriteFactory.Create(spriteInfo.Width, spriteInfo.Height, 0, 0, true, false);
            shadowSprite = spriteFactory.Create(spriteShadowInfo.Width, spriteShadowInfo.Height, 0, 0, true, false);

            if (!building.IsDone && building.BuildingType != Building.Type.Castle)
            {
                uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

                var frameSpriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, frameSpriteIndex);
                var frameSpriteShadowInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, frameSpriteIndex);

                frameSprite = spriteFactory.Create(frameSpriteInfo.Width, frameSpriteInfo.Height, 0, 0, true, false) as IMaskedSprite;
                frameShadowSprite = spriteFactory.Create(frameSpriteShadowInfo.Width, frameSpriteShadowInfo.Height, 0, 0, true, false) as IMaskedSprite;

                // we expect the same sprite size for cross and corner stone!
                var crossOrStoneSpriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, CrossSprite);

                // Note: Even the sprite doesn't have to be masked it is important to use similar sprite types in the same layer.
                crossOrStoneSprite = spriteFactory.Create(crossOrStoneSpriteInfo.Width, crossOrStoneSpriteInfo.Height, 0, 0, true, false) as IMaskedSprite;
            }

            burningSprites = new IMaskedSprite[12];

            for (int i = 0; i < 12; ++i)
                burningSprites[i] = spriteFactory.Create(0, 0, 0, 0, true, false) as IMaskedSprite;

            if (building.IsDone)
            {
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Buildings);

                (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);
            }
        }

        public override void Delete()
        {
            base.Delete();

            frameSprite?.Delete();
            frameSprite = null;

            frameShadowSprite?.Delete();
            frameShadowSprite = null;

            crossOrStoneSprite?.Delete();
            crossOrStoneSprite = null;

            if (burningSprites != null)
            {
                for (int i = 0; i < burningSprites.Length; ++i)
                    burningSprites[i]?.Delete();

                burningSprites = null;
            }

            for (int i = 0; i < stones.Count; ++i)
            {
                stones[i]?.Delete();
                stones[i] = null;
            }

            for (int i = 0; i < planks.Count; ++i)
            {
                planks[i]?.Delete();
                planks[i] = null;
            }

            foreach (var sprite in additionalSprites)
                sprite?.Delete();

            additionalSprites?.Clear();

            building = null;
        }

        Position GetBuildingMaskOffset(ITextureAtlas textureAtlas, int spriteHeight)
        {
            var offset = textureAtlas.GetOffset(BuildProgressMaskIndex);

            if (building.IsDone)
            {
                offset.Y += 100;
                return offset;
            }
            else
            {
                float progress;
                var buildingProgess = building.Progress;

                if (buildingProgess <= 1)
                {
                    return offset;
                }
                else if (building.FrameFinished && building.BuildingType != Building.Type.Castle)
                {
                    progress = 2.0f * (buildingProgess & 0x7fffu) / 0xffffu;
                }
                else
                {
                    progress = 2.0f * buildingProgess / 0xffffu;
                }

                int pixelOffset = 100 - spriteHeight;

                offset.Y += pixelOffset;
                offset.Y += Misc.Round(progress * spriteHeight);

                return offset;
            }
        }

        public void Update(int tick, RenderMap map, MapPos position)
        {
            var renderPosition = map.CoordinateSpace.TileSpaceToViewSpace(position);

            uint spriteIndex = MapBuildingSprite[(int)building.BuildingType];

            sprite.X = renderPosition.X + spriteOffsets[spriteIndex].X;
            sprite.Y = renderPosition.Y + spriteOffsets[spriteIndex].Y;
            shadowSprite.X = renderPosition.X + shadowSpriteOffsets[spriteIndex].X;
            shadowSprite.Y = renderPosition.Y + shadowSpriteOffsets[spriteIndex].Y;

            if (frameSprite != null)
            {
                uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

                frameSprite.X = renderPosition.X + frameSpriteOffsets[frameSpriteIndex].X;
                frameSprite.Y = renderPosition.Y + frameSpriteOffsets[frameSpriteIndex].Y;

                if (frameShadowSprite != null)
                {
                    frameShadowSprite.X = renderPosition.X + frameShadowSpriteOffsets[frameSpriteIndex].X;
                    frameShadowSprite.Y = renderPosition.Y + frameShadowSpriteOffsets[frameSpriteIndex].Y;
                }
            }

            if (crossOrStoneSprite != null)
            {
                crossOrStoneSprite.X = renderPosition.X + crossOrStoneSpriteOffset.X;
                crossOrStoneSprite.Y = renderPosition.Y + crossOrStoneSpriteOffset.Y;
                crossOrStoneSprite.BaseLineOffset = System.Math.Min(0, (frameSprite.Y + frameSprite.Height + frameSprite.BaseLineOffset - 2) - (crossOrStoneSprite.Y + crossOrStoneSprite.Height));
            }

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Buildings);

            (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

            // draw materials

            // Stones waiting
            for (int i = 0; i < stones.Count; ++i)
            {
                stones[i].X = renderPosition.X + materialSpriteInfos[0].Position.X + 10 - i * 3;
                stones[i].Y = renderPosition.Y + materialSpriteInfos[0].Position.Y - 8 + i;

                if (frameSprite != null)
                    stones[i].BaseLineOffset = System.Math.Min(0, (frameSprite.Y + frameSprite.Height + frameSprite.BaseLineOffset - 2) - (stones[i].Y + stones[i].Height));
            }

            // Planks waiting
            for (int i = 0; i < planks.Count; ++i)
            {
                planks[i].X = renderPosition.X + materialSpriteInfos[1].Position.X + 12 - i * 3;
                planks[i].Y = renderPosition.Y + materialSpriteInfos[1].Position.Y - 6 + i;

                if (frameSprite != null)
                    planks[i].BaseLineOffset = System.Math.Min(0, (frameSprite.Y + frameSprite.Height + frameSprite.BaseLineOffset - 2) - (planks[i].Y + planks[i].Height));
            }

            if (building.IsBurning)
            {
                DrawBurningSprite(renderPosition, tick);
            }
            else
            {
                UpdateSpecialBuilding(renderPosition, tick);
            }

            if (!sprite.Visible)
                building.StopPlayingSfx();
        }

        /// <summary>
        /// Returns true as long as further progress updating is necessary.
        /// 
        /// This is mostly true for building in progress but also for burning.
        /// </summary>
        /// <returns></returns>
        public bool UpdateProgress()
        {
            if (building == null || building.BuildingType == Building.Type.None) // the building was deleted or is not valid
                return false;

            // Note: While building we have 3 stages:
            // 1: Nothing is build, only the corner stone (or cross while leveling)
            // 2: The frame is being built (0-100%)
            // 3: The building is being built on top of the frame (0-100%)

            // To display building progress we have to draw only parts of sprites.
            // Therefore we need to adjust positioning and texture coords beforehand.
            // We use a mask to display only parts. The mask is added as sprite index 5000
            // inside the texture atlas for buildings. It contains 200 pixels in height
            // and 80 pixels in width. The upper 100 pixels are full transparent black and
            // the lower 100 pixels are full opaque white. The mask position is adjusted to
            // overlay the sprite in the correct way.

            // Note: The biggest building is the castle with 64x97 therefore we use 100 pixels
            // for the height areas. The widest image is the castle shadow with 80 pixels.

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Buildings);

            if (building.IsDone)
            {
                // If the building is finished we no longer need
                // updates and we no longer need the frame sprites.

                frameSprite?.Delete();
                frameSprite = null;

                frameShadowSprite?.Delete();
                frameShadowSprite = null;

                crossOrStoneSprite?.Delete();
                crossOrStoneSprite = null;

                (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

                if (!building.IsBurning) // we need updates while burning
                    return false;
            }
            else
            {
                if (building.BuildingType == Building.Type.Castle)
                {
                    (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

                    if (shadowSprite != null)
                        (shadowSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height); // it is correct to use sprite.Height here
                }
                else
                {
                    // Progress = 0: Leveling is active
                    // Progress = 1: Leveling is done (or not necessary)
                    // BitTest(Progress, 15) = true: Frame finished         [This means Progress >= 32768]
                    // Progress = 0xffff: Building finished                 [This means Progress == 65535]

                    var progress = building.Progress;

                    if (progress == 0)
                        crossOrStoneSprite.TextureAtlasOffset = textureAtlas.GetOffset(CrossSprite);
                    else if (!Misc.BitTest(progress, 15))
                        crossOrStoneSprite.TextureAtlasOffset = textureAtlas.GetOffset(CornerStoneSprite);
                    else if (crossOrStoneSprite != null)
                    {
                        crossOrStoneSprite?.Delete();
                        crossOrStoneSprite = null;
                    }

                    if (!Misc.BitTest(progress, 15)) // building frame
                    {
                        (sprite as IMaskedSprite).MaskTextureAtlasOffset = textureAtlas.GetOffset(BuildProgressMaskIndex); // the sprite is not visible

                        if (shadowSprite != null)
                            (shadowSprite as IMaskedSprite).MaskTextureAtlasOffset = textureAtlas.GetOffset(BuildProgressMaskIndex); // the sprite is not visible

                        (frameSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, frameSprite.Height);

                        if (frameShadowSprite != null)
                            (frameShadowSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, frameSprite.Height); // it is correct to use sprite.Height here
                    }
                    else // building on top of frame
                    {
                        (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

                        if (shadowSprite != null)
                            (shadowSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height); // it is correct to use sprite.Height here

                        var offset = textureAtlas.GetOffset(BuildProgressMaskIndex);
                        offset.Y += 100;

                        (frameSprite as IMaskedSprite).MaskTextureAtlasOffset = offset;

                        if (frameShadowSprite != null)
                            (frameShadowSprite as IMaskedSprite).MaskTextureAtlasOffset = offset;
                    }

                    // Update stone waiting
                    uint stone = building.WaitingStone; // max is 5

                    for (int i = stones.Count - 1; i >= stone; --i) // remove stones that are not present anymore
                    {
                        stones[i]?.Delete();
                        stones.RemoveAt(i);
                    }

                    for (int i = stones.Count; i < stone; ++i) // add missing stone sprites
                    {
                        var sprite = spriteFactory.Create(materialSpriteInfos[0].Size.Width, materialSpriteInfos[0].Size.Height, materialTextureOffsets[0].X, materialTextureOffsets[0].Y, false, false);
                        sprite.Layer = materialLayer;
                        sprite.Visible = true;
                        stones.Add(sprite);
                    }

                    // Update planks waiting
                    uint plank = building.WaitingPlanks; // max is 5

                    for (int i = planks.Count - 1; i >= plank; --i) // remove stones that are not present anymore
                    {
                        planks[i]?.Delete();
                        planks.RemoveAt(i);
                    }

                    for (int i = planks.Count; i < plank; ++i) // add missing stone sprites
                    {
                        var sprite = spriteFactory.Create(materialSpriteInfos[1].Size.Width, materialSpriteInfos[1].Size.Height, materialTextureOffsets[1].X, materialTextureOffsets[1].Y, false, false);
                        sprite.Layer = materialLayer;
                        sprite.Visible = true;
                        planks.Add(sprite);
                    }
                }
            }

            return true;
        }

        void DrawBurningSprite(Position renderPosition, int tick)
        {
            // Play sound effect
            if (sprite.Visible &&
                ((building.BurningCounter >> 3) & 3) == 3 &&
                !building.IsPlayingSfx)
            {
                building.StartPlayingSfx();
                PlaySound(Audio.Audio.TypeSfx.Burning);
            }
            // Stop playing sound effect if not on screen
            else if (!sprite.Visible && building.IsPlayingSfx)
            {
                building.StopPlayingSfx();
                StopSound();
            }

            ushort delta = (ushort)(tick - building.Tick);
            building.Tick = (uint)tick;

            if (building.BurningCounter >= delta)
            {
                building.DecreaseBurningCounter(delta);

                int type = 0;

                if (building.IsDone ||
                    building.Progress >= 16000)
                {
                    type = (int)building.BuildingType;
                }

                int offset = ((building.BurningCounter >> 3) & 7) ^ 7;
                int animationIndex = BuildingAnimationOffsetFromType[type];
                int burningSpriteIndex = 0;

                while (BuildingBurnAnimation[animationIndex] >= 0)
                {
                    int x = renderPosition.X;
                    int y = renderPosition.Y;
                    var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Buildings);
                    int index = BuildingBurnAnimation[animationIndex] + offset;
                    var spriteInfo = burningSpriteInfos[index];

                    var burningSprite = burningSprites[burningSpriteIndex++];

                    burningSprite.X = x + spriteInfo.Position.X + BuildingBurnAnimation[animationIndex + 1];
                    burningSprite.Y = y + spriteInfo.Position.Y + BuildingBurnAnimation[animationIndex + 2];
                    burningSprite.Resize(spriteInfo.Size.Width, spriteInfo.Size.Height);
                    burningSprite.TextureAtlasOffset = textureAtlas.GetOffset(2000u + (uint)index);
                    burningSprite.BaseLineOffset = System.Math.Max(0, (sprite.Y + sprite.Height) - (burningSprite.Y + burningSprite.Height) + 1);
                    burningSprite.Visible = true;

                    offset = (offset + 3) & 7;
                    animationIndex += 3;
                }

                while (burningSpriteIndex < 12)
                {
                    burningSprites[burningSpriteIndex++].Visible = false;
                }
            }
            else
            {
                building.BurningCounter = 0;
            }
        }

        void AddDummySprites(int n)
        {
            for (int i = 0; i < n; ++i)
                additionalSprites.Add(spriteFactory.Create(0, 0, 0, 0, false, false));
        }

        void CreateSpecialBuildingSprites()
        {
            switch (building.BuildingType)
            {
                case Building.Type.Baker:
                case Building.Type.Boatbuilder:
                case Building.Type.GoldSmelter:
                case Building.Type.Hut:
                case Building.Type.SteelSmelter:
                case Building.Type.Tower:
                case Building.Type.WeaponSmith:
                    AddDummySprites(1);
                    break;
                case Building.Type.Fortress:
                    // two flags
                    AddDummySprites(2);
                    break;
                case Building.Type.Mill:
                    // no additional sprites needed
                    break;
                case Building.Type.PigFarm:
                    // max 8 pigs
                    AddDummySprites(8);
                    break;
                case Building.Type.StoneMine:
                case Building.Type.CoalMine:
                case Building.Type.IronMine:
                case Building.Type.GoldMine:
                    // elevator and the rope
                    AddDummySprites(2);
                    break;
            }
        }

        void PlaySound(Audio.Audio.TypeSfx type)
        {
            audio?.GetSoundPlayer()?.PlayTrack((int)type);
        }

        void StopSound()
        {
            audio?.GetSoundPlayer()?.Stop();
        }

        static readonly int[] PigfarmAnimation = new int[]
        {
            0xa2, 0, 0xa2, 0, 0xa2, 0, 0xa2, 0, 0xa2, 0, 0xa3, 0,
            0xa2, 1, 0xa3, 1, 0xa2, 2, 0xa3, 2, 0xa2, 3, 0xa3, 3,
            0xa2, 4, 0xa3, 4, 0xa6, 4, 0xa6, 4, 0xa6, 4, 0xa6, 4,
            0xa4, 4, 0xa5, 4, 0xa4, 3, 0xa5, 3, 0xa4, 2, 0xa5, 2,
            0xa4, 1, 0xa5, 1, 0xa4, 0, 0xa5, 0, 0xa2, 0, 0xa2, 0,
            0xa6, 0, 0xa6, 0, 0xa6, 0, 0xa2, 0, 0xa7, 0, 0xa8, 0,
            0xa7, 0, 0xa8, 0, 0xa7, 0, 0xa8, 0, 0xa7, 0, 0xa8, 0,
            0xa7, 0, 0xa8, 0, 0xa7, 0, 0xa8, 0, 0xa7, 0, 0xa8, 0,
            0xa7, 0, 0xa8, 0, 0xa7, 0, 0xa2, 0, 0xa2, 0, 0xa2, 0,
            0xa2, 0, 0xa6, 0, 0xa6, 0, 0xa6, 0, 0xa6, 0, 0xa6, 0,
            0xa6, 0, 0xa2, 0, 0xa2, 0, 0xa7, 0, 0xa8, 0, 0xa9, 0,
            0xaa, 0, 0xab, 0, 0xac, 0, 0xad, 0, 0xac, 0, 0xad, 0,
            0xac, 0, 0xad, 0, 0xac, 0, 0xad, 0, 0xac, 0, 0xad, 0,
            0xac, 0, 0xad, 0, 0xac, 0, 0xad, 0, 0xac, 0, 0xab, 0,
            0xaa, 0, 0xa9, 0, 0xa8, 0, 0xa7, 0, 0xa2, 0, 0xa2, 0,
            0xa2, 0, 0xa2, 0, 0xa3, 0, 0xa2, 1, 0xa3, 1, 0xa2, 1,
            0xa3, 2, 0xa2, 2, 0xa3, 2, 0xa7, 2, 0xa8, 2, 0xa7, 2,
            0xa8, 2, 0xa7, 2, 0xa8, 2, 0xa7, 2, 0xa8, 2, 0xa7, 2,
            0xa8, 2, 0xa7, 2, 0xa8, 2, 0xa7, 2, 0xa8, 2, 0xa7, 2,
            0xa2, 2, 0xa2, 2, 0xa6, 2, 0xa6, 2, 0xa6, 2, 0xa6, 2,
            0xa4, 2, 0xa5, 2, 0xa4, 1, 0xa5, 1, 0xa4, 0, 0xa5, 0,
            0xa2, 0, 0xa2, 0
        };

        static int[] PigsLayout = new int[]
        {
             40,   2, 11,
            460,   0, 17,
            420, -11,  8,
             90, -11, 19,
            280,   8,  8,
            140,  -2,  6,
            180,  -8, 13,
            320,  13, 14,
        };

        void UpdateSpecialBuilding(Position renderPosition, int tick)
        {
            if (!building.IsDone)
                return;

            if (!sprite.Visible)
            {
                foreach (var additionalSprite in additionalSprites)
                    additionalSprite.Visible = false;

                return;
            }

            const uint SpecialObjectOffset = 10000u;
            var random = new Random();
            var textureAtlasBuildings = TextureAtlasManager.Instance.GetOrCreate(Layer.Buildings);
            var textureAtlasObjects = TextureAtlasManager.Instance.GetOrCreate(Layer.Objects);

            switch (building.BuildingType)
            {
                case Building.Type.Baker:
                    // draw smoke
                    if (building.IsActive)
                    {
                        int i = (tick >> 3) & 7;

                        uint spriteIndex = 153u + (uint)i;
                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[0].X = renderPosition.X + info.OffsetX + 5;
                        additionalSprites[0].Y = renderPosition.Y + info.OffsetY - 21;
                        additionalSprites[0].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[0].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[0].BaseLineOffset = sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[0].Y; // otherwise we wouldn't see them
                        additionalSprites[0].Resize(info.Width, info.Height);
                        additionalSprites[0].Visible = true;
                    }
                    else
                    {
                        additionalSprites[0].Visible = false;
                    }
                    break;
                case Building.Type.Boatbuilder:
                    // draw the boat that is built
                    if (building.GetResourceCountInStock(1) > 0)
                    {
                        uint spriteIndex = 173u + building.GetResourceCountInStock(1);
                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[0].X = renderPosition.X + 3;
                        additionalSprites[0].Y = renderPosition.Y + 13;
                        additionalSprites[0].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[0].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[0].BaseLineOffset = sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[0].Y; // otherwise we wouldn't see it
                        additionalSprites[0].Resize(info.Width, info.Height);
                        additionalSprites[0].Visible = true;
                    }
                    else
                    {
                        additionalSprites[0].Visible = false;
                    }
                    break;
                case Building.Type.Fortress:
                    // draw two flags
                    DrawOccupationFlag(renderPosition.X - 12, renderPosition.Y - 21, 0.5f, textureAtlasObjects, SpecialObjectOffset, tick);
                    DrawOccupationFlag(renderPosition.X + 22, renderPosition.Y - 34, 0.5f, textureAtlasObjects, SpecialObjectOffset, tick, true);
                    break;
                case Building.Type.GoldSmelter:
                    // draw smoke
                    if (building.IsActive)
                    {
                        int i = (tick >> 3) & 7;

                        if (i == 0 || (i == 7 && !building.IsPlayingSfx))
                        {
                            building.StartPlayingSfx();
                            PlaySound(Audio.Audio.TypeSfx.GoldBoils);
                        }
                        else if (i != 7)
                        {
                            building.StopPlayingSfx();
                        }

                        uint spriteIndex = 127u + (uint)i;
                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[0].X = renderPosition.X + info.OffsetX - 7;
                        additionalSprites[0].Y = renderPosition.Y + info.OffsetY - 33;
                        additionalSprites[0].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[0].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[0].BaseLineOffset = sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[0].Y; // otherwise we wouldn't see them
                        additionalSprites[0].Resize(info.Width, info.Height);
                        additionalSprites[0].Visible = true;
                    }
                    else
                    {
                        additionalSprites[0].Visible = false;
                    }
                    break;
                case Building.Type.Hut:
                    // draw the flag
                    DrawOccupationFlag(renderPosition.X - 14, renderPosition.Y + 2, 2.0f, textureAtlasObjects, SpecialObjectOffset, tick);
                    break;
                case Building.Type.Mill:
                    {
                        uint spriteIndex = MapBuildingSprite[(int)Building.Type.Mill];

                        // draw the mill rotation
                        if (building.IsActive)
                        {
                            if (((tick >> 4) & 3) != 0)
                            {
                                building.StopPlayingSfx();
                            }
                            else if (!building.IsPlayingSfx)
                            {
                                building.StartPlayingSfx();
                                PlaySound(Audio.Audio.TypeSfx.MillGrinding);
                            }

                            spriteIndex += ((uint)tick >> 4) & 3u;
                        }

                        sprite.TextureAtlasOffset = textureAtlasBuildings.GetOffset(spriteIndex);
                        shadowSprite.TextureAtlasOffset = textureAtlasBuildings.GetOffset(ShadowOffset + spriteIndex);
                        sprite.X = renderPosition.X + spriteOffsets[spriteIndex].X;
                        sprite.Y = renderPosition.Y + spriteOffsets[spriteIndex].Y;
                        shadowSprite.X = renderPosition.X + shadowSpriteOffsets[spriteIndex].X;
                        shadowSprite.Y = renderPosition.Y + shadowSpriteOffsets[spriteIndex].Y;
                    }
                    break;
                case Building.Type.PigFarm:
                    {
                        // draw the pigs
                        int pigCount = (int)building.GetResourceCountInStock(1);

                        if (pigCount > 0)
                        {
                            if ((random.Next() & 0x7f) < pigCount)
                            {
                                PlaySound(Audio.Audio.TypeSfx.PigOink);
                            }

                            for (int p = 0; p < pigCount; ++p)
                            {
                                int i = (PigsLayout[p * 3] + (tick >> 3)) & 0xfe;
                                uint spriteIndex = (uint)PigfarmAnimation[i] - 1u;

                                var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                                additionalSprites[p].X = renderPosition.X + PigfarmAnimation[i + 1] + PigsLayout[p * 3 + 1];
                                additionalSprites[p].Y = renderPosition.Y + PigsLayout[p * 3 + 2];
                                additionalSprites[p].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                                additionalSprites[p].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                                additionalSprites[p].BaseLineOffset = sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[p].Y; // otherwise we wouldn't see them
                                additionalSprites[p].Resize(info.Width, info.Height);
                                additionalSprites[p].Visible = true;
                            }
                        }

                        // hide the rest
                        for (int p = pigCount; p < 8; ++p)
                            additionalSprites[p].Visible = false;
                    }
                    break;
                case Building.Type.StoneMine:
                case Building.Type.CoalMine:
                case Building.Type.IronMine:
                case Building.Type.GoldMine:
                    // TODO: doesn't look perfect yet
                    // draw the elevator
                    if (building.IsActive)
                    {
                        uint spriteIndex = 151u;

                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[0].X = renderPosition.X + info.OffsetX - 6;
                        additionalSprites[0].Y = renderPosition.Y + info.OffsetY - 39;
                        additionalSprites[0].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[0].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[0].BaseLineOffset = 0; // the elevator should be hidden by the mine
                        additionalSprites[0].Resize(info.Width, info.Height);
                        additionalSprites[0].Visible = true;
                    }
                    else
                    {
                        additionalSprites[0].Visible = false;
                    }
                    // draw the rope
                    if (building.IsPlayingSfx)
                    {
                        uint spriteIndex = 152u;

                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[1].X = renderPosition.X + info.OffsetX - 6;
                        additionalSprites[1].Y = renderPosition.Y + info.OffsetY - 39;
                        additionalSprites[1].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[1].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[1].BaseLineOffset = 0; // the elevator should be hidden by the mine
                        additionalSprites[1].Resize(info.Width, info.Height);
                        additionalSprites[1].Visible = true;

                        var position = building.Position;

                        if ((((tick + (position & 0xff)) >> 3) & 7) == 0 && random.Next() < 40000)
                        {
                            PlaySound(Audio.Audio.TypeSfx.Elevator);
                        }
                    }
                    else
                    {
                        additionalSprites[1].Visible = false;
                    }
                    break;
                case Building.Type.SteelSmelter:
                    // draw smoke
                    if (building.IsActive)
                    {
                        int i = (tick >> 3) & 7;

                        if (i == 0 || (i == 7 && !building.IsPlayingSfx))
                        {
                            building.StartPlayingSfx();
                            PlaySound(Audio.Audio.TypeSfx.GoldBoils);
                        }
                        else if (i != 7)
                        {
                            building.StopPlayingSfx();
                        }

                        uint spriteIndex = 127u + (uint)i;
                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[0].X = renderPosition.X + info.OffsetX + 6;
                        additionalSprites[0].Y = renderPosition.Y + info.OffsetY - 32;
                        additionalSprites[0].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[0].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[0].BaseLineOffset = sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[0].Y; // otherwise we wouldn't see them
                        additionalSprites[0].Resize(info.Width, info.Height);
                        additionalSprites[0].Visible = true;
                    }
                    else
                    {
                        additionalSprites[0].Visible = false;
                    }
                    break;
                case Building.Type.Tower:
                    // draw flag
                    DrawOccupationFlag(renderPosition.X + 13, renderPosition.Y - 18, 1.0f, textureAtlasObjects, SpecialObjectOffset, tick);
                    break;
                case Building.Type.WeaponSmith:
                    // draw smoke
                    if (building.IsActive)
                    {
                        int i = (tick >> 3) & 7;

                        uint spriteIndex = 127u + (uint)i;
                        var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                        additionalSprites[0].X = renderPosition.X + info.OffsetX - 16;
                        additionalSprites[0].Y = renderPosition.Y + info.OffsetY - 21;
                        additionalSprites[0].TextureAtlasOffset = textureAtlasObjects.GetOffset(SpecialObjectOffset + spriteIndex);
                        additionalSprites[0].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                        additionalSprites[0].BaseLineOffset = sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[0].Y; // otherwise we wouldn't see them
                        additionalSprites[0].Resize(info.Width, info.Height);
                        additionalSprites[0].Visible = true;
                    }
                    else
                    {
                        additionalSprites[0].Visible = false;
                    }
                    break;
            }
        }

        void DrawOccupationFlag(int x, int y, float mul, ITextureAtlas textureAtlas, uint spriteIndexOffset, int tick, bool second = false)
        {
            int index = (second) ? 1 : 0;

            if (building.HasKnight())
            {
                uint tickBase = (second) ? ((((uint)tick >> 3) + 2) & 3) : (((uint)tick >> 3) & 3);
                uint spriteIndex = 181u + tickBase + 4 * building.ThreatLevel;
                var info = dataSource.GetSpriteInfo(Data.Resource.GameObject, spriteIndex);

                additionalSprites[index].X = x;
                if (second)
                    additionalSprites[index].Y = y - ((int)building.KnightCount + 1) / 2;
                else
                    additionalSprites[index].Y = y - Misc.Round(mul * building.KnightCount);
                additionalSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(spriteIndexOffset + spriteIndex);
                additionalSprites[index].Layer = materialLayer; // we use the material layer for special objects (it is the map object layer)
                additionalSprites[index].BaseLineOffset = System.Math.Max(0, sprite.Y + sprite.Height + sprite.BaseLineOffset + 1 - additionalSprites[index].Y); // otherwise we wouldn't see them
                additionalSprites[index].Resize(info.Width, info.Height);
                additionalSprites[index].Visible = true;
            }
            else
            {
                additionalSprites[index].Visible = false;
            }
        }
    }
}
