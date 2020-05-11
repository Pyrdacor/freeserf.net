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

using Freeserf.Data;
using System;

namespace Freeserf.Render
{
    using Data = Data.Data;
    using MapPos = UInt32;

    internal class RenderFlag : RenderObject
    {
        readonly Flag flag = null;
        // sprite index 128-143 (16 sprites)
        static Position[] spriteOffsets = null;
        static Position[] shadowSpriteOffsets = null;
        readonly ISprite[] resources = new ISprite[8];
        readonly ISpriteFactory spriteFactory = null;
        static Rect[] resourceSpriteInfos = null;
        int maxBaseLine = 0;

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
            this.spriteFactory = spriteFactory;

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
                    var spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapObject, 128u + (uint)i);

                    spriteOffsets[i] = new Position(spriteInfo.OffsetX, spriteInfo.OffsetY);

                    spriteInfo = dataSource.GetSpriteInfo(Data.Resource.MapShadow, 128u + (uint)i);

                    shadowSpriteOffsets[i] = new Position(spriteInfo.OffsetX, spriteInfo.OffsetY);
                }
            }

            if (resourceSpriteInfos == null)
            {
                resourceSpriteInfos = new Rect[26];

                for (uint i = 0; i <= 25u; ++i)
                {
                    var spriteInfo = dataSource.GetSpriteInfo(Data.Resource.GameObject, i);

                    if (spriteInfo != null)
                        resourceSpriteInfos[i] = new Rect(spriteInfo.OffsetX, spriteInfo.OffsetY, spriteInfo.Width, spriteInfo.Height);
                }
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            // max sprite size is 16x19 pixels but we use 16x20 for better base line matching
            sprite = spriteFactory.Create(16, 20, 0, 0, false, false);
            shadowSprite = spriteFactory.Create(16, 20, 0, 0, false, false);
        }

        public override void Delete()
        {
            base.Delete();

            for (int i = 0; i < 8; ++i)
            {
                resources[i]?.Delete();
                resources[i] = null;
            }
        }

        public void Update(uint tick, RenderMap map, MapPos position)
        {
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Objects);
            uint offset = (tick >> 3) & 3;
            uint spriteIndex = 128u + offset;

            uint flagSpriteIndex = spriteIndex + flag.Player * 16u; // player colors

            var renderPosition = map.CoordinateSpace.TileSpaceToViewSpace(position);

            sprite.X = renderPosition.X + spriteOffsets[(int)offset].X;
            sprite.Y = renderPosition.Y + spriteOffsets[(int)offset].Y;
            shadowSprite.X = renderPosition.X + shadowSpriteOffsets[(int)offset].X;
            shadowSprite.Y = renderPosition.Y + shadowSpriteOffsets[(int)offset].Y;

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(flagSpriteIndex);
            shadowSprite.TextureAtlasOffset = textureAtlas.GetOffset(1000u + spriteIndex);

            int baseLine = sprite.Y + sprite.Height;

            if (baseLine > maxBaseLine)
                maxBaseLine = baseLine;
            else if (baseLine < maxBaseLine)
                sprite.BaseLineOffset = maxBaseLine - baseLine;

            // resources
            for (int i = 0; i < 8; ++i)
            {
                var resource = flag.GetResourceAtSlot(i);

                if (resource != Resource.Type.None)
                {
                    int baselineOffset = (i < 3) ? -2 : 2;
                    var info = resourceSpriteInfos[(int)resource];

                    if (resources[i] == null)
                    {
                        var spriteOffset = textureAtlas.GetOffset(2000u + (uint)resource);
                        resources[i] = spriteFactory.Create(info.Size.Width, info.Size.Height, spriteOffset.X, spriteOffset.Y, false, false);
                    }

                    // resource at slot 2 may be hidden by castle/stock so adjust baseline in this case
                    if (i == 2 && flag.HasBuilding)
                    {
                        switch (flag.Building.BuildingType)
                        {
                            case Building.Type.Castle:
                            case Building.Type.Stock:
                                baselineOffset += 4;
                                break;
                        }
                    }

                    resources[i].X = renderPosition.X + info.Position.X + ResPos[i * 2];
                    resources[i].Y = renderPosition.Y + info.Position.Y + ResPos[i * 2 + 1];
                    resources[i].Layer = sprite.Layer;
                    resources[i].BaseLineOffset = baselineOffset;
                    resources[i].Visible = true;
                }
                else
                {
                    resources[i]?.Delete();
                    resources[i] = null;
                }
            }
        }
    }
}
