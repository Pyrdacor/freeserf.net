using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Freeserf
{
    public static partial class Global
    {
        public const int UIFontCharacterWidth = 16;
        public const int UIFontCharacterHeight = 16;
        public const int UIFontCharactersPerLine = 16;
        public const int UIFontCharacterLines = 16;
    }
}

namespace Freeserf.Render
{
    using Data = Data.Data;

    internal enum TextRenderType
    {
        Legacy,
        LegacySpecialDigits,
        NewUI
    }

    internal class TextRenderer
    {
        static readonly Encoding encoding = Encoding.GetEncoding("iso-8859-1");
        // this are the valid input characters
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

            public TextRenderType TextRenderType
            {
                get;
                set;
            }

            public RenderText(string text)
            {
                Text = text;
            }

            public void UpdatePositions(int characterGapSize)
            {
                int x = 0;
                int characterIndex = 0;

                foreach (var character in Text)
                {
                    if (character != ' ')
                    {
                        Characters[characterIndex].Sprite.X = Position.X + x;
                        Characters[characterIndex].Sprite.Y = Position.Y;
                        ++characterIndex;
                    }

                    x += characterGapSize;
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

        readonly IRenderLayer layerLegacy = null;
        readonly IRenderLayer layerNewFont = null;
        readonly ISpriteFactory spriteFactory = null;
        readonly Size characterSizeLegacy = null;
        readonly Size characterSizeNew = null;
        readonly List<RenderText> renderTextsLegacy = new List<RenderText>();
        readonly List<RenderText> renderTextsNew = new List<RenderText>();
        readonly List<SpriteInfo> characterSpritesLegacy = new List<SpriteInfo>(); // shared by all texts
        readonly List<SpriteInfo> characterSpritesNew = new List<SpriteInfo>(); // shared by all texts

        public TextRenderer(IRenderView renderView)
        {
            spriteFactory = renderView.SpriteFactory;
            layerLegacy = renderView.GetLayer(Layer.Gui);
            layerNewFont = renderView.GetLayer(Layer.GuiFont);

            // original size is 8x8 pixels
            characterSizeLegacy = new Size(8, 8);
            // new font uses a different size
            characterSizeNew = new Size(Global.UIFontCharacterWidth, Global.UIFontCharacterHeight);
        }

        public int CreateText(string text, byte displayLayer, TextRenderType renderType, Position position = null, int characterGapSize = 8)
        {
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;
            var characterSprites = renderType == TextRenderType.NewUI ? characterSpritesNew : characterSpritesLegacy;

            // look if we have an unused render text
            int index = renderTexts.FindIndex(checkText => checkText == null);
            var spritePool = characterSprites.Where(sprite => !sprite.InUse);
            int numAvailableCharacters = spritePool.Count();
            int lengthWithoutSpaces = text.Replace(" ", "").Length;

            List<SpriteInfo> sprites;

            if (numAvailableCharacters < lengthWithoutSpaces)
            {
                sprites = new List<SpriteInfo>(spritePool);
                sprites.AddRange(AddCharacterSprites(lengthWithoutSpaces - numAvailableCharacters, renderType));
            }
            else
            {
                sprites = new List<SpriteInfo>(spritePool.Take(lengthWithoutSpaces));
            }

            // mark all as in use
            foreach (var sprite in sprites)
                sprite.InUse = true;

            var renderText = new RenderText(text);

            renderText.TextRenderType = renderType;
            renderText.Characters = sprites;

            if (position != null)
            {
                renderText.Position.X = position.X;
                renderText.Position.Y = position.Y;
            }

            SetTextToSprites(renderText.Characters, text, renderText.TextRenderType);
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

        public void ShowText(TextRenderType renderType, int index, bool show)
        {
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;
            var renderText = renderTexts[index];

            if (renderText.Visible == show)
                return; // nothing to do

            foreach (var character in renderText.Characters)
                character.Sprite.Visible = show;

            renderText.Visible = show;
        }

        public void ChangeText(int index, string newText, byte displayLayer, TextRenderType renderType, int characterGapSize = 8)
        {
            var characterSprites = renderType == TextRenderType.NewUI ? characterSpritesNew : characterSpritesLegacy;
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;
            var renderText = renderTexts[index];

            if (renderText.Text == newText)
                return;

            int newLengthWithoutSpaces = newText.Replace(" ", "").Length;

            if (renderText.Characters.Count < newLengthWithoutSpaces)
            {
                var spritePool = characterSprites.Where(sprite => !sprite.InUse);

                int numAdditionalChars = newLengthWithoutSpaces - renderText.Characters.Count;
                int numAvailableChars = spritePool.Count();

                List<SpriteInfo> sprites;

                if (numAvailableChars < numAdditionalChars)
                {
                    sprites = new List<SpriteInfo>(spritePool);
                    sprites.AddRange(AddCharacterSprites(numAdditionalChars - numAvailableChars, renderType));
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

            SetTextToSprites(renderText.Characters, newText, renderText.TextRenderType);
            renderText.Text = newText;

            renderText.UpdatePositions(characterGapSize);
            renderText.UpdateDisplayLayer(displayLayer);

            // update character visibility
            foreach (var character in renderText.Characters)
                character.Sprite.Visible = renderText.Visible;
        }

        public void DestroyText(TextRenderType renderType, int index)
        {
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;
            var renderText = renderTexts[index];

            foreach (var character in renderText.Characters)
            {
                character.Sprite.Visible = false;
                character.InUse = false;
            }

            renderTexts[index] = null;
        }

        public void ChangeDisplayLayer(TextRenderType renderType, int index, byte displayLayer)
        {
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;
            var renderText = renderTexts[index];

            foreach (var character in renderText.Characters)
                character.Sprite.DisplayLayer = displayLayer;
        }

        public void SetPosition(TextRenderType renderType, int index, Position position, int characterGapSize = 8)
        {
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;
            var renderText = renderTexts[index];

            if (position == renderText.Position)
                return;

            renderText.Position.X = position.X;
            renderText.Position.Y = position.Y;

            renderText.UpdatePositions(characterGapSize);
        }

        /// <summary>
        /// This is only used for legacy types
        /// </summary>
        /// <param name="index"></param>
        /// <param name="type"></param>
        public void SetRenderType(int index, TextRenderType type)
        {
            if (type == TextRenderType.NewUI)
                throw new ExceptionFreeserf("Switching to new UI font is not supported.");

            var renderText = renderTextsLegacy[index];

            if (type == renderText.TextRenderType)
                return;

            renderText.TextRenderType = type;

            SetTextToSprites(renderText.Characters, renderText.Text, renderText.TextRenderType);
        }

        public bool IsVisible(TextRenderType renderType, int index)
        {
            var renderTexts = renderType == TextRenderType.NewUI ? renderTextsNew : renderTextsLegacy;

            if (renderTexts[index] == null)
                return false;

            return renderTexts[index].Visible;
        }

        uint MapCharacterToSpriteIndex(byte ch)
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

            return 42u; // Invalid characters are printed as '?'
            //throw new ExceptionFreeserf(ErrorSystemType.Render, "Unsupported character: " + encoding.GetString(new byte[1] { ch }));
        }

        void SetTextToSprites(List<SpriteInfo> sprites, string text, TextRenderType renderType)
        {
            var bytes = encoding.GetBytes(text);
            int charIndex = 0;

            // the length of sprites is the same than the text length
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] != 32) // space
                {
                    if (renderType == TextRenderType.NewUI)
                    {
                        if (bytes[i] >= 128)
                            bytes[i] = 0; // map to unsupported character

                        var textureAtlasOffset = UI.GuiObject.GetTextureAtlasOffset(Data.Resource.UIText, 0);
                        textureAtlasOffset.X += (bytes[i] % Global.UIFontCharactersPerLine) * Global.UIFontCharacterWidth;
                        textureAtlasOffset.Y += (bytes[i] / Global.UIFontCharactersPerLine) * Global.UIFontCharacterHeight;
                        sprites[charIndex].Sprite.Layer = layerNewFont;
                        sprites[charIndex++].Sprite.TextureAtlasOffset = textureAtlasOffset;
                    }
                    else
                    {
                        sprites[charIndex].Sprite.Layer = layerLegacy;

                        if (renderType == TextRenderType.LegacySpecialDigits && bytes[i] >= '0' && bytes[i] <= '9')
                            sprites[charIndex++].Sprite.TextureAtlasOffset = UI.GuiObject.GetTextureAtlasOffset(Data.Resource.Icon, 78u + (uint)(bytes[i] - '0'));
                        else
                            sprites[charIndex++].Sprite.TextureAtlasOffset = UI.GuiObject.GetTextureAtlasOffset(Data.Resource.Font, MapCharacterToSpriteIndex(bytes[i]));
                    }
                }
            }
        }

        List<SpriteInfo> AddCharacterSprites(int number, TextRenderType renderType)
        {
            var characterSprites = renderType == TextRenderType.NewUI ? characterSpritesNew : characterSpritesLegacy;
            var characterSize = renderType == TextRenderType.NewUI ? characterSizeNew : characterSizeLegacy;
            List<SpriteInfo> sprites = new List<SpriteInfo>(number);

            for (int i = 0; i < number; ++i)
            {
                var spriteInfo = new SpriteInfo()
                {
                    Sprite = spriteFactory.Create(characterSize.Width, characterSize.Height, 0, 0, false, true, 0) as ILayerSprite,
                    InUse = false
                };

                spriteInfo.Sprite.Layer = renderType == TextRenderType.NewUI ? layerNewFont : layerLegacy;

                sprites.Add(spriteInfo);
            }

            characterSprites.AddRange(sprites);

            return sprites;
        }
    }
}
