using Freeserf.Data;
using System;

namespace Freeserf.Render
{
    using Data = Data.Data;
    using MapPos = UInt32;

    internal class RenderBorderSegment : RenderObject
    {
        readonly Map map = null;
        readonly MapPos position = Global.INVALID_MAPPOS;
        readonly Direction direction = Direction.None;
        int offsetX = 0;
        int offsetY = 0;
        int spriteIndex = 0;
        readonly DataSource dataSource = null;

        public static long CreateIndex(MapPos position, Direction direction)
        {
            return (((long)direction) << 32) | position;
        }

        public RenderBorderSegment(Map map, MapPos position, Direction direction,
            IRenderLayer renderLayer, ISpriteFactory spriteFactory, DataSource dataSource)
            : base(renderLayer, spriteFactory, dataSource)
        {
            this.map = map;
            this.position = position;
            this.direction = direction;
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
            int height1 = (int)map.GetHeight(position);
            int height2 = (int)map.GetHeight(map.Move(position, direction));
            int heightDifference = height2 - height1;

            Map.Terrain terrain1 = Map.Terrain.Water0;
            Map.Terrain terrain2 = Map.Terrain.Water0;
            int height3, height4, heightDifference2 = 0;

            offsetX = 0;
            offsetY = 4 * height1;

            switch (direction)
            {
                case Direction.Right:
                    offsetX += RenderMap.TILE_WIDTH / 2;
                    offsetY -= 2 * (height1 + height2) + 4;
                    terrain1 = map.TypeDown(position);
                    terrain2 = map.TypeUp(map.MoveUp(position));
                    height3 = (int)map.GetHeight(map.MoveUp(position));
                    height4 = (int)map.GetHeight(map.MoveDownRight(position));
                    heightDifference2 = height3 - height4 - 4 * heightDifference;
                    break;
                case Direction.DownRight:
                    offsetX += RenderMap.TILE_WIDTH / 4;
                    offsetY -= 2 * (height1 + height2) - 6;
                    terrain1 = map.TypeUp(position);
                    terrain2 = map.TypeDown(position);
                    height3 = (int)map.GetHeight(map.MoveRight(position));
                    height4 = (int)map.GetHeight(map.MoveDown(position));
                    heightDifference2 = 2 * (height3 - height4);
                    break;
                case Direction.Down:
                    offsetX -= RenderMap.TILE_WIDTH / 4;
                    offsetY -= 2 * (height1 + height2) - 6;
                    terrain1 = map.TypeUp(position);
                    terrain2 = map.TypeDown(map.MoveLeft(position));
                    height3 = (int)map.GetHeight(map.MoveLeft(position));
                    height4 = (int)map.GetHeight(map.MoveDown(position));
                    heightDifference2 = 4 * heightDifference - height3 + height4;
                    break;
                default:
                    Debug.NotReached();
                    break;
            }

            spriteIndex = 0;
            var type = (Map.Terrain)Math.Max((int)terrain1, (int)terrain2);

            if (heightDifference2 > 1)
            {
                spriteIndex = 0;
            }
            else if (heightDifference2 > -9)
            {
                spriteIndex = 1;
            }
            else
            {
                spriteIndex = 2;
            }

            if (type <= Map.Terrain.Water3)
            {
                spriteIndex = 9; // Bouy 
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
            var renderPosition = map.CoordinateSpace.TileSpaceToViewSpace(position);

            sprite.X = renderPosition.X + offsetX;
            sprite.Y = renderPosition.Y + offsetY;
        }
    }
}
