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

        Building building = null;
        ISprite frameSprite = null;
        ISprite frameShadowSprite = null;

        public RenderBuilding(Building building, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.building = building;

            Initialize();

            if (frameSprite != null)
                frameSprite.Layer = renderLayer;

            if (frameShadowSprite != null)
                frameShadowSprite.Layer = renderLayer;
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
            uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

            var spriteData = dataSource.GetSprite(Data.Resource.MapObject, spriteIndex, Sprite.Color.Transparent);
            var spriteShadowData = dataSource.GetSprite(Data.Resource.MapShadow, spriteIndex, Sprite.Color.Transparent);
            var frameSpriteData = dataSource.GetSprite(Data.Resource.MapObject, frameSpriteIndex, Sprite.Color.Transparent);
            var frameSpriteShadowData = dataSource.GetSprite(Data.Resource.MapShadow, frameSpriteIndex, Sprite.Color.Transparent);

            //sprite = spriteFactory.Create(spriteData)
            // TODO
            // sprite = spriteFactory.Create(...);
        }

        public void UpdateProgress()
        {
            // Note: While building we have 3 stages:
            // 1: Nothing is build, only the corner stone (or cross while leveling)
            // 2: The frame is being built (0-100%)
            // 3: The building is being built on top of the frame (0-100%)

            // To display building progress we have to draw only parts of sprites.
            // Therefore we need to adjust positioning and texture coords beforehand.

            if (building.IsDone())
            {

            }
            else
            {
                // Progress = 0: Leveling is active
                // Progress = 1: Leveling is done (or not necessary)
                // BitTest(Progress, 15) = true: Frame finished         [This means Progress >= 32768]
                // Progress = 0xffff: Building finished                 [This means Progress >= 65536]

                var progress = building.GetProgress();

                if (progress == 0) // cross
                {

                }
                else if (progress == 1) // corner stone
                {

                }
                else
                {
                    var textureAtlas = TextureAtlasManager.Instance.GetOrCreate((int)Layer.Buildings);

                    float factorX = 1.0f / textureAtlas.Texture.Width;  // pixel factor x direction
                    float factorY = 1.0f / textureAtlas.Texture.Height; // pixel factor y direction
                    uint frameSpriteIndex = MapBuildingFrameSprite[(int)building.BuildingType];

                    if (!Misc.BitTest(progress, 15)) // building frame
                    {

                    }
                    else // building on top of frame
                    {
                        // draw full frame
                        textureAtlas.GetOffset(frameSpriteIndex);
                    }
                }
            }
        }
    }
}
