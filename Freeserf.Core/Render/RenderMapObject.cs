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

using Freeserf.Data;
using System.Collections.Generic;

namespace Freeserf.Render
{
    using Data = Data.Data;

    internal class RenderMapObject : RenderObject
    {
        Map.Object objectType = Map.Object.None;
        DataSource dataSource = null;
        static Dictionary<uint, Position> spriteOffsets = null;
        static Dictionary<uint, Position> shadowSpriteOffsets = null;

        public RenderMapObject(Map.Object objectType, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.objectType = objectType;
            this.dataSource = dataSource;

            Initialize();

            InitOffsets(dataSource);
        }

        static void InitOffsets(DataSource dataSource)
        {
            if (spriteOffsets == null)
            {
                spriteOffsets = new Dictionary<uint, Position>(79);
                shadowSpriteOffsets = new Dictionary<uint, Position>(79);

                SpriteInfo spriteInfo;

                for (uint i = 0; i <= 118; ++i)
                {
                    spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, i);

                    if (spriteInfo != null)
                        spriteOffsets.Add(i, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));

                    spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, i);

                    if (spriteInfo != null)
                        shadowSpriteOffsets.Add(i, new Position(spriteInfo.OffsetX, spriteInfo.OffsetY));
                }
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            uint spriteIndex = (uint)objectType - 8;

            var spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);
            var shadowInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, spriteIndex);

            sprite = spriteFactory.Create(spriteInfo.Width, spriteInfo.Height, 0, 0, false, false);
            shadowSprite = spriteFactory.Create(shadowInfo.Width, shadowInfo.Height, 0, 0, false, false);
        }

        public void Update(uint tick, RenderMap map, uint position)
        {
            uint spriteIndex = (uint)objectType - 8;

            if (spriteIndex < 24) // tree
            {
                /* Adding sprite number to animation ensures
                   that the tree animation won't be synchronized
                   for all trees on the map. */
                uint treeAnim = (tick + spriteIndex) >> 4;

                if (spriteIndex < 16) // pine and normal tree (8 sprites each)
                {
                    spriteIndex = (uint)((spriteIndex & ~7) + (treeAnim & 7));
                }
                else // palm and water tree (4 sprites each)
                {
                    spriteIndex = (uint)((spriteIndex & ~3) + (treeAnim & 3));
                }
            }

            // the tree sprite sizes are the same per tree type, so no resize is necessary

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Objects);

            var renderPosition = map.CoordinateSpace.TileSpaceToViewSpace(position);

            sprite.X = renderPosition.X + spriteOffsets[spriteIndex].X;
            sprite.Y = renderPosition.Y + spriteOffsets[spriteIndex].Y;
            shadowSprite.X = renderPosition.X + shadowSpriteOffsets[spriteIndex].X;
            shadowSprite.Y = renderPosition.Y + shadowSpriteOffsets[spriteIndex].Y;

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(spriteIndex);
            shadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(1000u + spriteIndex);
        }

        // Note: this is not used for tree animations!
        // Only if objects types change (like logging a tree, digging stones and so on).
        public void ChangeObjectType(Map.Object objectType)
        {
            if (objectType == this.objectType)
                return; // nothing changed

            if (this.objectType == Map.Object.None) // from None to something valid
            {
                // this is handled by Game so this should not happen at all
                Debug.NotReached();
                return;
            }

            if (objectType < Map.Object.Tree0) // from something valid to None (or something we don't need to draw here)
            {
                // this is handled by Game so this should not happen at all
                Debug.NotReached();
                return;
            }

            this.objectType = objectType;

            uint spriteIndex = (uint)objectType - 8;

            var spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, spriteIndex);
            var shadowInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, spriteIndex);

            sprite.Resize(spriteInfo.Width, spriteInfo.Height);
            shadowSprite.Resize(shadowInfo.Width, shadowInfo.Height);
        }
    }
}
