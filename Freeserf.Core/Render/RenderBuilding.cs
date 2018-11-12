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

namespace Freeserf.Render
{
    // TODO: burning
    internal class RenderBuilding : RenderObject
    {
        static readonly uint[] MapBuildingFrameSprite = new uint[]
        {
            0, 0xba, 0xba, 0xba, 0xba,
            0xb9, 0xb9, 0xb9, 0xb9,
            0xba, 0xc1, 0xba, 0xb1, 0xb8, 0xb1, 0xbb,
            0xb7, 0xb5, 0xb6, 0xb0, 0xb8, 0xb3, 0xaf, 0xb4
        };

        static readonly uint[] MapBuildingSprite = new uint[]
        {
            0, 0xa7, 0xa8, 0xae, 0xa9,
            0xa3, 0xa4, 0xa5, 0xa6,
            0xaa, 0xc0, 0xab, 0x9a, 0x9c, 0x9b, 0xbc,
            0xa2, 0xa0, 0xa1, 0x99, 0x9d, 0x9e, 0x98, 0x9f, 0xb2
        };

        const uint CrossSprite = 0x90;
        const uint CornerStoneSprite = 0x91;
        const uint ShadowOffset = 1000u;

        Building building = null;
        ISprite frameSprite = null;
        ISprite frameShadowSprite = null;
        ISprite crossOrStoneSprite = null;
        ISprite burningSprite = null;

        public RenderBuilding(Building building, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.building = building;

            Initialize();

            if (frameSprite != null)
                frameSprite.Layer = renderLayer;

            if (frameShadowSprite != null)
                frameShadowSprite.Layer = renderLayer;

            if (crossOrStoneSprite != null)
                crossOrStoneSprite.Layer = renderLayer;

            if (burningSprite != null)
                burningSprite.Layer = renderLayer;

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate((int)Layer.Buildings);

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(MapBuildingSprite[(int)building.BuildingType]);
            shadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(ShadowOffset + MapBuildingSprite[(int)building.BuildingType]);

            if (frameSprite != null)
                frameSprite.TextureAtlasOffset = textureAtlas.GetOffset(MapBuildingFrameSprite[(int)building.BuildingType]);

            if (frameShadowSprite != null)
                frameShadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(ShadowOffset + MapBuildingFrameSprite[(int)building.BuildingType]);
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
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            uint spriteIndex = MapBuildingSprite[(int)building.BuildingType];

            var spriteData = dataSource.GetSprite(Data.Resource.MapObject, spriteIndex, Sprite.Color.Transparent);
            var spriteShadowData = dataSource.GetSprite(Data.Resource.MapShadow, spriteIndex, Sprite.Color.Transparent);

            sprite = spriteFactory.Create((int)spriteData.Width, (int)spriteData.Height, 0, 0, true);
            shadowSprite = spriteFactory.Create((int)spriteShadowData.Width, (int)spriteShadowData.Height, 0, 0, true);

            if (!building.IsDone() && building.BuildingType != Building.Type.Castle)
            {
                uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

                var frameSpriteData = dataSource.GetSprite(Data.Resource.MapObject, frameSpriteIndex, Sprite.Color.Transparent);
                var frameSpriteShadowData = dataSource.GetSprite(Data.Resource.MapShadow, frameSpriteIndex, Sprite.Color.Transparent);

                frameSprite = spriteFactory.Create((int)frameSpriteData.Width, (int)frameSpriteData.Height, 0, 0, true);
                frameShadowSprite = spriteFactory.Create((int)frameSpriteShadowData.Width, (int)frameSpriteShadowData.Height, 0, 0, true);

                // we expect the same sprite size for cross and corner stone!
                var crossOrStoneSpriteData = dataSource.GetSprite(Data.Resource.MapObject, CrossSprite, Sprite.Color.Transparent);

                crossOrStoneSprite = spriteFactory.Create((int)crossOrStoneSpriteData.Width, (int)crossOrStoneSpriteData.Height, 0, 0, false);
            }

            if (building.IsBurning())
            {
                // burning is in Data.Resource.GameObject beginning at 135
                // TODO
            }
        }

        public override void Delete()
        {
            base.Delete();

            if (frameSprite != null)
            {
                frameSprite.Delete();
                frameSprite = null;
            }

            if (frameShadowSprite != null)
            {
                frameShadowSprite.Delete();
                frameShadowSprite = null;
            }

            if (burningSprite != null)
            {
                burningSprite.Delete();
                burningSprite = null;
            }

            building = null;
        }

        Position GetBuildingMaskOffset(ITextureAtlas textureAtlas, int spriteHeight)
        {
            // sprite index 0 holds the mask
            var offset = textureAtlas.GetOffset(0u);

            if (building.IsDone())
            {
                offset.Y = 100;
                return offset;
            }
            else
            {
                float progress;
                var buildingProgess = building.GetProgress();

                if (buildingProgess <= 1)
                {
                    return offset;
                }
                else if (Misc.BitTest(buildingProgess, 15))
                {
                    progress = 2 * (buildingProgess & 0x7fffu) / 0xffffu;
                }
                else
                {
                    progress = 2 * buildingProgess / 0xffffu;
                }

                int pixelOffset = 100 - spriteHeight;

                offset.Y += pixelOffset;
                offset.Y += Misc.Round(progress * spriteHeight);

                return offset;
            }
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
            // We use a mask to display only parts. The mask is added as sprite index 0
            // inside the texture atlas for buildings. It contains 200 pixels in height
            // and 64 pixels in width. The upper 100 pixels are full transparent black and
            // the lower 100 pixels are full opaque white. The mask position is adjusted to
            // overlay the sprite in the correct way.

            // Note: The biggest building is the castle with 64x97 therefore we use 100 pixels
            // for the height areas.

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate((int)Layer.Buildings);

            if (building.IsDone())
            {
                // If the building is finished we no longer need
                // updates and we no longer need the frame sprites.

                if (frameSprite != null)
                {
                    frameSprite.Delete();
                    frameSprite = null;
                }

                if (frameShadowSprite != null)
                {
                    frameShadowSprite.Delete();
                    frameShadowSprite = null;
                }

                (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

                if (!building.IsBurning()) // we need updates while burning
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
                    // Progress = 0xffff: Building finished                 [This means Progress >= 65536]

                    var progress = building.GetProgress();

                    uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

                    (frameSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, frameSprite.Height);

                    if (frameShadowSprite != null)
                        (frameShadowSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, frameSprite.Height); // it is correct to use sprite.Height here


                    if (!Misc.BitTest(progress, 15)) // building frame
                    {
                        (sprite as IMaskedSprite).MaskTextureAtlasOffset = textureAtlas.GetOffset(0u); // the sprite is not visible

                        if (shadowSprite != null)
                            (shadowSprite as IMaskedSprite).MaskTextureAtlasOffset = textureAtlas.GetOffset(0u); // the sprite is not visible
                    }
                    else // building on top of frame
                    {
                        (sprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height);

                        if (shadowSprite != null)
                            (shadowSprite as IMaskedSprite).MaskTextureAtlasOffset = GetBuildingMaskOffset(textureAtlas, sprite.Height); // it is correct to use sprite.Height here
                    }
                }
            }

            // TODO: overlay burning sprite?
            if (building.IsBurning())
            {
                // TODO
            }

            return true;
        }
    }
}
