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

namespace Freeserf.Render
{
    internal class RenderMapObject : RenderObject
    {
        Map.Object objectType = Map.Object.None;

        public RenderMapObject(Map.Object objectType, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.objectType = objectType;

            Initialize();
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            // TODO
            // sprite = spriteFactory.Create(...);
        }

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
                Delete();
                return;
            }

            // TODO: set tex coords and size
        }
    }
}
