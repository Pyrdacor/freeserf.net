using Freeserf.Data;
using Freeserf.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Freeserf.Render
{
    using Data = Data.Data;

    public class TextureAtlasManager
    {
        static TextureAtlasManager instance = null;
        static ITextureAtlasBuilderFactory factory = null;
        readonly Dictionary<Layer, ITextureAtlasBuilder> atlasBuilders = new Dictionary<Layer, ITextureAtlasBuilder>();
        readonly Dictionary<Layer, ITextureAtlas> atlas = new Dictionary<Layer, ITextureAtlas>();
        readonly Dictionary<Data.Resource, uint> guiResourceOffsets = new Dictionary<Data.Resource, uint>();

        public static TextureAtlasManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new TextureAtlasManager();

                return instance;
            }
        }

        TextureAtlasManager()
        {

        }

        public static void RegisterFactory(ITextureAtlasBuilderFactory factory)
        {
            TextureAtlasManager.factory = factory;
        }

        public uint GetGuiTypeOffset(Data.Resource type)
        {
            if (!guiResourceOffsets.ContainsKey(type))
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "The given resource type is not part of the gui.");

            return guiResourceOffsets[type];
        }

        public void AddSprite(Layer layer, uint spriteIndex, Sprite sprite)
        {
            if (factory == null)
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "No TextureAtlasBuilderFactory was registered.");

            if (layer == Layer.GuiBuildings)
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "Adding sprites for layer GuiBuildings is not allowed.");

            if (atlas.ContainsKey(layer))
                throw new ExceptionFreeserf(ErrorSystemType.Textures, "Texture atlas already created.");

            if (!atlasBuilders.ContainsKey(layer))
                atlasBuilders.Add(layer, factory.Create());

            atlasBuilders[layer].AddSprite(spriteIndex, sprite);
        }

        public ITextureAtlas GetOrCreate(Layer layer)
        {
            if (layer == Layer.GuiBuildings)
                layer = Layer.Buildings; // Use the same atlas as for layer Buildings

            if (!atlas.ContainsKey(layer))
                atlas.Add(layer, atlasBuilders[layer].Create());

            return atlas[layer];
        }

        public void AddAll(DataSource data)
        {
            uint i;
            Layer atlasIndex;
            Sprite sprite;

            var color = Sprite.Color.Transparent;

            // Note: Don't add sprites to the layer GuiBuildings.
            // We use the same atlas as for layer Buildings.


            #region Landscape

            atlasIndex = Layer.Landscape;

            // Note:
            // We enlarge all tile sprites to the maximum height of 41 (max mask height) with repeated texture data.
            // The masks are also enlarged to this height but with cleared data (full transparency).
            // This way the masked tiles will show up correctly and we don't need to change sizes when tiles change.

            // add all normal landscape tile sprites
            for (i = 0; i < 33u; ++i) // 33 map tile sprites
                AddSprite(atlasIndex, i, data.GetSprite(Data.Resource.MapGround, i, color).RepeatTo(RenderMap.TILE_RENDER_MAX_HEIGHT));

            // add all tile up mask sprites
            uint numUpMasks = 61u; // 61 tile up mask sprites
            i = 0;
            uint maskIndex = 0;

            while (maskIndex < numUpMasks)
            {
                var mask = data.GetSprite(Data.Resource.MapMaskUp, i, color);

                if (mask != null)
                {
                    AddSprite(atlasIndex, maskIndex + 33u, mask.ClearTo(RenderMap.TILE_RENDER_MAX_HEIGHT));
                    ++maskIndex;
                }

                ++i;
            }

            // add all tile down mask sprites
            uint numDownMasks = 61u; // 61 tile down mask sprites
            i = 0;
            maskIndex = 0;

            while (maskIndex < numDownMasks)
            {
                var mask = data.GetSprite(Data.Resource.MapMaskDown, i, color);

                if (mask != null)
                {
                    AddSprite(atlasIndex, maskIndex + 33u + 61u, mask.ClearTo(RenderMap.TILE_RENDER_MAX_HEIGHT));
                    ++maskIndex;
                }

                ++i;
            }

            #endregion


            #region Waves

            atlasIndex = Layer.Waves;

            // add water waves
            for (i = 0; i < 16; ++i)
            {
                sprite = data.GetSprite(Data.Resource.MapWaves, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, i, sprite);
            }

            // we also need 3 masks
            // the wave sprites have a size of 48x19
            // the masks have a size of 32x25
            // the masks will therefore be sized to 48x25
            sprite = Sprite.CreateFullMask(48u, 25u);

            if (sprite != null)
                AddSprite(atlasIndex, 16u, sprite);

            sprite = data.GetSprite(Data.Resource.MapMaskUp, 40u, color).ClearTo(48, 25);

            if (sprite != null)
                AddSprite(atlasIndex, 17u, sprite);

            sprite = data.GetSprite(Data.Resource.MapMaskDown, 40u, color).ClearTo(48, 25);

            if (sprite != null)
                AddSprite(atlasIndex, 18u, sprite);

            #endregion


            #region Buildings

            atlasIndex = Layer.Buildings;

            // building sprites are located in sprites 144 to 193 (with some gaps)
            for (uint buildingSprite = 144; buildingSprite <= 193; ++buildingSprite)
            {
                sprite = data.GetSprite(Data.Resource.MapObject, buildingSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, buildingSprite, sprite);

                // shadow
                sprite = data.GetSprite(Data.Resource.MapShadow, buildingSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 1000u + buildingSprite, sprite); // we use 1000 as the shadow offset (see RenderBuilding)
            }

            // we also add the build-in-progress mask
            AddSprite(atlasIndex, 5000u, Sprite.CreateHalfMask(80u, 200u, true));

            // as we use this texture atlas also for gui buildings we add the basic flag sprite as well (for each player color)
            // they can be found at index 128-131
            for (i = 0; i < 4; ++i)
            {
                var playerColor = PlayerInfo.PlayerColors[i];
                var flagColor = new Sprite.Color()
                {
                    Red = playerColor.Red,
                    Green = playerColor.Green,
                    Blue = playerColor.Blue,
                    Alpha = 255
                };

                AddSprite(atlasIndex, 128u + i, data.GetSprite(Data.Resource.MapObject, 128u, flagColor));
            }

            // also we need some trees in gui (for the start attack popup)
            // they start at index 0
            for (i = 0; i < 16; ++i)
            {
                AddSprite(atlasIndex, i, data.GetSprite(Data.Resource.MapObject, i, color));
            }

            // we also add the burning sprites
            for (i = 0; i < 16; ++i)
            {
                AddSprite(atlasIndex, 2000u + i, data.GetSprite(Data.Resource.GameObject, 135u + i, color));
            }

            #endregion


            #region Serfs

            atlasIndex = Layer.Serfs;

            // for all player colors

            // heads
            for (uint serfHeadSprite = 0u; serfHeadSprite <= 629u; ++serfHeadSprite)
            {
                sprite = data.GetSprite(Data.Resource.SerfHead, serfHeadSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, serfHeadSprite, sprite);
            }

            // torsos
            // all 4 player colors
            for (uint c = 0; c < 4; ++c)
            {
                var playerColor = PlayerInfo.PlayerColors[c];
                var serfColor = new Sprite.Color()
                {
                    Red = playerColor.Red,
                    Green = playerColor.Green,
                    Blue = playerColor.Blue,
                    Alpha = 255
                };

                for (uint serfTorsoSprite = 0u; serfTorsoSprite <= 540u; ++serfTorsoSprite)
                {
                    sprite = data.GetSprite(Data.Resource.SerfTorso, serfTorsoSprite, serfColor);

                    if (sprite != null)
                        AddSprite(atlasIndex, 1000u + c * 1000u + serfTorsoSprite, sprite);
                }
            }

            // shadow
            AddSprite(atlasIndex, 5000u, data.GetSprite(Data.Resource.SerfShadow, 0u, color));

            // add fighting stuff (offset is 5000)
            for (i = 197u; i <= 200u; ++i)
            {
                sprite = data.GetSprite(Data.Resource.GameObject, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 5000u + i, sprite);
            }


            #endregion


            #region Map Objects

            atlasIndex = Layer.Objects;

            // sprites 0 - 127 are normal map objects
            for (uint objectSprite = 0; objectSprite < 128; ++objectSprite)
            {
                sprite = data.GetSprite(Data.Resource.MapObject, objectSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, objectSprite, sprite);

                // shadow
                sprite = data.GetSprite(Data.Resource.MapShadow, objectSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 1000u + objectSprite, sprite); // we use 1000 as the shadow offset
            }

            // 128 - 143 are flags
            for (uint objectSprite = 128; objectSprite <= 143; ++objectSprite)
            {
                // shadow
                sprite = data.GetSprite(Data.Resource.MapShadow, objectSprite, color);

                if (sprite != null)
                {
                    AddSprite(atlasIndex, 1000u + objectSprite, sprite.ClearTo(20)); // we use 1000 as the shadow offset
                }

                // all 4 player colors
                for (uint c = 0; c < 4; ++c)
                {
                    var playerColor = PlayerInfo.PlayerColors[c];
                    var flagColor = new Sprite.Color()
                    {
                        Red = playerColor.Red,
                        Green = playerColor.Green,
                        Blue = playerColor.Blue,
                        Alpha = 255
                    };

                    sprite = data.GetSprite(Data.Resource.MapObject, objectSprite, flagColor);

                    if (sprite != null)
                    {
                        // We enlarge the height to 20 as for example the castle
                        // will have a lower/equal baseline otherwise.
                        AddSprite(atlasIndex, objectSprite + c * 16u, sprite.ClearTo(20));
                    }
                }
            }

            // also add resources/materials that can be found at flags or building constructions
            for (uint resourceSprite = 0; resourceSprite <= 25; ++resourceSprite)
            {
                sprite = data.GetSprite(Data.Resource.GameObject, resourceSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 2000u + resourceSprite, sprite.ClearTo(20)); // we use 2000 as the resource offset
            }

            // add special building stuff (offset is 10000)
            for (i = 127u; i <= 196; ++i)
            {
                sprite = data.GetSprite(Data.Resource.GameObject, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 10000u + i, sprite);
            }

            // add borders (offset is 20000)
            for (i = 0; i < 10; ++i)
            {
                sprite = data.GetSprite(Data.Resource.MapBorder, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 20000u + i, sprite);
            }

            #endregion


            #region Paths

            atlasIndex = Layer.Paths;

            // Note:
            // We enlarge all path sprites to the maximum height of 41 (max mask height) with repeated texture data.
            // The masks are also enlarged to this height but with cleared data (full transparency).
            // This way the masked tiles will show up correctly and we don't need to change sizes when tiles change.

            // 10 path grounds
            for (i = 0; i < 10; ++i)
            {
                sprite = data.GetSprite(Data.Resource.PathGround, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, i, sprite.RepeatTo(RenderMap.TILE_RENDER_MAX_HEIGHT));
            }

            // 27 path masks
            for (i = 0; i < 27; ++i)
            {
                sprite = data.GetSprite(Data.Resource.PathMask, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 10u + i, sprite.ClearTo(RenderMap.TILE_WIDTH, RenderMap.TILE_RENDER_MAX_HEIGHT));
            }

            #endregion


            #region Builds

            atlasIndex = Layer.Builds;

            // build sprites are located in sprites 31 to 51 (with a gap)
            // these are build indicators and road build symbols
            for (uint buildSprite = 31; buildSprite <= 51; ++buildSprite)
            {
                sprite = data.GetSprite(Data.Resource.GameObject, buildSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, buildSprite, sprite);
            }

            #endregion


            #region Cursor

            atlasIndex = Layer.Cursor;

            AddSprite(atlasIndex, 0u, data.GetSprite(Data.Resource.Cursor, 0u, color));

            #endregion


            #region Gui

            uint index = 0u;
            var fontColor = new Sprite.Color() { Red = 0x73, Green = 0xb3, Blue = 0x43, Alpha = 0xff };
            var fontShadowColor = new Sprite.Color() { Red = 0x00, Green = 0x00, Blue = 0x00, Alpha = 0xff };

            AddGuiElements(Data.Resource.ArtBox, 14, ref index, data, color);
            AddGuiElements(Data.Resource.ArtFlag, 7, ref index, data, color);
            AddGuiElements(Data.Resource.ArtLandscape, 1, ref index, data, color);
            AddGuiElements(Data.Resource.CreditsBg, 1, ref index, data, color);
            AddGuiElements(Data.Resource.DottedLines, 7, ref index, data, fontColor); // not sure about the color here
            AddGuiElements(Data.Resource.Font, 44, ref index, data, fontColor);
            AddGuiElements(Data.Resource.FontShadow, 44, ref index, data, fontShadowColor);
            AddGuiElements(Data.Resource.FrameBottom, 26, ref index, data, color); // actually there are only 23 sprites but we have to pass the max sprite number + 1 (non-existent sprites are skipped)
            AddGuiElements(Data.Resource.FramePopup, 4, ref index, data, color);
            AddGuiElements(Data.Resource.FrameSplit, 3, ref index, data, color);
            AddGuiElements(Data.Resource.FrameTop, 4, ref index, data, color);
            AddGuiElements(Data.Resource.Indicator, 8, ref index, data, color);
            AddGuiElements(Data.Resource.Logo, 1, ref index, data, color);
            AddGuiElements(Data.Resource.PanelButton, 25, ref index, data, color);
            //AddGuiElements(Data.Resource.Symbol, 16, ref index, data); // TODO: skip them for now. maybe re-add later
            AddGuiElements(Data.Resource.Icon, 318, ref index, data, color);

            // game init box background
            // we add a compound background of sprites 290-293 with a bigger size
            var backgroundSprites = new Sprite[5];
            backgroundSprites[0] = data.GetSprite(Data.Resource.Icon, 290u, color);
            backgroundSprites[1] = data.GetSprite(Data.Resource.Icon, 291u, color);
            backgroundSprites[2] = data.GetSprite(Data.Resource.Icon, 292u, color);
            backgroundSprites[3] = data.GetSprite(Data.Resource.Icon, 293u, color);
            backgroundSprites[4] = data.GetSprite(Data.Resource.Icon, 294u, color);
            var backgroundCompoundSprite = new Sprite(320u, 184u);

            for (int row = 0; row < 23; ++row) // 23 rows with 8 pixels each = 184 pixels
            {
                for (int column = 0; column < 8; ++column) // 8 columns with 40 pixels each = 320 pixels
                {
                    backgroundCompoundSprite.Add(column * 40, row * 8, backgroundSprites[row % 5]);
                }
            }

            // index is now 318 inside the icons
            AddSprite(Layer.Gui, index++, backgroundCompoundSprite);

            // notification box background
            // we add a compound background of sprites 314 with a bigger size
            backgroundCompoundSprite = new Sprite(128u, 144u);
            sprite = data.GetSprite(Data.Resource.Icon, 314u, color);

            // 9 rows, 8 columns with 16x16 pixels = 128x144
            for (int row = 0; row < 9; ++row)
            {
                for (int column = 0; column < 8; ++column)
                {
                    backgroundCompoundSprite.Add(column * 16, row * 16, sprite);
                }
            }

            // index is now 319 inside the icons
            AddSprite(Layer.Gui, index++, backgroundCompoundSprite);

            // popup backgrounds
            var popupBackgrounds = Enum.GetValues(typeof(PopupBox.BackgroundPattern));

            foreach (PopupBox.BackgroundPattern popupBackground in popupBackgrounds)
            {
                if (popupBackground >= PopupBox.BackgroundPattern.OverallComparison && popupBackground <= PopupBox.BackgroundPattern.Shield)
                    continue; // these are compound backgrounds that are handled internal

                backgroundCompoundSprite = new Sprite(128u, 144u);
                sprite = data.GetSprite(Data.Resource.Icon, (uint)popupBackground, color);

                // 9 rows, 8 columns with 16x16 pixels = 128x144
                for (int row = 0; row < 9; ++row)
                {
                    for (int column = 0; column < 8; ++column)
                    {
                        backgroundCompoundSprite.Add(column * 16, row * 16, sprite);
                    }
                }

                // index is now (320 + popup box background pattern index) inside the icons
                AddSprite(Layer.Gui, index + (uint)popupBackground, backgroundCompoundSprite);
            }

            // we create a compound icon for game type load
            var loadIcon = new Sprite(32u, 32u);
            loadIcon.Add(data.GetSprite(Data.Resource.Icon, 262u, color));
            loadIcon.Add(14, 5, data.GetSprite(Data.Resource.Icon, 93u, color));

            AddSprite(Layer.Gui, guiResourceOffsets[Data.Resource.Icon] + 500u, loadIcon);

            #endregion


            #region UI Font

            // add new UI font
            guiResourceOffsets.Add(Data.Resource.UIText, guiResourceOffsets[Data.Resource.Icon] + 1000u);
            AddSprite(Layer.GuiFont, guiResourceOffsets[Data.Resource.UIText], Sprite.CreateFromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("Freeserf.assets.ui_font.png")
            ));

            #endregion

        }

        void AddGuiElements(Data.Resource resourceType, uint num, ref uint index, DataSource data, Sprite.Color color)
        {
            guiResourceOffsets.Add(resourceType, index);

            for (uint i = 0; i < num; ++i)
            {
                var sprite = data.GetSprite(resourceType, i, color);

                if (sprite != null)
                    AddSprite(Layer.Gui, index, sprite);

                ++index;
            }
        }
    }
}
