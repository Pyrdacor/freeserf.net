using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    public class TextureAtlasManager
    {
        static TextureAtlasManager instance = null;
        static ITextureAtlasBuilderFactory factory = null;
        readonly Dictionary<Layer, ITextureAtlasBuilder> atlasBuilders = new Dictionary<Layer, ITextureAtlasBuilder>();
        readonly Dictionary<Layer, ITextureAtlas> atlas = new Dictionary<Layer, ITextureAtlas>();

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

        public void AddSprite(Layer layer, uint spriteIndex, Sprite sprite)
        {
            if (factory == null)
                throw new ExceptionFreeserf("No TextureAtlasBuilderFactory was registered.");

            if (layer == Layer.GuiBuildings)
                throw new ExceptionFreeserf("Adding sprites for layer GuiBuildings is not allowed.");

            if (atlas.ContainsKey(layer))
                throw new ExceptionFreeserf("Texture atlas already created.");

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

            // use transparent color (TODO: correct for all?)
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


            #region Buildings

            atlasIndex = Layer.Buildings;

            // building sprites are located in sprites 144 to 193 (with some gaps)
            for (uint buildingSprite = 144; buildingSprite <= 193; ++buildingSprite)
            {
                var sprite = data.GetSprite(Data.Resource.MapObject, buildingSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, buildingSprite, sprite);

                // shadow
                sprite = data.GetSprite(Data.Resource.MapShadow, buildingSprite, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 1000u + buildingSprite, sprite); // we use 1000 as the shadow offset (see RenderBuilding)
            }

            // we also add the build-in-progress mask
            AddSprite(atlasIndex, 0u, Sprite.CreateHalfMask(64u, 200u, true));

            // we also add the burning sprites
            // TODO

            #endregion


            #region Map Objects

            atlasIndex = Layer.Objects;

            // sprites 0 - 127 are normal map objects
            for (uint objectSprite = 0; objectSprite < 128; ++objectSprite)
            {
                var sprite = data.GetSprite(Data.Resource.MapObject, objectSprite, color);

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
                var sprite = data.GetSprite(Data.Resource.MapShadow, objectSprite, color);

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

            #endregion


            #region Paths (and borders)

            atlasIndex = Layer.Paths;

            // Note:
            // We enlarge all path sprites to the maximum height of 41 (max mask height) with repeated texture data.
            // The masks are also enlarged to this height but with cleared data (full transparency).
            // This way the masked tiles will show up correctly and we don't need to change sizes when tiles change.

            // 10 path grounds
            for (i = 0; i < 10; ++i)
            {
                var sprite = data.GetSprite(Data.Resource.PathGround, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, i, sprite.RepeatTo(RenderMap.TILE_RENDER_MAX_HEIGHT));
            }

            // 27 path masks
            for (i = 0; i < 27; ++i)
            {
                var sprite = data.GetSprite(Data.Resource.PathMask, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 10u + i, sprite.ClearTo(RenderMap.TILE_WIDTH, RenderMap.TILE_RENDER_MAX_HEIGHT));
            }

            // 10 borders
            for (i = 0; i < 10; ++i)
            {
                var sprite = data.GetSprite(Data.Resource.MapBorder, i, color);

                if (sprite != null)
                    AddSprite(atlasIndex, 100u + i, sprite);
            }

            #endregion


            // TODO
        }
    }
}
