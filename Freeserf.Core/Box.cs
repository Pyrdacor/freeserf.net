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
        readonly SpriteDefinition definition;
        readonly Render.ILayerSprite background = null;

        static readonly SpriteDefinition[] definitions = new SpriteDefinition[]
        {
            new SpriteDefinition(318u, 320, 184), // used for GameInitBox
            new SpriteDefinition(319u, 128, 144)  // used for NotificationBox
        };

        protected BackgroundPattern()
        {

        }

        BackgroundPattern(Render.ISpriteFactory spriteFactory, int type)
        {
            definition = definitions[type];

            var offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, definition.SpriteIndex);

            background = spriteFactory.Create(definition.SpriteWidth, definition.SpriteHeight, offset.X, offset.Y, false, true) as Render.ILayerSprite;
        }

        BackgroundPattern(Render.ISpriteFactory spriteFactory, int type, uint spriteIndex)
        {
            definition = definitions[type];

            var offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, spriteIndex);

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

        public static BackgroundPattern CreatePopupBoxBackground(Render.ISpriteFactory spriteFactory, uint index)
        {
            return new BackgroundPattern(spriteFactory, 1, index);
        }

        public static BackgroundPattern CreateResourceStatisticPopupBoxBackground(Render.ISpriteFactory spriteFactory, uint index)
        {
            return new ResourceStatisticBackgroundPattern(spriteFactory, index);
        }

        public static BackgroundPattern CreatePlayerStatisticPopupBoxBackground(Render.ISpriteFactory spriteFactory, uint index)
        {
            return new PlayerStatisticBackgroundPattern(spriteFactory, index);
        }

        public virtual void Draw(GuiObject parent)
        {
            if (parent == null)
            {
                background.Visible = false;
                return;
            }

            Resize(parent.Width, parent.Height);

            background.X = parent.TotalX + Offset.X + (parent.Width - background.Width) / 2;
            background.Y = parent.TotalY + Offset.Y + (parent.Height - background.Height) / 2;
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

        public Position Offset
        {
            get;
            set;
        } = new Position();

        public virtual bool Visible
        {
            get => background.Visible;
            set => background.Visible = value;
        }

        class ResourceStatisticBackgroundPattern : BackgroundPattern
        {
            readonly Render.ILayerSprite[] iconBackground = new Render.ILayerSprite[4 * 7]; // 4 rows, 7 columns
            readonly new Render.ILayerSprite[] background = new Render.ILayerSprite[3 * 8];
            readonly Render.ILayerSprite[] backgroundPieces = new Render.ILayerSprite[4];
            bool visible = false;

            internal ResourceStatisticBackgroundPattern(Render.ISpriteFactory spriteFactory, uint spriteIndex)
            {
                Position offset;

                for (int i = 0; i < 4 * 7; ++i)
                {
                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, spriteIndex);
                    iconBackground[i] = spriteFactory.Create(16, 16, offset.X, offset.Y, false, true) as Render.ILayerSprite;
                }

                for (int i = 0; i < 3 * 8; ++i)
                {
                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 129u);
                    background[i] = spriteFactory.Create(16, 16, offset.X, offset.Y, false, true) as Render.ILayerSprite;
                }

                for (int i = 0; i < 4; ++i)
                {
                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 129u);
                    backgroundPieces[i] = spriteFactory.Create(16, 16, offset.X, offset.Y, false, true) as Render.ILayerSprite;
                }
            }

            public override void Draw(GuiObject parent)
            {
                if (parent == null)
                {
                    Action<Render.ILayerSprite, int, int> action = (Render.ILayerSprite sprite, int index, int param) => sprite.Visible = false;

                    BackgroundAction(iconBackground, action);
                    BackgroundAction(background, action);
                    BackgroundAction(backgroundPieces, action);

                    visible = false;

                    return;
                }

                Action<Render.ILayerSprite, int, int> spriteAction = (Render.ILayerSprite sprite, int index, int param) =>
                {
                    if (param == 2)
                    {
                        sprite.X = parent.TotalX + Offset.X + 8 + (3 + index % 2) * 16;
                        sprite.Y = parent.TotalY + Offset.Y + 8 + 80 + (index / 2) * 16;
                    }
                    else
                    {
                        int yOffset = 8 + param * 64;
                        int columns = 7 + param;

                        if (param == 0)
                        {
                            yOffset += (index / columns) * 16;
                        }
                        else
                        {
                            int row = (index / columns);

                            if (row == 1)
                                yOffset += 48;
                            else if (row == 2)
                                yOffset += 64;
                        }

                        sprite.X = parent.TotalX + Offset.X + 8 + (index % columns) * 16;
                        sprite.Y = parent.TotalY + Offset.Y + yOffset;
                    }

                    sprite.DisplayLayer = parent.BaseDisplayLayer;
                    sprite.Layer = parent.Layer;
                    sprite.Visible = visible && parent.Displayed && sprite.X < parent.TotalX + parent.Width && sprite.Y < parent.TotalY + parent.Height;
                };

                BackgroundAction(iconBackground, spriteAction, 0);
                BackgroundAction(background, spriteAction, 1);
                BackgroundAction(backgroundPieces, spriteAction, 2);
            }

            void BackgroundAction(Render.ILayerSprite[] backgrounds, Action<Render.ILayerSprite, int, int> action, int param = 0)
            {
                int index = 0;

                foreach (var background in backgrounds)
                    action?.Invoke(background, index++, param);
            }

            public override bool Visible
            {
                get => visible;
                set
                {
                    if (visible == value)
                        return;

                    visible = value;

                    Action<Render.ILayerSprite, int, int> action = (Render.ILayerSprite sprite, int index, int param) => sprite.Visible = visible;

                    BackgroundAction(iconBackground, action);
                    BackgroundAction(background, action);
                    BackgroundAction(backgroundPieces, action);
                }
            }
        }

        // this is used for player statistic popups
        // icons are 16x16
        // lower background parts are 16x8, 16x16 and 16x12
        // as the width is 128 we have 8 icons per row
        // the height will be 148 instead of 144 (we use 7 icon rows)
        class PlayerStatisticBackgroundPattern : BackgroundPattern
        {
            readonly Render.ILayerSprite[] iconBackground = new Render.ILayerSprite[7 * 8]; // 7 rows, 8 columns
            readonly Render.ILayerSprite[] background1 = new Render.ILayerSprite[8];
            readonly Render.ILayerSprite[] background2 = new Render.ILayerSprite[8];
            readonly Render.ILayerSprite[] background3 = new Render.ILayerSprite[8];
            bool visible = false;

            internal PlayerStatisticBackgroundPattern(Render.ISpriteFactory spriteFactory, uint spriteIndex)
            {
                Position offset;

                for (int i = 0; i < 7 * 8; ++i)
                {
                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, spriteIndex);
                    iconBackground[i] = spriteFactory.Create(16, 16, offset.X, offset.Y, false, true) as Render.ILayerSprite;
                }

                for (int i = 0; i < 8; ++i)
                {
                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 136u);
                    background1[i] = spriteFactory.Create(16, 8, offset.X, offset.Y, false, true) as Render.ILayerSprite;

                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 129u);
                    background2[i] = spriteFactory.Create(16, 16, offset.X, offset.Y, false, true) as Render.ILayerSprite;

                    offset = GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 137u);
                    background3[i] = spriteFactory.Create(16, 12, offset.X, offset.Y, false, true) as Render.ILayerSprite;
                }
            }

            public override void Draw(GuiObject parent)
            {
                if (parent == null)
                {
                    Action<Render.ILayerSprite, int, int> action = (Render.ILayerSprite sprite, int index, int param) => sprite.Visible = false;

                    BackgroundAction(iconBackground, action);
                    BackgroundAction(background1, action);
                    BackgroundAction(background2, action);
                    BackgroundAction(background3, action);

                    visible = false;

                    return;
                }

                Action<Render.ILayerSprite, int, int> spriteAction = (Render.ILayerSprite sprite, int index, int param) =>
                {
                    int yOffset = 0;

                    if (param == 1)
                        yOffset = 108;
                    else if (param == 2)
                        yOffset = 116;
                    else if (param == 3)
                        yOffset = 132;

                    sprite.X = parent.TotalX + Offset.X + (parent.Width - 128) / 2 + (index % 8) * 16;
                    sprite.Y = parent.TotalY + Offset.Y + 8 + yOffset + (index / 8) * 16;
                    sprite.DisplayLayer = parent.BaseDisplayLayer;
                    sprite.Layer = parent.Layer;
                    sprite.Visible = visible && parent.Displayed && sprite.X < parent.TotalX + parent.Width && sprite.Y < parent.TotalY + parent.Height;
                };

                BackgroundAction(iconBackground, spriteAction, 0);
                BackgroundAction(background1, spriteAction, 1);
                BackgroundAction(background2, spriteAction, 2);
                BackgroundAction(background3, spriteAction, 3);
            }

            void BackgroundAction(Render.ILayerSprite[] backgrounds, Action<Render.ILayerSprite, int, int> action, int param = 0)
            {
                int index = 0;

                foreach (var background in backgrounds)
                    action?.Invoke(background, index++, param);
            }

            public override bool Visible
            {
                get => visible;
                set
                {
                    if (visible == value)
                        return;

                    visible = value;

                    Action<Render.ILayerSprite, int, int> action = (Render.ILayerSprite sprite, int index, int param) => sprite.Visible = visible;

                    BackgroundAction(iconBackground, action);
                    BackgroundAction(background1, action);
                    BackgroundAction(background2, action);
                    BackgroundAction(background3, action);
                }
            }
        }
    }

    internal class Border
    {
        readonly SpriteDefinition[] definition = new SpriteDefinition[4];
        readonly Render.ILayerSprite[] borders = new Render.ILayerSprite[4];
        readonly bool horizontalBordersInside = false;

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
        };

        public Position GetBackgroundOffset()
        {
            return new Position((borders[2].Width - borders[1].Width) / 2, (borders[0].Height - borders[3].Height) / 2);
        }

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

        public static Border CreatePopupBoxBorder(Render.ISpriteFactory spriteFactory)
        {
            return new Border(spriteFactory, Data.Resource.FramePopup, 1, false);
        }

        public void Draw(GuiObject parent)
        {
            if (parent == null)
                return;

            byte displayLayer = (byte)(parent.BaseDisplayLayer + 9);

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
        readonly Interface interf = null;
        BackgroundPattern background = null;
        Border border = null;

        protected Box(Interface interf, BackgroundPattern background, Border border)
            : base(interf)
        {
            this.interf = interf;
            this.border = border;

            SetBackground(background);
        }

        public void SetBackground(BackgroundPattern background)
        {
            if (this.background == background)
                return;

            if (this.background != null)
            {
                this.background.Visible = false;
            }

            this.background = background;

            if (this.background != null)
            {
                this.background.Visible = true;
                this.background.Offset = border.GetBackgroundOffset();
            }
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
