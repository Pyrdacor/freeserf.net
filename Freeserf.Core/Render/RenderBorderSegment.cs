using System;
using Freeserf.Data;

namespace Freeserf.Render
{
    using MapPos = UInt32;
    using Data = Data.Data;

    internal class RenderBorderSegment : RenderObject
    {
        Map map = null;
        MapPos pos = Global.BadMapPos;
        Direction dir = Direction.None;
        int offsetX = 0;
        int offsetY = 0;
        int spriteIndex = 0;
        DataSource dataSource = null;

        public static long CreateIndex(MapPos pos, Direction dir)
        {
            return (((long)dir) << 32) | pos;
        }

        public RenderBorderSegment(Map map, MapPos pos, Direction dir, IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.map = map;
            this.pos = pos;
            this.dir = dir;
            this.dataSource = dataSource;

            Initialize();
        }

        protected override void Create(ISpriteFactory spriteFactory, DataSource dataSource)
        {
            sprite = spriteFactory.Create(RenderMap.TILE_WIDTH, RenderMap.TILE_RENDER_MAX_HEIGHT, 0, 0, false, false);
            sprite.Visible = true;

            UpdateAppearance();
        }

        // TODO: call this after leveling the ground near the border etc (is this even possible?)
        // e.g. when map height changes
        // also used on initialization
        internal void UpdateAppearance()
        {
            int h1 = (int)map.GetHeight(pos);
            int h2 = (int)map.GetHeight(map.Move(pos, dir));
            int hDiff = h2 - h1;

            Map.Terrain t1 = Map.Terrain.Water0;
            Map.Terrain t2 = Map.Terrain.Water0;
            int h3 = 0, h4 = 0, hDiff2 = 0;

            offsetX = 0;
            offsetY = 4 * h1;

            switch (dir)
            {
                case Direction.Right:
                    offsetX += RenderMap.TILE_WIDTH / 2;
                    offsetY -= 2 * (h1 + h2) + 4;
                    t1 = map.TypeDown(pos);
                    t2 = map.TypeUp(map.MoveUp(pos));
                    h3 = (int)map.GetHeight(map.MoveUp(pos));
                    h4 = (int)map.GetHeight(map.MoveDownRight(pos));
                    hDiff2 = h3 - h4 - 4 * hDiff;
                    break;
                case Direction.DownRight:
                    offsetX += RenderMap.TILE_WIDTH / 4;
                    offsetY -= 2 * (h1 + h2) - 6;
                    t1 = map.TypeUp(pos);
                    t2 = map.TypeDown(pos);
                    h3 = (int)map.GetHeight(map.MoveRight(pos));
                    h4 = (int)map.GetHeight(map.MoveDown(pos));
                    hDiff2 = 2 * (h3 - h4);
                    break;
                case Direction.Down:
                    offsetX -= RenderMap.TILE_WIDTH / 4;
                    offsetY -= 2 * (h1 + h2) - 6;
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

            spriteIndex = 0;
            Map.Terrain type = (Map.Terrain)Math.Max((int)t1, (int)t2);

            if (hDiff2 > 1)
            {
                spriteIndex = 0;
            }
            else if (hDiff2 > -9)
            {
                spriteIndex = 1;
            }
            else
            {
                spriteIndex = 2;
            }

            if (type <= Map.Terrain.Water3)
            {
                spriteIndex = 9; /* Bouy */
            }
            else if (type >= Map.Terrain.Snow0)
            {
                spriteIndex += 6;
            }
            else if (type >= Map.Terrain.Desert0)
            {
                spriteIndex += 3;
            }

            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Objects);
            var info = dataSource.GetSpriteInfo(Data.Resource.MapBorder, (uint)spriteIndex);

            sprite.Resize(info.Width, info.Height);
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(20000u + (uint)spriteIndex);
        }

        public void Update(RenderMap map)
        {
            var renderPosition = map.CoordinateSpace.TileSpaceToViewSpace(pos);

            sprite.X = renderPosition.X + offsetX;
            sprite.Y = renderPosition.Y + offsetY;
        }
    }
}
