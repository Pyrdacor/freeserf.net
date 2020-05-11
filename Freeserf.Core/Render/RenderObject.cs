/*
 * RenderObject.cs - Base class for game object rendering
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
    internal abstract class RenderObject
    {
        IRenderLayer renderLayer = null;
        ISpriteFactory spriteFactory = null;
        DataSource dataSource = null;
        protected ISprite sprite = null;
        protected ISprite shadowSprite = null;

        public virtual bool Visible
        {
            get => sprite.Visible;
            set
            {
                sprite.Visible = value;

                if (shadowSprite != null)
                    shadowSprite.Visible = value;
            }
        }

        protected RenderObject(IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
        {
            this.renderLayer = renderLayer;
            this.spriteFactory = spriteFactory;
            this.dataSource = dataSource;
        }

        protected void Initialize()
        {
            Create(spriteFactory, dataSource);

            if (sprite == null)
                throw new ExceptionFreeserf(ErrorSystemType.Render, "Failed to create sprite");

            sprite.Layer = renderLayer;

            if (shadowSprite != null)
                shadowSprite.Layer = renderLayer;
        }

        protected abstract void Create(ISpriteFactory spriteFactory, DataSource dataSource);

        public virtual void Delete()
        {
            sprite?.Delete();
            sprite = null;

            shadowSprite?.Delete();
            shadowSprite = null;
        }

        public bool IsVisibleIn(Rect rect)
        {
            if (shadowSprite == null)
            {
                return rect.IntersectsWith(new Rect(sprite.X, sprite.Y, sprite.Width, sprite.Height));
            }
            else
            {
                int left = Math.Min(sprite.X, shadowSprite.X);
                int top = Math.Min(sprite.Y, shadowSprite.Y);
                int right = Math.Max(sprite.X + sprite.Width, shadowSprite.X + shadowSprite.Width);
                int bottom = Math.Max(sprite.Y + sprite.Height, shadowSprite.Y + shadowSprite.Height);

                return rect.IntersectsWith(new Rect(left, top, right - left, bottom - top));
            }
        }
    }
}
