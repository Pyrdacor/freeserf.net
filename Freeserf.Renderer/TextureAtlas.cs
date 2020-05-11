/*
 * TextureAtlas.cs - Texture atlas creating and handling
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.Renderer
{
    public class TextureAtlas : ITextureAtlas
    {
        readonly Dictionary<uint, Position> textureOffsets = new Dictionary<uint, Position>();

        public Render.Texture Texture
        {
            get;
        }

        internal TextureAtlas(Texture texture, Dictionary<uint, Position> textureOffsets)
        {
            Texture = texture;
            this.textureOffsets = textureOffsets;
        }

        public Position GetOffset(uint spriteIndex)
        {
            return new Position(textureOffsets[spriteIndex]);
        }
    }

    public class TextureAtlasBuilder : ITextureAtlasBuilder
    {
        readonly Dictionary<uint, Data.Sprite> sprites = new Dictionary<uint, Data.Sprite>();

        // key = max height of category
        class SpriteCategorySorter : IComparer<KeyValuePair<uint, List<uint>>>
        {
            public int Compare(KeyValuePair<uint, List<uint>> x, KeyValuePair<uint, List<uint>> y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }

        public void AddSprite(uint spriteIndex, Data.Sprite sprite)
        {
            sprites.Add(spriteIndex, sprite);
        }

        // it is not the best sprite packing algorithm but it will do its job
        // TODO: improve later
        public ITextureAtlas Create()
        {
            // sort sprites by similar heights (16-pixel bands)
            // heights of items are < key * 16
            // value = list of sprite indices
            Dictionary<uint, List<uint>> spriteCategories = new Dictionary<uint, List<uint>>();
            Dictionary<uint, uint> spriteCategoryMinValues = new Dictionary<uint, uint>();
            Dictionary<uint, uint> spriteCategoryMaxValues = new Dictionary<uint, uint>();
            Dictionary<uint, uint> spriteCategoryTotalWidth = new Dictionary<uint, uint>();

            foreach (var sprite in sprites)
            {
                uint category = sprite.Value.Height / 16;

                if (!spriteCategories.ContainsKey(category))
                {
                    spriteCategories.Add(category, new List<uint>());
                    spriteCategoryMinValues.Add(category, sprite.Value.Height);
                    spriteCategoryMaxValues.Add(category, sprite.Value.Height);
                    spriteCategoryTotalWidth.Add(category, sprite.Value.Width);
                }
                else
                {
                    if (sprite.Value.Height < spriteCategoryMinValues[category])
                        spriteCategoryMinValues[category] = sprite.Value.Height;
                    if (sprite.Value.Height > spriteCategoryMaxValues[category])
                        spriteCategoryMaxValues[category] = sprite.Value.Height;
                    spriteCategoryTotalWidth[category] += sprite.Value.Width;
                }

                spriteCategories[category].Add(sprite.Key);
            }

            var filteredSpriteCategories = new List<KeyValuePair<uint, List<uint>>>();

            foreach (var category in spriteCategories)
            {
                if (spriteCategories[category.Key].Count == 0)
                    continue; // was merged with lower category

                // merge categories with minimal differences
                if (spriteCategoryMinValues[category.Key] >= category.Key * 16 + 8 &&
                    spriteCategories.ContainsKey(category.Key + 1) &&
                    spriteCategoryMaxValues[category.Key + 1] <= (category.Key + 1) * 16 + 8)
                {
                    spriteCategories[category.Key].AddRange(spriteCategories[category.Key + 1]);
                    spriteCategoryMaxValues[category.Key] = Math.Max(spriteCategoryMaxValues[category.Key], spriteCategoryMaxValues[category.Key + 1]);
                    spriteCategories[category.Key + 1].Clear();
                }

                filteredSpriteCategories.Add(new KeyValuePair<uint, List<uint>>(spriteCategoryMaxValues[category.Key], spriteCategories[category.Key]));
            }

            filteredSpriteCategories.Sort(new SpriteCategorySorter());

            // now we have a sorted category list with all sprite indices

            uint maxWidth = Math.Max(512u, spriteCategoryMaxValues.Max(m => m.Value));
            uint width = 0u;
            uint height = 0u;
            uint xOffset = 0u;
            uint yOffset = 0u;
            Dictionary<uint, Position> textureOffsets = new Dictionary<uint, Position>();

            // create texture offsets
            foreach (var category in filteredSpriteCategories)
            {
                foreach (var spriteIndex in category.Value)
                {
                    var sprite = sprites[spriteIndex];

                    if (xOffset + sprite.Width <= maxWidth)
                    {
                        if (yOffset + sprite.Height > height)
                            height = yOffset + sprite.Height;

                        textureOffsets.Add(spriteIndex, new Position((int)xOffset, (int)yOffset));

                        xOffset += sprite.Width;

                        if (xOffset > width)
                            width = xOffset;
                    }
                    else
                    {
                        xOffset = 0;
                        yOffset = height;

                        height = yOffset + sprite.Height;

                        textureOffsets.Add(spriteIndex, new Position((int)xOffset, (int)yOffset));

                        xOffset += sprite.Width;

                        if (xOffset > width)
                            width = xOffset;
                    }
                }

                if (xOffset > maxWidth - 360) // we do not expect sprites with a width greater than 360
                {
                    xOffset = 0;
                    yOffset = height;
                }
            }

            // create texture
            MutableTexture texture = new MutableTexture((int)width, (int)height);

            foreach (var offset in textureOffsets)
            {
                var sprite = sprites[offset.Key];

                texture.AddSprite(offset.Value, sprite.GetData(), (int)sprite.Width, (int)sprite.Height);
            }

            texture.Finish(0);

            return new TextureAtlas(texture, textureOffsets);
        }
    }

    public class TextureAtlasBuilderFactory : ITextureAtlasBuilderFactory
    {
        public ITextureAtlasBuilder Create()
        {
            return new TextureAtlasBuilder();
        }
    }
}
