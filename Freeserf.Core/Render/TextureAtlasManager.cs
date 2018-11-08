using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    public class TextureAtlasManager
    {
        static TextureAtlasManager instance = null;
        static ITextureAtlasBuilderFactory factory = null;
        readonly Dictionary<int, ITextureAtlasBuilder> atlasBuilders = new Dictionary<int, ITextureAtlasBuilder>();
        readonly Dictionary<int, ITextureAtlas> atlas = new Dictionary<int, ITextureAtlas>();

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

        public void AddSprite(int atlasIndex, uint spriteIndex, Sprite sprite)
        {
            if (factory == null)
                throw new ExceptionFreeserf("No TextureAtlasBuilderFactory was registered.");

            if (atlas.ContainsKey(atlasIndex))
                throw new ExceptionFreeserf("Texture atlas already created.");

            if (!atlasBuilders.ContainsKey(atlasIndex))
                atlasBuilders.Add(atlasIndex, factory.Create());

            atlasBuilders[atlasIndex].AddSprite(spriteIndex, sprite);
        }

        public ITextureAtlas GetOrCreate(int index)
        {
            if (!atlas.ContainsKey(index))
                atlas.Add(index, atlasBuilders[index].Create());

            return atlas[index];
        }
    }
}
