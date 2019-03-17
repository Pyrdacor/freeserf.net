/*
 * Inventory.cs - Resources related definitions.
 *
 * Copyright (C) 2015  Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    using SerfMap = Dictionary<Serf.Type, uint>;
    using ResourceMap = Dictionary<Resource.Type, int>;

    public class Inventory : GameObject, IDisposable
    {
        public enum Mode
        {
            In = 0,    // 00
            Stop = 1,  // 01
            Out = 3,   // 11
        }

        class OutQueue
        {
            public Resource.Type Type;
            public uint Dest;
        }

        /* Index of flag connected to this inventory */
        uint flag = 0;
        /* Index of building containing this inventory */
        uint building = 0;
        /* Count of resources */
        readonly ResourceMap resources = new ResourceMap();
        /* Resources waiting to be moved out */
        readonly OutQueue[] outQueue = new OutQueue[2]
        {
            new OutQueue(), new OutQueue()
        };
        /* Count of serfs waiting to move out */
        uint serfsOut = 0;
        /* Count of generic serfs */
        uint genericCount = 0;
        uint resourceDir = 0;
        /* Indices to serfs of each type */
        readonly SerfMap serfs = new SerfMap();

        public Inventory(Game game, uint index)
            : base(game, index)
        {
            for (int i = 0; i < 2; i++)
            {
                outQueue[i].Type = Resource.Type.None;
                outQueue[i].Dest = 0;
            }

            foreach (Serf.Type type in Enum.GetValues(typeof(Serf.Type)))
            {
                serfs.Add(type, 0u);
            }

            for (var r = Resource.Type.Fish; r <= Resource.Type.Shield; ++r)
            {
                resources.Add(r, 0);
            }
        }

        /* Inventory owner */
        public uint Player { get; internal set; } = 0;

        public uint GetFlagIndex()
        {
            return flag;
        }

        public void SetFlagIndex(uint flagIndex)
        {
            flag = flagIndex;
        }

        public uint GetBuildingIndex()
        {
            return building;
        }

        public void SetBuildingIndex(uint buildingIndex)
        {
            building = buildingIndex;
        }

        public Mode GetResourceMode()
        {
            return (Mode)(resourceDir & 3);
        }

        public void SetResourceMode(Mode mode)
        {
            resourceDir = (resourceDir & 0xFC) | (uint)mode;
        }

        public Mode GetSerfMode()
        {
            return (Mode)((resourceDir >> 2) & 3);
        }

        public void SetSerfMode(Mode mode)
        {
            resourceDir = (resourceDir & 0xF3) | ((uint)mode << 2);
        }

        public bool HaveAnyOutMode()
        {
            return (resourceDir & 0x0A) != 0;
        }

        public uint GetSerfQueueLength()
        {
            return serfsOut;
        }

        public void SerfAway()
        {
            --serfsOut;
        }

        public bool CallOutSerf(Serf serf)
        {
            var serfType = serf.GetSerfType();

            if (serfs[serfType] != serf.Index)
            {
                return false;
            }

            serfs[serfType] = 0;

            if (serfType == Serf.Type.Generic)
            {
                --genericCount;
            }

            ++serfsOut;

            return true;
        }

        public Serf CallOutSerf(Serf.Type type)
        {
            if (serfs[type] == 0)
            {
                return null;
            }

            Serf serf = Game.GetSerf(serfs[type]);

            if (!CallOutSerf(serf))
            {
                return null;
            }

            return serf;
        }

        public bool CallInternal(Serf serf)
        {
            if (serfs[serf.GetSerfType()] != serf.Index)
            {
                return false;
            }

            serfs[serf.GetSerfType()] = 0;

            return true;
        }

        public Serf CallInternal(Serf.Type type)
        {
            if (serfs[type] == 0)
            {
                return null;
            }

            Serf serf = Game.GetSerf(serfs[type]);
            serfs[type] = 0;

            return serf;
        }

        public void SerfComeBack()
        {
            ++genericCount;
        }

        public uint FreeSerfCount()
        {
            return genericCount;
        }

        public bool HaveSerf(Serf.Type type)
        {
            return serfs[type] != 0;
        }

        public uint GetCountOf(Resource.Type resource)
        {
            return (uint)resources[resource];
        }

        public ResourceMap GetAllResources()
        {
            return resources;
        }

        public void PopResource(Resource.Type resource)
        {
            --resources[resource];
        }

        public void PushResource(Resource.Type resource)
        {
            resources[resource] += (resources[resource] < 50000) ? 1 : 0;
        }

        public bool HasResourceInQueue()
        {
            return (outQueue[0].Type != Resource.Type.None);
        }

        public bool IsQueueFull()
        {
            return (outQueue[1].Type != Resource.Type.None);
        }

        public void GetResourceFromQueue(ref Resource.Type res, ref uint dest)
        {
            res = outQueue[0].Type;
            dest = outQueue[0].Dest;

            outQueue[0].Type = outQueue[1].Type;
            outQueue[0].Dest = outQueue[1].Dest;

            outQueue[1].Type = Resource.Type.None;
            outQueue[1].Dest = 0;
        }

        public void ResetQueueForDest(Flag flag)
        {
            if (outQueue[1].Type != Resource.Type.None && outQueue[1].Dest == flag.Index)
            {
                PushResource(outQueue[1].Type);
                outQueue[1].Type = Resource.Type.None;
            }

            if (outQueue[0].Type != Resource.Type.None && outQueue[0].Dest == flag.Index)
            {
                PushResource(outQueue[0].Type);
                outQueue[0].Type = outQueue[1].Type;
                outQueue[0].Dest = outQueue[1].Dest;
                outQueue[1].Type = Resource.Type.None;
            }
        }

        public bool HasFood()
        {
            return resources[Resource.Type.Fish] != 0 ||
                   resources[Resource.Type.Meat] != 0 ||
                   resources[Resource.Type.Bread] != 0;
        }

        /* Take resource from inventory and put in out queue.
           The resource must be present.*/
        public void AddToQueue(Resource.Type type, uint dest)
        {
            if (type == Resource.Type.GroupFood)
            {
                /* Select the food resource with highest amount available */
                if (resources[Resource.Type.Meat] > resources[Resource.Type.Bread])
                {
                    if (resources[Resource.Type.Meat] > resources[Resource.Type.Fish])
                    {
                        type = Resource.Type.Meat;
                    }
                    else
                    {
                        type = Resource.Type.Fish;
                    }
                }
                else if (resources[Resource.Type.Bread] > resources[Resource.Type.Fish])
                {
                    type = Resource.Type.Bread;
                }
                else
                {
                    type = Resource.Type.Fish;
                }
            }

            if (resources[type] == 0)
            {
                throw new ExceptionFreeserf(Game, "inventory", "No resource with type.");
            }

            --resources[type];

            if (outQueue[0].Type == Resource.Type.None)
            {
                outQueue[0].Type = type;
                outQueue[0].Dest = dest;
            }
            else
            {
                outQueue[1].Type = type;
                outQueue[1].Dest = dest;
            }
        }

        static readonly uint[,] SuppliesTemplates = new uint[5, 26]
        {
            {  0,  0,  0,  0,  0,  0,  0,   7,   0,   2,  0,   0,   0,  0,  0,  1,
                6,  1,  0,  0,  1,  2,  3,   0,  10,  10 },
            {  2,  1,  1,  3,  2,  1,  0,  25,   1,   8,  4,   3,   8,  2,  1,  3,
                12,  2,  1,  1,  2,  3,  4,   1,  30,  30 },
            {  3,  2,  2, 10,  3,  1,  0,  40,   2,  20, 12,   8,  20,  4,  2,  5,
                20,  3,  1,  2,  3,  4,  6,   2,  60,  60 },
            {  8,  4,  6, 20,  7,  5,  3,  80,   5,  40, 20,  40,  50,  8,  4, 10,
                30,  5,  2,  4,  6,  6, 12,   4, 100, 100 },
            { 30, 10, 30, 50, 10, 30, 10, 200,  10, 100, 30, 150, 100, 10,  5, 20,
                50, 10,  5, 10, 20, 20, 50,  10, 200, 200 }
        };

        /* Create initial resources */
        public void ApplySuppliesPreset(uint supplies)
        {
            IEnumerable<uint> template1 = null;
            IEnumerable<uint> template2 = null;

            if (supplies < 10)
            {
                template1 = SuppliesTemplates.SliceRow(0);
                template2 = SuppliesTemplates.SliceRow(1);
            }
            else if (supplies < 20)
            {
                template1 = SuppliesTemplates.SliceRow(1);
                template2 = SuppliesTemplates.SliceRow(2);
                supplies -= 10;
            }
            else if (supplies < 30)
            {
                template1 = SuppliesTemplates.SliceRow(2);
                template2 = SuppliesTemplates.SliceRow(3);
                supplies -= 20;
            }
            else if (supplies < 40)
            {
                template1 = SuppliesTemplates.SliceRow(3);
                template2 = SuppliesTemplates.SliceRow(4);
                supplies -= 30;
            }
            else
            {
                template1 = SuppliesTemplates.SliceRow(4);
                template2 = SuppliesTemplates.SliceRow(4);
                supplies -= 40;
            }

            var template1Array = template1.ToArray();
            var template2Array = template2.ToArray();

            for (int i = 0; i < 26; i++)
            {
                uint t1 = template1Array[i];
                uint n = (template2Array[i] - t1) * supplies * 6554;

                if (n >= 0x8000)
                    ++t1;

                resources[(Resource.Type)i] = (int)(t1 + (n >> 16));
            }
        }

        public Serf CallTransporter(bool water)
        {
            Serf serf = null;

            if (water)
            {
                if (serfs[Serf.Type.Sailor] != 0)
                {
                    serf = Game.GetSerf(serfs[Serf.Type.Sailor]);
                    serfs[Serf.Type.Sailor] = 0;
                }
                else
                {
                    if ((serfs[Serf.Type.Generic] != 0) &&
                        (resources[Resource.Type.Boat] > 0))
                    {
                        serf = Game.GetSerf(serfs[Serf.Type.Generic]);
                        serfs[Serf.Type.Generic] = 0;
                        --resources[Resource.Type.Boat];
                        serf.SetSerfType(Serf.Type.Sailor);
                        --genericCount;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (serfs[Serf.Type.Transporter] != 0)
                {
                    serf = Game.GetSerf(serfs[Serf.Type.Transporter]);
                    serfs[Serf.Type.Transporter] = 0;
                }
                else
                {
                    if (serfs[Serf.Type.Generic] != 0)
                    {
                        serf = Game.GetSerf(serfs[Serf.Type.Generic]);
                        serfs[Serf.Type.Generic] = 0;
                        serf.SetSerfType(Serf.Type.Transporter);
                        --genericCount;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            ++serfsOut;

            return serf;
        }


        public bool PromoteSerfToKnight(Serf serf)
        {
            if (serf.GetSerfType() != Serf.Type.Generic)
            {
                return false;
            }

            if (resources[Resource.Type.Sword] == 0 ||
                resources[Resource.Type.Shield] == 0)
            {
                return false;
            }

            PopResource(Resource.Type.Sword);
            PopResource(Resource.Type.Shield);
            --genericCount;
            serfs[Serf.Type.Generic] = 0;

            serf.SetSerfType(Serf.Type.Knight0);

            return true;
        }


        public Serf SpawnSerfGeneric()
        {
            Serf serf = Game.GetPlayer(Player).SpawnSerfGeneric();

            if (serf != null)
            {
                serf.InitGeneric(this);

                ++genericCount;

                if (serfs[Serf.Type.Generic] == 0)
                {
                    serfs[Serf.Type.Generic] = serf.Index;
                }
            }

            return serf;
        }

        internal static readonly Resource.Type[] ResourcesNeededForSpecializing = new Resource.Type[]
        {
            Resource.Type.None,    Resource.Type.None,    // SERF_TRANSPORTER = 0,
            Resource.Type.Boat,    Resource.Type.None,    // SERF_SAILOR,
            Resource.Type.Shovel,  Resource.Type.None,    // SERF_DIGGER,
            Resource.Type.Hammer,  Resource.Type.None,    // SERF_BUILDER,
            Resource.Type.None,    Resource.Type.None,    // SERF_TRANSPORTER_INVENTORY,
            Resource.Type.Axe,     Resource.Type.None,    // SERF_LUMBERJACK,
            Resource.Type.Saw,     Resource.Type.None,    // TypeSawmiller,
            Resource.Type.Pick,    Resource.Type.None,    // TypeStonecutter,
            Resource.Type.None,    Resource.Type.None,    // TypeForester,
            Resource.Type.Pick,    Resource.Type.None,    // TypeMiner,
            Resource.Type.None,    Resource.Type.None,    // TypeSmelter,
            Resource.Type.Rod,     Resource.Type.None,    // TypeFisher,
            Resource.Type.None,    Resource.Type.None,    // TypePigFarmer,
            Resource.Type.Cleaver, Resource.Type.None,    // TypeButcher,
            Resource.Type.Scythe,  Resource.Type.None,    // TypeFarmer,
            Resource.Type.None,    Resource.Type.None,    // TypeMiller,
            Resource.Type.None,    Resource.Type.None,    // TypeBaker,
            Resource.Type.Hammer,  Resource.Type.None,    // TypeBoatBuilder,
            Resource.Type.Hammer,  Resource.Type.Saw,     // TypeToolmaker,
            Resource.Type.Hammer,  Resource.Type.Pincer,  // TypeWeaponSmith,
            Resource.Type.Hammer,  Resource.Type.None,    // TypeGeologist,
            Resource.Type.None,    Resource.Type.None,    // TypeGeneric,
            Resource.Type.Sword,   Resource.Type.Shield,  // TypeKnight0,
            Resource.Type.None,    Resource.Type.None,    // TypeKnight1,
            Resource.Type.None,    Resource.Type.None,    // TypeKnight2,
            Resource.Type.None,    Resource.Type.None,    // TypeKnight3,
            Resource.Type.None,    Resource.Type.None,    // TypeKnight4,
            Resource.Type.None,    Resource.Type.None,    // TypeDead
        };

        public bool SpecializeSerf(Serf serf, Serf.Type type)
        {
            if (serf.GetSerfType() != Serf.Type.Generic)
            {
                return false;
            }

            if (serfs[type] != 0)
            {
                return false;
            }

            if ((ResourcesNeededForSpecializing[(int)type * 2] != Resource.Type.None)
                && (resources[ResourcesNeededForSpecializing[(int)type * 2]] == 0))
            {
                return false;
            }

            if ((ResourcesNeededForSpecializing[(int)type * 2 + 1] != Resource.Type.None)
                && (resources[ResourcesNeededForSpecializing[(int)type * 2 + 1]] == 0))
            {
                return false;
            }

            if (serfs[Serf.Type.Generic] == serf.Index)
            {
                serfs[Serf.Type.Generic] = 0;
            }

            --genericCount;

            if (ResourcesNeededForSpecializing[(int)type * 2] != Resource.Type.None)
            {
                --resources[ResourcesNeededForSpecializing[(int)type * 2]];
            }

            if (ResourcesNeededForSpecializing[(int)type * 2 + 1] != Resource.Type.None)
            {
                --resources[ResourcesNeededForSpecializing[(int)type * 2 + 1]];
            }

            serf.SetSerfType(type);

            serfs[type] = serf.Index;

            return true;
        }

        public Serf SpecializeFreeSerf(Serf.Type type)
        {
            if (serfs[Serf.Type.Generic] == 0)
            {
                return null;
            }

            Serf serf = Game.GetSerf(serfs[Serf.Type.Generic]);

            if (!SpecializeSerf(serf, type))
            {
                return null;
            }

            return serf;
        }

        public uint SerfPotentialCount(Serf.Type type)
        {
            uint count = genericCount;

            if (ResourcesNeededForSpecializing[(int)type * 2] != Resource.Type.None)
            {
                count = Math.Min(count, (uint)resources[ResourcesNeededForSpecializing[(int)type * 2]]);
            }

            if (ResourcesNeededForSpecializing[(int)type * 2 + 1] != Resource.Type.None)
            {
                count = Math.Min(count, (uint)resources[ResourcesNeededForSpecializing[(int)type * 2 + 1]]);
            }

            return count;
        }

        public void SerfIdleInStock(Serf serf)
        {
            serfs[serf.GetSerfType()] = serf.Index;
        }

        public void KnightTraining(Serf serf, int p)
        {
            Serf.Type oldType = serf.GetSerfType();

            if (serf.TrainKnight(p))
                serfs[oldType] = 0;

            SerfIdleInStock(serf);
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            Player = reader.ReadByte();  // 0
            resourceDir = reader.ReadByte();  // 1
            flag = reader.ReadWord(); // 2
            building = reader.ReadWord(); // 4

            for (int j = 0; j < 26; ++j)
            {
                resources[(Resource.Type)j] = reader.ReadWord(); // 6 + 2*j
            }

            for (int j = 0; j < 2; ++j)
            {
                outQueue[j].Type = (Resource.Type)(reader.ReadByte() - 1); // 58 + j
            }

            for (int j = 0; j < 2; ++j)
            {
                outQueue[j].Dest = reader.ReadWord(); // 60 + 2*j
            }

            genericCount = reader.ReadWord(); // 64

            for (int j = 0; j < 27; ++j)
            {
                serfs[(Serf.Type)j] = reader.ReadWord(); // 66 + 2*j
            }
        }

        public void ReadFrom(SaveReaderText reader)
        {
            Player = reader.Value("player").ReadUInt();
            resourceDir = reader.Value("res_dir").ReadUInt();
            flag = reader.Value("flag").ReadUInt();
            building = reader.Value("building").ReadUInt();

            for (int i = 0; i < 2; ++i)
            {
                outQueue[i].Type = (Resource.Type)reader.Value("queue.type")[i].ReadInt();
                outQueue[i].Dest = reader.Value("queue.dest")[i].ReadUInt();
            }

            genericCount = reader.Value("generic_count").ReadUInt();

            for (int i = 0; i < 26; ++i)
            {
                resources[(Resource.Type)i] = reader.Value("resources")[i].ReadInt();
                serfs[(Serf.Type)i] = reader.Value("serfs")[i].ReadUInt();
            }

            serfs[(Serf.Type)26] = reader.Value("serfs")[26].ReadUInt();
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("player").Write(Player);
            writer.Value("res_dir").Write(resourceDir);
            writer.Value("flag").Write(flag);
            writer.Value("building").Write(building);

            for (int i = 0; i < 2; ++i)
            {
                writer.Value("queue.type").Write((int)outQueue[i].Type);
                writer.Value("queue.dest").Write(outQueue[i].Dest);
            }

            writer.Value("generic_count").Write(genericCount);

            for (int i = 0; i < 26; ++i)
            {
                writer.Value("resources").Write(resources[(Resource.Type)i]);
                writer.Value("serfs").Write(serfs[(Serf.Type)i]);
            }

            writer.Value("serfs").Write(serfs[(Serf.Type)26]);
        }


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    for (int i = 0; i < 2 && outQueue[i].Type != Resource.Type.None; ++i)
                    {
                        Resource.Type res = outQueue[i].Type;
                        uint dest = outQueue[i].Dest;

                        Game.CancelTransportedResource(res, dest);
                        Game.LoseResource(res);
                    }

                    Game.AddGoldTotal(-(int)resources[Resource.Type.GoldBar]);
                    Game.AddGoldTotal(-(int)resources[Resource.Type.GoldOre]);
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}
