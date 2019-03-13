/*
 * Icon.cs - Icon GUI components
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

namespace Freeserf.UI
{
    using Data = Data.Data;

    internal class Icon : GuiObject
    {
        Render.ILayerSprite sprite = null;
        byte displayLayerOffset = 0;

        public uint SpriteIndex { get; private set; } = 0u;
        public Data.Resource ResourceType { get; private set; } = Data.Resource.None;
        public object Tag { get; set; }

        // only used by BuildingIcon
        protected Icon(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf)
        {
            sprite = CreateSprite(interf.RenderView.SpriteFactory, width, height, Data.Resource.MapObject, spriteIndex, (byte)(BaseDisplayLayer + displayLayerOffset));
            this.displayLayerOffset = displayLayerOffset;
            ResourceType = Data.Resource.MapObject;
            sprite.Layer = interf.RenderView.GetLayer(Freeserf.Layer.GuiBuildings);
            SpriteIndex = spriteIndex;

            SetSize(width, height);
        }

        public Icon(Interface interf, int width, int height, Data.Resource resourceType, uint spriteIndex, byte displayLayerOffset)
            : base(interf)
        {
            sprite = CreateSprite(interf.RenderView.SpriteFactory, width, height, resourceType, spriteIndex, (byte)(BaseDisplayLayer + displayLayerOffset));
            this.displayLayerOffset = displayLayerOffset;
            this.ResourceType = resourceType;
            sprite.Layer = Layer;
            SpriteIndex = spriteIndex;

            SetSize(width, height);
        }

        protected override void InternalDraw()
        {
            sprite.X = TotalX;
            sprite.Y = TotalY;

            sprite.Visible = Displayed;
        }

        protected internal override void UpdateParent()
        {
            sprite.DisplayLayer = (byte)(BaseDisplayLayer + displayLayerOffset);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            sprite.Visible = false;
        }

        public void SetDisplayLayerOffset(byte offset)
        {
            if (displayLayerOffset == offset)
                return;

            displayLayerOffset = offset;

            sprite.DisplayLayer = (byte)Misc.Min(255, BaseDisplayLayer + displayLayerOffset);
        }

        public void SetResourceType(Data.Resource type)
        {
            ResourceType = type;
            sprite.TextureAtlasOffset = GetTextureAtlasOffset(ResourceType, SpriteIndex);
        }

        public void SetSpriteIndex(uint spriteIndex)
        {
            sprite.TextureAtlasOffset = GetTextureAtlasOffset(ResourceType, spriteIndex);
            SpriteIndex = spriteIndex;
        }

        public void SetSpriteIndex(Data.Resource type, uint spriteIndex)
        {
            ResourceType = type;
            SetSpriteIndex(spriteIndex);
        }

        public void Resize(int width, int height)
        {
            sprite.Resize(width, height);
            SetSize(width, height);
        }
    }

    internal class BuildingIcon : Icon
    {
        public BuildingIcon(Interface interf, int width, int height, uint spriteIndex, byte displayLayerOffset)
            : base(interf, width, height, spriteIndex, displayLayerOffset)
        {

        }
    }
}