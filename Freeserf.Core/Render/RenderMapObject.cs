/*
 * RenderMapObject.cs - Handles map object rendering
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
    internal class RenderMapObject : RenderObject
    {
        Map.Object objectType = Map.Object.None;
        static readonly Dictionary<uint, Position> spriteOffsets = new Dictionary<uint, Position>();
        static readonly Dictionary<uint, Position> shadowSpriteOffsets = new Dictionary<uint, Position>();

        public RenderMapObject(Map.Object objectType, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.objectType = objectType;

            Initialize();

            InitOffsets(dataSource);
        }

        void InitOffsets(DataSource dataSource)
        {
            Sprite sprite;
            var color = Sprite.Color.Transparent;

            uint spriteIndex = (uint)objectType - 8;

            if (!spriteOffsets.ContainsKey(spriteIndex))
            {
                sprite = dataSource.GetSprite(Data.Resource.MapObject, spriteIndex, color);
                spriteOffsets.Add(spriteIndex, new Position(sprite.OffsetX, sprite.OffsetY));

                sprite = dataSource.GetSprite(Data.Resource.MapShadow, spriteIndex, color);
                shadowSpriteOffsets.Add(spriteIndex, new Position(sprite.OffsetX, sprite.OffsetY));
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            uint spriteIndex = (uint)objectType - 8;

            var spriteInfo = dataSource.GetSprite(Data.Resource.MapObject, spriteIndex, Sprite.Color.Transparent);
            var shadowInfo = dataSource.GetSprite(Data.Resource.MapShadow, spriteIndex, Sprite.Color.Transparent);

            sprite = spriteFactory.Create((int)spriteInfo.Width, (int)spriteInfo.Height, 0, 0, false);
            shadowSprite = spriteFactory.Create((int)shadowInfo.Width, (int)shadowInfo.Height, 0, 0, false);
        }

        public void Update(uint tick, RenderMap map, uint pos)
        {
            // TODO: animations
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate((int)Layer.Objects);
            uint spriteIndex = (uint)objectType - 8;

            var renderPosition = map.GetObjectRenderPosition(pos);

            sprite.X = renderPosition.X + spriteOffsets[spriteIndex].X;
            sprite.Y = renderPosition.Y + spriteOffsets[spriteIndex].Y;
            shadowSprite.X = renderPosition.X + shadowSpriteOffsets[spriteIndex].X;
            shadowSprite.Y = renderPosition.Y + shadowSpriteOffsets[spriteIndex].Y;

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(spriteIndex);
            shadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(1000u + spriteIndex);
        }

        // TODO:
        // trees, stones etc
        public void ChangeObjectType(Map.Object objectType)
        {
            if (objectType == this.objectType)
                return; // nothing changed

            if (this.objectType == Map.Object.None) // from None to something valid
            {
                // do we support this? can this even happen?
            }

            if (objectType == Map.Object.None) // from something valid to None
            {
                // this is handled by Game so this should not happen at all
                Debug.NotReached();
                return;
            }

            // TODO: set tex coords and size
        }
    }
}
