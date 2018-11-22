using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf
{
    // See TextureAtlasManager.AddAll for details.
    // Backgrounds are created by creating a new larger
    // sprite and add some smaller sprites to it.

    struct SpriteDefinition
    {
        public SpriteDefinition(uint spriteIndex, int width, int height)
        {
            SpriteIndex = spriteIndex;
            SpriteWidth = width;
            SpriteHeight = height;
        }

        public uint SpriteIndex;
        public int SpriteWidth;
        public int SpriteHeight;
    }

    internal class BackgroundPattern
    {
        SpriteDefinition definition;
        readonly Render.ILayerSprite background = null;

        static readonly SpriteDefinition[] definitions = new SpriteDefinition[]
        {
            new SpriteDefinition(318u, 320, 184), // used for GameInitBox
            new SpriteDefinition(319u, 128, 144)  // used for NotificationBox
            // TODO ...
        };

        BackgroundPattern(Render.ISpriteFactory spriteFactory, int type)
        {
            definition = definitions[type];

            var offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, definition.SpriteIndex);

            background = spriteFactory.Create(definition.SpriteWidth, definition.SpriteHeight, offset.X, offset.Y, false, true) as Render.ILayerSprite;
        }

        public static BackgroundPattern CreateGameInitBoxBackground(Render.ISpriteFactory spriteFactory)
        {
            return new BackgroundPattern(spriteFactory, 0);
        }

        public static BackgroundPattern CreateNotificationBoxBackground(Render.ISpriteFactory spriteFactory)
        {
            return new BackgroundPattern(spriteFactory, 1);
        }

        // TODO ...

        public void Draw(GuiObject parent)
        {
            if (parent == null)
            {
                background.Visible = false;
                return;
            }

            Resize(parent.Width, parent.Height);

            background.X = parent.TotalX + (parent.Width - background.Width) / 2;
            background.Y = parent.TotalY + (parent.Height - background.Height) / 2;
            background.DisplayLayer = parent.BaseDisplayLayer;
            background.Layer = parent.Layer;
            background.Visible = parent.Displayed;
        }

        void Resize(int width, int height)
        {
            width = Math.Min(width, definition.SpriteWidth);
            height = Math.Min(height, definition.SpriteHeight);

            background.Resize(width, height);
        }

        public bool Visible
        {
            get => background.Visible;
            set => background.Visible = value;
        }
    }

    internal class Border
    {
        SpriteDefinition[] definition = new SpriteDefinition[4];
        Render.ILayerSprite[] borders = new Render.ILayerSprite[4];
        bool horizontalBordersInside = false;

        static readonly SpriteDefinition[,] definitions = new SpriteDefinition[,]
        {
            { // used for GameInitBox
                new SpriteDefinition(2u, 320,   8),
                new SpriteDefinition(0u,  16, 200),
                new SpriteDefinition(1u,  16, 200),
                new SpriteDefinition(2u, 320,   8),
            },
            { // used for NotificationBox, PopupBox
                new SpriteDefinition(0u, 144,   9),
                new SpriteDefinition(2u,   8, 144),
                new SpriteDefinition(3u,   8, 144),
                new SpriteDefinition(1u, 144,   7),
            }
            // TODO ...
        };

        Border(Render.ISpriteFactory spriteFactory, Data.Resource resourceType, int type, bool horizontalBordersInside)
        {
            this.horizontalBordersInside = horizontalBordersInside;
            definition = definitions.SliceRow(type).ToArray();

            for (int i = 0; i < 4; ++i)
            {
                var offset = GuiObject.GetTextureAtlasOffset(resourceType, definition[i].SpriteIndex);

                borders[i] = spriteFactory.Create(definition[i].SpriteWidth, definition[i].SpriteHeight, offset.X, offset.Y, false, true) as Render.ILayerSprite;
            }
        }

        public static Border CreateGameInitBoxBorder(Render.ISpriteFactory spriteFactory)
        {
            return new Border(spriteFactory, Data.Resource.FrameTop, 0, true);
        }

        public static Border CreateNotificationBoxBorder(Render.ISpriteFactory spriteFactory)
        {
            return new Border(spriteFactory, Data.Resource.FramePopup, 1, false);
        }

        // TODO ...

        public void Draw(GuiObject parent)
        {
            if (parent == null)
                return;

            byte displayLayer = (byte)(parent.BaseDisplayLayer + 1);

            int horizontalXOffset = (horizontalBordersInside) ? definition[1].SpriteWidth : 0;
            int verticalYOffset = (horizontalBordersInside) ? 0 : definition[0].SpriteHeight;

            // top border
            borders[0].X = parent.TotalX + horizontalXOffset;
            borders[0].Y = parent.TotalY;
            borders[0].DisplayLayer = displayLayer;
            borders[0].Layer = parent.Layer;
            borders[0].Visible = parent.Displayed;

            // left border
            borders[1].X = parent.TotalX;
            borders[1].Y = parent.TotalY + verticalYOffset;
            borders[1].DisplayLayer = displayLayer;
            borders[1].Layer = parent.Layer;
            borders[1].Visible = parent.Displayed;

            // right border
            borders[2].X = parent.TotalX + parent.Width - definition[2].SpriteWidth;
            borders[2].Y = parent.TotalY + verticalYOffset;
            borders[2].DisplayLayer = displayLayer;
            borders[2].Layer = parent.Layer;
            borders[2].Visible = parent.Displayed;

            // bottom border
            borders[3].X = parent.TotalX + horizontalXOffset;
            borders[3].Y = parent.TotalY + parent.Height - definition[3].SpriteHeight;
            borders[3].DisplayLayer = displayLayer;
            borders[3].Layer = parent.Layer;
            borders[3].Visible = parent.Displayed;
        }

        public bool Visible
        {
            get => borders[0].Visible;
            set
            {
                borders[0].Visible = value;
                borders[1].Visible = value;
                borders[2].Visible = value;
                borders[3].Visible = value;
            }
        }
    }

    internal abstract class Box : GuiObject
    {
        Interface interf = null;
        BackgroundPattern background = null;
        Border border = null;

        protected Box(Interface interf, BackgroundPattern background, Border border)
            : base(interf)
        {
            this.interf = interf;
            this.background = background;
            this.border = border;
        }

        protected override void InternalDraw()
        {
            background?.Draw(this);
            border?.Draw(this);
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            if (background != null)
                background.Visible = false;

            if (border != null)
                border.Visible = false;
        }
    }
}
