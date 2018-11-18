using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Freeserf.Render
{
    internal class TextRenderer
    {
        class SpriteInfo
        {
            public ILayerSprite Sprite;
            public bool InUse;
        }

        class RenderText
        {
            public List<SpriteInfo> Characters
            {
                get;
                set;
            } = null;

            public string Text
            {
                get;
                set;
            } = "";

            public bool Visible
            {
                get;
                set;
            } = false;

            public Position Position
            {
                get;
            } = new Position();

            public RenderText(string text)
            {
                Text = text;
            }

            public void UpdatePositions(int charGapSize)
            {
                int x = 0;

                foreach (var character in Characters)
                {
                    character.Sprite.X = Position.X + x;
                    character.Sprite.Y = Position.Y;

                    x += charGapSize;
                }
            }

            public void UpdateDisplayLayer(byte displayLayer)
            {
                foreach (var character in Characters)
                {
                    character.Sprite.DisplayLayer = displayLayer;
                }
            }
        }

        readonly IRenderLayer layer = null;
        readonly ISpriteFactory spriteFactory = null;
        readonly Size characterSize = null;
        readonly int characterGapSize = 0;
        readonly List<RenderText> renderTexts = new List<RenderText>();
        readonly List<SpriteInfo> characterSprites = new List<SpriteInfo>(); // shared by all texts
        readonly ITextureAtlas textureAtlas = null;
        static readonly Encoding encoding = Encoding.GetEncoding("iso-8859-1");

        public TextRenderer(IRenderView renderView)
        {
            spriteFactory = renderView.SpriteFactory;
            layer = renderView.GetLayer(Layer.Gui);

            // original size is 8x8 pixels
            characterSize = new Size(8, 8);
            characterGapSize = 9; // distance betwee starts of characters in x direction

            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Gui);
        }

        public int CreateText(string text, byte displayLayer, Position position = null)
        {
            var spritePool = characterSprites.Where(s => !s.InUse);

            int numAvailableChars = spritePool.Count();

            List<SpriteInfo> sprites;

            if (numAvailableChars < text.Length)
            {
                sprites = new List<SpriteInfo>(spritePool);
                sprites.AddRange(AddCharSprites(text.Length - numAvailableChars));
            }
            else
            {
                sprites = new List<SpriteInfo>(spritePool.Take(text.Length));
            }

            // mark all as in use
            foreach (var sprite in sprites)
                sprite.InUse = true;

            var renderText = new RenderText(text);

            renderText.Characters = sprites;

            if (position != null)
            {
                renderText.Position.X = position.X;
                renderText.Position.Y = position.Y;

                renderText.UpdatePositions(characterGapSize);
            }

            renderText.UpdateDisplayLayer(displayLayer);

            renderTexts.Add(renderText);

            return renderTexts.Count - 1;
        }

        public void ShowText(int index, bool show)
        {
            var renderText = renderTexts[index];

            if (renderText.Visible == show)
                return; // nothing to do

            foreach (var character in renderText.Characters)
                character.Sprite.Visible = show;

            renderText.Visible = show;
        }

        public void ChangeText(int index, string newText)
        {
            var renderText = renderTexts[index];

            if (renderText.Text == newText)
                return;

            if (renderText.Text.Length < newText.Length)
            {
                var spritePool = characterSprites.Where(s => !s.InUse);

                int numAdditionalChars = newText.Length - renderText.Text.Length;
                int numAvailableChars = spritePool.Count();

                List<SpriteInfo> sprites;

                if (numAvailableChars < numAdditionalChars)
                {
                    sprites = new List<SpriteInfo>(spritePool);
                    sprites.AddRange(AddCharSprites(numAdditionalChars - numAvailableChars));
                }
                else
                {
                    sprites = new List<SpriteInfo>(spritePool.Take(numAdditionalChars));
                }

                // mark all as in use
                foreach (var sprite in sprites)
                    sprite.InUse = true;

                renderText.Characters.AddRange(sprites);
            }
            else if (renderText.Text.Length > newText.Length)
            {
                for (int i = newText.Length; i < renderText.Text.Length; ++i)
                {
                    renderText.Characters[i].Sprite.Visible = false;
                    renderText.Characters[i].InUse = false;
                }

                renderText.Characters.RemoveRange(newText.Length, renderText.Text.Length - newText.Length);
            }

            SetTextToSprites(renderText.Characters, newText);

            renderText.Text = newText;
        }

        public void DestroyText(int index)
        {
            var renderText = renderTexts[index];

            foreach (var character in renderText.Characters)
            {
                character.Sprite.Visible = false;
                character.InUse = false;
            }

            renderTexts.RemoveAt(index);
        }

        public void ChangeDisplayLayer(int index, byte displayLayer)
        {
            var renderText = renderTexts[index];

            foreach (var character in renderText.Characters)
                character.Sprite.DisplayLayer = displayLayer;
        }

        public void SetPosition(int index, Position position)
        {
            var renderText = renderTexts[index];

            if (position == renderText.Position)
                return;

            renderText.Position.X = position.X;
            renderText.Position.Y = position.Y;

            renderText.UpdatePositions(characterGapSize);
        }

        public bool IsVisible(int index)
        {
            return renderTexts[index].Visible;
        }

        uint MapCharToSpriteIndex(byte ch)
        {
            if (ch >= 'A' && ch <= 'Z')
                return (uint)(ch - 'A');
            if (ch >= 'a' && ch <= 'z')
                return (uint)(ch - 'a');
            if (ch == 0xC4 || ch == 0xE4) // 'Ä' or 'ä'
                return 26u;
            if (ch == 0xD6 || ch == 0xF6) // 'Ö' or 'ö'
                return 27u;
            if (ch == 0xDC || ch == 0xFC) // 'Ü' or 'ü'
                return 28u;
            if (ch >= '0' && ch <= '9')
                return 29u + (uint)(ch - '0');
            if (ch == '.')
                return 39u;
            if (ch == '-')
                return 40u;
            if (ch == ':')
                return 41u;
            if (ch == '?')
                return 42u;
            if (ch == '%')
                return 43u;

            throw new ExceptionFreeserf("Unsupported character: " + encoding.GetString(new byte[1] { ch }));
        }

        void SetTextToSprites(List<SpriteInfo> sprites, string text)
        {
            var bytes = encoding.GetBytes(text);

            // the length of sprites is the same than the text length
            for (int i = 0; i < bytes.Length; ++i)
            {
                sprites[i].Sprite.TextureAtlasOffset = textureAtlas.GetOffset(MapCharToSpriteIndex(bytes[i]));
            }
        }

        List<SpriteInfo> AddCharSprites(int num)
        {
            List<SpriteInfo> sprites = new List<SpriteInfo>(num);

            for (int i = 0; i < num; ++i)
            {
                var spriteInfo = new SpriteInfo()
                {
                    Sprite = spriteFactory.Create(characterSize.Width, characterSize.Height, 0, 0, false, true, 255) as Render.ILayerSprite,
                    InUse = false
                };

                spriteInfo.Sprite.Layer = layer;

                sprites.Add(spriteInfo);
            }

            characterSprites.AddRange(sprites);

            return sprites;
        }
    }

    internal class TextField
    {
        readonly TextRenderer textRenderer;
        int index = -1;
        string text = "";
        byte displayLayer = 0;

        public int X { get; private set; } = 0;
        public int Y { get; private set; } = 0;

        public byte DisplayLayer
        {
            get => displayLayer;
            set
            {
                if (displayLayer == value)
                    return;

                displayLayer = value;

                if (index != -1)
                    textRenderer.ChangeDisplayLayer(index, displayLayer);
            }
        }

        public TextField(TextRenderer textRenderer)
        {
            this.textRenderer = textRenderer;
        }

        public void Destroy()
        {
            if (index != -1)
                textRenderer.DestroyText(index);

            text = "";
            index = -1;
        }

        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                if (index == -1)
                    index = textRenderer.CreateText(text, DisplayLayer, new Position(X, Y));
                else
                    textRenderer.ChangeText(index, text);
            }
        }

        public bool Visible
        {
            get
            {
                if (index == -1)
                    return false;

                return textRenderer.IsVisible(index);
            }
            set
            {
                if (index == -1 && !value)
                    return;

                if (index == -1 && value)
                    index = textRenderer.CreateText(text, DisplayLayer, new Position(X, Y));

                textRenderer.ShowText(index, value);
            }
        }

        public void SetPosition(int x, int y)
        {
            if (index != -1)
                textRenderer.SetPosition(index, new Position(x, y));

            X = x;
            Y = y;
        }
    }
}
