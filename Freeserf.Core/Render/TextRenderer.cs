using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Freeserf.Render
{
    using Data = Data.Data;

    internal class TextRenderer
    {
        static readonly Encoding encoding = Encoding.GetEncoding("iso-8859-1");
        public static readonly byte[] ValidCharacters = encoding.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-:?%äÄöÖüÜ");

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

            public bool UseSpecialDigits
            {
                get;
                set;
            }

            public RenderText(string text)
            {
                Text = text;
            }

            public void UpdatePositions(int charGapSize)
            {
                int x = 0;
                int charIndex = 0;

                foreach (var character in Text)
                {
                    if (character != ' ')
                    {
                        Characters[charIndex].Sprite.X = Position.X + x;
                        Characters[charIndex].Sprite.Y = Position.Y;
                        ++charIndex;
                    }

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
        readonly List<RenderText> renderTexts = new List<RenderText>();
        readonly List<SpriteInfo> characterSprites = new List<SpriteInfo>(); // shared by all texts
        readonly ITextureAtlas textureAtlas = null;

        public TextRenderer(IRenderView renderView)
        {
            spriteFactory = renderView.SpriteFactory;
            layer = renderView.GetLayer(Layer.Gui);

            // original size is 8x8 pixels
            characterSize = new Size(8, 8);

            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Gui);
        }

        public int CreateText(string text, byte displayLayer, bool specialDigits, Position position = null, int characterGapSize = 8)
        {
            // look if we have a unused render text
            int index = renderTexts.FindIndex(r => r == null);

            var spritePool = characterSprites.Where(s => !s.InUse);

            int numAvailableChars = spritePool.Count();
            int lengthWithoutSpaces = text.Replace(" ", "").Length;

            List<SpriteInfo> sprites;

            if (numAvailableChars < lengthWithoutSpaces)
            {
                sprites = new List<SpriteInfo>(spritePool);
                sprites.AddRange(AddCharSprites(lengthWithoutSpaces - numAvailableChars));
            }
            else
            {
                sprites = new List<SpriteInfo>(spritePool.Take(lengthWithoutSpaces));
            }

            // mark all as in use
            foreach (var sprite in sprites)
                sprite.InUse = true;

            var renderText = new RenderText(text);

            renderText.UseSpecialDigits = specialDigits;
            renderText.Characters = sprites;

            if (position != null)
            {
                renderText.Position.X = position.X;
                renderText.Position.Y = position.Y;
            }

            SetTextToSprites(renderText.Characters, text, renderText.UseSpecialDigits);
            renderText.UpdatePositions(characterGapSize);
            renderText.UpdateDisplayLayer(displayLayer);

            if (index == -1)
            {
                index = renderTexts.Count;
                renderTexts.Add(renderText);
            }
            else
            {
                renderTexts[index] = renderText;
            }

            return index;
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

        public void ChangeText(int index, string newText, byte displayLayer, int characterGapSize = 8)
        {
            var renderText = renderTexts[index];

            if (renderText.Text == newText)
                return;

            int newLengthWithoutSpaces = newText.Replace(" ", "").Length;

            if (renderText.Characters.Count < newLengthWithoutSpaces)
            {
                var spritePool = characterSprites.Where(s => !s.InUse);

                int numAdditionalChars = newLengthWithoutSpaces - renderText.Characters.Count;
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
            else if (renderText.Characters.Count > newLengthWithoutSpaces)
            {
                for (int i = newLengthWithoutSpaces; i < renderText.Characters.Count; ++i)
                {
                    renderText.Characters[i].Sprite.Visible = false;
                    renderText.Characters[i].InUse = false;
                }

                renderText.Characters.RemoveRange(newLengthWithoutSpaces, renderText.Characters.Count - newLengthWithoutSpaces);
            }

            SetTextToSprites(renderText.Characters, newText, renderText.UseSpecialDigits);
            renderText.Text = newText;

            renderText.UpdatePositions(characterGapSize);
            renderText.UpdateDisplayLayer(displayLayer);

            // update character visibility
            foreach (var character in renderText.Characters)
                character.Sprite.Visible = renderText.Visible;
        }

        public void DestroyText(int index)
        {
            var renderText = renderTexts[index];

            foreach (var character in renderText.Characters)
            {
                character.Sprite.Visible = false;
                character.InUse = false;
            }

            renderTexts[index] = null;
        }

        public void ChangeDisplayLayer(int index, byte displayLayer)
        {
            var renderText = renderTexts[index];

            foreach (var character in renderText.Characters)
                character.Sprite.DisplayLayer = displayLayer;
        }

        public void SetPosition(int index, Position position, int characterGapSize = 8)
        {
            var renderText = renderTexts[index];

            if (position == renderText.Position)
                return;

            renderText.Position.X = position.X;
            renderText.Position.Y = position.Y;

            renderText.UpdatePositions(characterGapSize);
        }

        public void UseSpecialDigits(int index, bool use)
        {
            var renderText = renderTexts[index];

            if (use == renderText.UseSpecialDigits)
                return;

            renderText.UseSpecialDigits = use;

            SetTextToSprites(renderText.Characters, renderText.Text, renderText.UseSpecialDigits);
        }

        public bool IsVisible(int index)
        {
            if (renderTexts[index] == null)
                return false;

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

            throw new ExceptionFreeserf("render", "Unsupported character: " + encoding.GetString(new byte[1] { ch }));
        }

        void SetTextToSprites(List<SpriteInfo> sprites, string text, bool useSpecialDigits)
        {
            var bytes = encoding.GetBytes(text);
            int charIndex = 0;

            // the length of sprites is the same than the text length
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] != 32) // space
                {
                    if (useSpecialDigits && bytes[i] >= '0' && bytes[i] <= '9')
                        sprites[charIndex++].Sprite.TextureAtlasOffset = UI.GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 78u + (uint)(bytes[i] - '0'));
                    else
                        sprites[charIndex++].Sprite.TextureAtlasOffset = UI.GuiObject.GetTextureAtlasOffset(Data.Resource.Font, MapCharToSpriteIndex(bytes[i]));
                }
            }
        }

        List<SpriteInfo> AddCharSprites(int num)
        {
            List<SpriteInfo> sprites = new List<SpriteInfo>(num);

            for (int i = 0; i < num; ++i)
            {
                var spriteInfo = new SpriteInfo()
                {
                    Sprite = spriteFactory.Create(characterSize.Width, characterSize.Height, 0, 0, false, true, 0) as ILayerSprite,
                    InUse = false
                };

                spriteInfo.Sprite.Layer = layer;

                sprites.Add(spriteInfo);
            }

            characterSprites.AddRange(sprites);

            return sprites;
        }
    }
}
