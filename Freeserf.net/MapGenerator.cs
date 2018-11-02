/*
 * MapGenerator.cs - Map generator
 *
 * Copyright (C) 2013-2016  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    using MapPos = UInt32;

    public abstract class MapGenerator
    {
        public enum HeightGenerator
        {
            Midpoints = 0,
            DiamondSquare
        }

        public abstract void Generate();

        public abstract int GetHeight(MapPos pos);
        public abstract Map.Terrain GetTypeUp(MapPos pos);
        public abstract Map.Terrain GetTypeDown(MapPos pos);
        public abstract Map.Object GetObject(MapPos pos);
        public abstract Map.Minerals GetResourceType(MapPos pos);
        public abstract int GetResourceAmount(MapPos pos);
        public abstract List<Map.LandscapeTile> GetLandscape();
    }

    /* Classic map generator as in original game. */
    public class ClassicMapGenerator : MapGenerator
    {
        const int default_max_lake_area;
        const int default_water_level;
        const int default_terrain_spikyness;

        public ClassicMapGenerator(Map map, Random random)
        {

        }

        public void Init(HeightGenerator height_generator, bool preserve_bugs,
            int max_lake_area = default_max_lake_area,
            int water_level = default_water_level,
            int terrain_spikyness = default_terrain_spikyness)
        {

        }

        public override void Generate()
        {
            throw new NotImplementedException();
        }

        public override uint GetHeight(uint pos)
        {
            return tiles[(int)pos].Height;
        }

        public override List<Map.LandscapeTile> GetLandscape()
        {
            return tiles;
        }

        public override Map.Object GetObject(uint pos)
        {
            return tiles[(int)pos].Object;
        }

        public override int GetResourceAmount(uint pos)
        {
            return tiles[(int)pos].ResourceAmount;
        }

        public override Map.Minerals GetResourceType(uint pos)
        {
            return tiles[(int)pos].Mineral;
        }

        public override Map.Terrain GetTypeDown(uint pos)
        {
            return tiles[(int)pos].TypeDown;
        }

        public override Map.Terrain GetTypeUp(uint pos)
        {
            return tiles[(int)pos].TypeUp;
        }




        Map map;
        Random rnd;

        List<Map.LandscapeTile> tiles = new List<Map.LandscapeTile>();
        List<int> tags = new List<int>();
        HeightGenerator height_generator;
        bool preserve_bugs;

        uint water_level;
        uint max_lake_area;
        int terrain_spikyness;

        ushort random_int()
        {

        }

        MapPos pos_add_spirally_random(MapPos pos, int mask)
        {

        }

        bool is_water_tile(MapPos pos)
        {

        }

        bool is_in_water(MapPos pos)
        {

        }

        void init_heights_squares()
        {

        }

        int calc_height_displacement(int avg, int base_, int offset)
        {

        }

        void init_heights_midpoints()
        {

        }

        void init_heights_diamond_square()
        {

        }

        bool adjust_map_height(int h1, int h2, MapPos pos)
        {

        }

        void clamp_heights()
        {

        }

        bool expand_water_position(MapPos pos)
        {

        }

        void expand_water_body(MapPos pos)
        {

        }

        void create_water_bodies()
        {

        }

        void heights_rebase()
        {

        }

        void init_types()
        {

        }

        void clear_all_tags()
        {

        }

        void remove_islands()
        {

        }

        void heights_rescale()
        {

        }

        void seed_terrain_type(Map::Terrain old, Map::Terrain seed,
                               Map::Terrain new_)
        {

        }

        void change_shore_water_type()
        {

        }

        void change_shore_grass_type()
        {

        }

        bool check_desert_down_triangle(MapPos pos)
        {

        }

        bool check_desert_up_triangle(MapPos pos)
        {

        }

        void create_deserts()
        {

        }

        void create_crosses()
        {

        }

        void create_objects()
        {

        }

        bool hexagon_types_in_range(MapPos pos, Map::Terrain min, Map::Terrain max)
        {

        }

        void create_random_object_clusters(int num_clusters, int objs_in_cluster,
                                           int pos_mask, Map::Terrain type_min,
                                           Map::Terrain type_max, int obj_base,
                                           int obj_mask)
        {

        }

        void expand_mineral_cluster(int iters, MapPos pos, int* index,
                                    int amount, Map::Minerals type)
        {

        }

        void create_random_mineral_clusters(int num_clusters, Map::Minerals type,
                                            Map::Terrain min, Map::Terrain max)
        {

        }

        void create_mineral_deposits()
        {

        }

        void clean_up()
        {

        }
    }

    /* Classic map generator that generates identical maps for missions. */
    public class ClassicMissionMapGenerator : ClassicMapGenerator
    {
        public ClassicMissionMapGenerator(Map map, Random random)
        {

        }

        public void Init()
        {

        }
    }
}
