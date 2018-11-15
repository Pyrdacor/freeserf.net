using System;

namespace Freeserf.Render
{
    using MapPos = UInt32;

    // TODO: I guess in some cases the building will have a lower baseline than the
    //       road to the building (e.g. castle). Therefore the road is not drawn due
    //       to depth testing. We should have a look at this later.
    internal class RenderRoadSegment : RenderObject
    {
        Map map = null;
        MapPos pos = Global.BadMapPos;
        Direction dir = Direction.None;
        int offsetX = 0;
        int offsetY = 0;
        int spriteIndex = 0;
        int maskIndex = 0;

        static Position[] groundOffsets = null;
        static Position[] maskOffsets = null;

        public static long CreateIndex(MapPos pos, Direction dir)
        {
            return (((long)dir) << 32) | pos;
        }

        public RenderRoadSegment(Map map, MapPos pos, Direction dir, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.map = map;
            this.pos = pos;
            this.dir = dir;

            Initialize();

            InitOffsets(dataSource);
        }

        static void InitOffsets(DataSource dataSource)
        {
            if (groundOffsets == null)
            {
                groundOffsets = new Position[10];
                maskOffsets = new Position[27];

                Sprite sprite;
                var color = Sprite.Color.Transparent;

                // grounds
                for (uint i = 0; i < 10; ++i)
                {
                    sprite = dataSource.GetSprite(Data.Resource.PathGround, i, color);
                    groundOffsets[(int)i] = new Position((int)sprite.Width, (int)sprite.Height);
                }

                // masks
                for (uint i = 0; i < 27; ++i)
                {
                    sprite = dataSource.GetSprite(Data.Resource.PathMask, i, color);
                    maskOffsets[(int)i] = new Position((int)sprite.Width, (int)sprite.Height);
                }
            }
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            sprite = spriteFactory.Create(RenderMap.TILE_WIDTH, RenderMap.TILE_RENDER_MAX_HEIGHT, 0, 0, true);

            UpdateAppearance();
        }

        // TODO: call this after leveling the ground near the road etc
        // e.g. when map height changes
        // also used on initialization
        void UpdateAppearance()
        {
            int h1 = (int)map.GetHeight(pos);
            int h2 = (int)map.GetHeight(map.Move(pos, dir));
            int hDiff = h1 - h2;

            Map.Terrain t1 = Map.Terrain.Water0;
            Map.Terrain t2 = Map.Terrain.Water0;
            int h3 = 0, h4 = 0, hDiff2 = 0;

            switch (dir)
            {
                case Direction.Right:
                    offsetY = -(4 * Math.Max(h1, h2) + 2);
                    t1 = map.TypeDown(pos);
                    t2 = map.TypeUp(map.MoveUp(pos));
                    h3 = (int)map.GetHeight(map.MoveUp(pos));
                    h4 = (int)map.GetHeight(map.MoveDownRight(pos));
                    hDiff2 = (h3 - h4) - 4 * hDiff;
                    break;
                case Direction.DownRight:
                    offsetY = -(4 * h1 + 2);
                    t1 = map.TypeUp(pos);
                    t2 = map.TypeDown(pos);
                    h3 = (int)map.GetHeight(map.MoveRight(pos));
                    h4 = (int)map.GetHeight(map.MoveDown(pos));
                    hDiff2 = 2 * (h3 - h4);
                    break;
                case Direction.Down:
                    offsetX = -RenderMap.TILE_WIDTH / 2;
                    offsetY = -(4 * h1 + 2);
                    t1 = map.TypeUp(pos);
                    t2 = map.TypeDown(map.MoveLeft(pos));
                    h3 = (int)map.GetHeight(map.MoveLeft(pos));
                    h4 = (int)map.GetHeight(map.MoveDown(pos));
                    hDiff2 = 4 * hDiff - h3 + h4;
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            // TODO: RenderMap.GetObjectRenderPosition will add an
            //       offset to y that is based on tile height.
            //       But this is already considered here I guess.
            //       So maybe uncomment the following line and
            //       leave a note about it later.
            //offsetY += h1 * 4;
            // TODO: rendering is still not at the right spot (seems to be wrong dependent of map pos)

            maskIndex = hDiff + 4 + (int)dir * 9;
            spriteIndex = 0;
            Map.Terrain type = (Map.Terrain)Math.Max((int)t1, (int)t2);

            if (hDiff2 > 4)
            {
                spriteIndex = 0;
            }
            else if (hDiff2 > -6)
            {
                spriteIndex = 1;
            }
            else
            {
                spriteIndex = 2;
            }

            if (type <= Map.Terrain.Water3)
            {
                spriteIndex = 9;
            }
            else if (type >= Map.Terrain.Snow0)
            {
                spriteIndex += 6;
            }
            else if (type >= Map.Terrain.Desert0)
            {
                spriteIndex += 3;
            }

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Paths);

            sprite.TextureAtlasOffset = textureAtlas.GetOffset((uint)spriteIndex);
            (sprite as IMaskedSprite).MaskTextureAtlasOffset = textureAtlas.GetOffset(10u + (uint)maskIndex);
        }

        public void Update(RenderMap map)
        {
            var renderPosition = map.GetObjectRenderPosition(pos);

            // the mask offset is the right offset for drawing
            sprite.X = renderPosition.X + offsetX + maskOffsets[maskIndex].X;
            sprite.Y = renderPosition.Y + offsetY + maskOffsets[maskIndex].Y;
        }
    }
}
