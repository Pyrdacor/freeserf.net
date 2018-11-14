/*
 * RenderFlag.cs - Handles flag rendering
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
    // TODO: flag colors
    internal class RenderFlag : RenderObject
    {
        Flag flag = null;
        // sprite index 128-143 (16 sprites)
        static Position[] spriteOffsets = null;
        static Position[] shadowSpriteOffsets = null;

        static readonly int[] ResPos = new int[] 
        {
             6, -4,
            10, -2,
            -4, -4,
            10,  2,
            -8, -2,
             6,  4,
            -8,  2,
            -4,  4
        };

        public RenderFlag(Flag flag, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.flag = flag;

            Initialize();

            InitOffsets(dataSource);
        }

        static void InitOffsets(DataSource dataSource)
        {
            if (spriteOffsets == null)
            {
                spriteOffsets = new Position[16];
                shadowSpriteOffsets = new Position[16];

                for (int i = 0; i < 16; ++i)
                {
                    var sprite = dataSource.GetSprite(Data.Resource.MapObject, 128u + (uint)i, Sprite.Color.Transparent);

                    spriteOffsets[i] = new Position(sprite.OffsetX, sprite.OffsetY);

                    sprite = dataSource.GetSprite(Data.Resource.MapShadow, 128u + (uint)i, Sprite.Color.Transparent);

                    shadowSpriteOffsets[i] = new Position(sprite.OffsetX, sprite.OffsetY);
                }
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            // max sprite size is 16x19 pixels but we use 16x20 for better base line matching
            sprite = spriteFactory.Create(16, 20, 0, 0, false);
            shadowSprite = spriteFactory.Create(16, 20, 0, 0, false);
        }

        public void Update(uint tick, RenderMap map, uint pos)
        {
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate((int)Layer.Objects);
            uint offset = (tick >> 3) & 3;
            uint spriteIndex = 128u + offset;

            uint flagSpriteIndex = spriteIndex + flag.GetOwner() * 16u; // player colors

            var renderPosition = map.GetObjectRenderPosition(pos);

            sprite.X = renderPosition.X + spriteOffsets[(int)offset].X;
            sprite.Y = renderPosition.Y + spriteOffsets[(int)offset].Y;
            shadowSprite.X = renderPosition.X + shadowSpriteOffsets[(int)offset].X;
            shadowSprite.Y = renderPosition.Y + shadowSpriteOffsets[(int)offset].Y;

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(flagSpriteIndex);
            shadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(1000u + spriteIndex);
        }
    }
}
