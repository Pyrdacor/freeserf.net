/*
 * Inventory.cs - Resources related definitions.
 *
 * Copyright (C) 2015       Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
    using Serialize;
    using ResourceMap = Serialize.DirtyMap<Resource.Type, uint>;
    using word = UInt16;

    public class Inventory : GameObject, IState, IDisposable
    {
        public enum Mode
        {
            In = 0,    // 00
            Stop = 1,  // 01
            Out = 3,   // 11
        }

        internal class OutQueue : IComparable
        {
            public Resource.Type Type;
            public uint Destination;

            public int CompareTo(object other)
            {
                if (other is OutQueue)
                {
                    var otherQueue = other as OutQueue;

                    if (Type == otherQueue.Type)
                        return Destination.CompareTo(otherQueue.Destination);

                    return Type.CompareTo(otherQueue.Type);
                }

                return 1;
            }
        }

        [Data]
        private InventoryState state = new InventoryState();

        // Count of serfs waiting to move out
        uint serfsOut = 0;

        public Inventory(Game game, uint index)
            : base(game, index)
        {
            for (int i = 0; i < 2; ++i)
            {
                state.OutQueue[i].Type = Resource.Type.None;
                state.OutQueue[i].Destination = 0;
            }

            foreach (Serf.Type type in Enum.GetValues(typeof(Serf.Type)))
            {
                state.Serfs.Add(type, 0u);
            }

            for (var resource = Resource.Type.Fish; resource <= Resource.Type.Shield; ++resource)
            {
                state.Resources.Add(resource, 0);
            }
        }

        public bool Dirty => state.Dirty;

        /// <summary>
        /// Inventory owner
        /// </summary>
        public uint Player
        {
            get => state.Player;
            internal set => state.Player = (byte)value;
        }

        public uint Flag
        {
            get => state.Flag;
            set => state.Flag = (word)value;
        }

        public uint Building
        {
            get => state.Building;
            set => state.Building = (word)value;
        }

        public Mode ResourceMode
        {
            get => state.ResourceMode;
            set => state.ResourceMode = value;
        }

        public Mode SerfMode
        {
            get => state.SerfMode;
            set => state.SerfMode = value;
        }

        public void ResetDirtyFlag()
        {
            state.ResetDirtyFlag();
        }

        public bool HasAnyOutMode()
        {
            return state.HasAnyOutMode();
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
            var serfType = serf.SerfType;

            if (state.Serfs[serfType] != serf.Index)
            {
                return false;
            }

            state.Serfs[serfType] = 0;

            if (serfType == Serf.Type.Generic)
            {
                --state.GenericCount;
            }

            ++serfsOut;

            return true;
        }

        public Serf CallOutSerf(Serf.Type type)
        {
            if (state.Serfs[type] == 0)
            {
                return null;
            }

            var serf = Game.GetSerf(state.Serfs[type]);

            if (!CallOutSerf(serf))
            {
                return null;
            }

            return serf;
        }

        public bool CallInternal(Serf serf)
        {
            if (state.Serfs[serf.SerfType] != serf.Index)
            {
                return false;
            }

            state.Serfs[serf.SerfType] = 0;

            return true;
        }

        public Serf CallInternal(Serf.Type type)
        {
            if (state.Serfs[type] == 0)
            {
                return null;
            }

            var serf = Game.GetSerf(state.Serfs[type]);
            state.Serfs[type] = 0;

            return serf;
        }

        public void SerfComeBack()
        {
            ++state.GenericCount;
        }

        public uint FreeSerfCount()
        {
            return state.GenericCount;
        }

        public bool HasSerf(Serf.Type type)
        {
            return state.Serfs[type] != 0;
        }

        public uint GetCountOf(Resource.Type resource)
        {
            return state.Resources[resource];
        }

        public ResourceMap GetAllResources()
        {
            return state.Resources;
        }

        public void PopResource(Resource.Type resource)
        {
            --state.Resources[resource];
        }

        public void PushResource(Resource.Type resource)
        {
            state.Resources[resource] += (state.Resources[resource] < 50000) ? 1u : 0u;
        }

        public bool HasResourceInQueue()
        {
            return (state.OutQueue[0].Type != Resource.Type.None);
        }

        public bool IsQueueFull()
        {
            return (state.OutQueue[1].Type != Resource.Type.None);
        }

        public void GetResourceFromQueue(ref Resource.Type resource, ref uint destination)
        {
            resource = state.OutQueue[0].Type;
            destination = state.OutQueue[0].Destination;

            state.OutQueue[0].Type = state.OutQueue[1].Type;
            state.OutQueue[0].Destination = state.OutQueue[1].Destination;

            state.OutQueue[1].Type = Resource.Type.None;
            state.OutQueue[1].Destination = 0;
        }

        public void ResetQueueForDest(Flag flag)
        {
            if (state.OutQueue[1].Type != Resource.Type.None && state.OutQueue[1].Destination == flag.Index)
            {
                PushResource(state.OutQueue[1].Type);
                state.OutQueue[1].Type = Resource.Type.None;
            }

            if (state.OutQueue[0].Type != Resource.Type.None && state.OutQueue[0].Destination == flag.Index)
            {
                PushResource(state.OutQueue[0].Type);
                state.OutQueue[0].Type = state.OutQueue[1].Type;
                state.OutQueue[0].Destination = state.OutQueue[1].Destination;
                state.OutQueue[1].Type = Resource.Type.None;
            }
        }

        public bool HasFood()
        {
            return state.Resources[Resource.Type.Fish] != 0 ||
                   state.Resources[Resource.Type.Meat] != 0 ||
                   state.Resources[Resource.Type.Bread] != 0;
        }

        // Take resource from inventory and put in out queue.
        // The resource must be present.
        public void AddToQueue(Resource.Type type, uint destination)
        {
            if (type == Resource.Type.GroupFood)
            {
                // Select the food resource with highest amount available
                if (state.Resources[Resource.Type.Meat] > state.Resources[Resource.Type.Bread])
                {
                    if (state.Resources[Resource.Type.Meat] > state.Resources[Resource.Type.Fish])
                    {
                        type = Resource.Type.Meat;
                    }
                    else
                    {
                        type = Resource.Type.Fish;
                    }
                }
                else if (state.Resources[Resource.Type.Bread] > state.Resources[Resource.Type.Fish])
                {
                    type = Resource.Type.Bread;
                }
                else
                {
                    type = Resource.Type.Fish;
                }
            }

            if (state.Resources[type] == 0)
            {
                throw new ExceptionFreeserf(Game, ErrorSystemType.Inventory, "No state.Resource with type.");
            }

            --state.Resources[type];

            if (state.OutQueue[0].Type == Resource.Type.None)
            {
                state.OutQueue[0].Type = type;
                state.OutQueue[0].Destination = destination;
            }
            else
            {
                state.OutQueue[1].Type = type;
                state.OutQueue[1].Destination = destination;
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

        // Create initial resources
        public void ApplySuppliesPreset(uint supplies)
        {
            IEnumerable<uint> template1;
            IEnumerable<uint> template2;

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

            for (int i = 0; i < 26; ++i)
            {
                uint t1 = template1Array[i];
                uint n = (template2Array[i] - t1) * supplies * 6554;

                if (n >= 0x8000)
                    ++t1;

                state.Resources[(Resource.Type)i] = t1 + (n >> 16);
            }
        }

        public Serf CallTransporter(bool water)
        {
            Serf serf;

            if (water)
            {
                if (state.Serfs[Serf.Type.Sailor] != 0)
                {
                    serf = Game.GetSerf(state.Serfs[Serf.Type.Sailor]);
                    state.Serfs[Serf.Type.Sailor] = 0;
                }
                else
                {
                    if ((state.Serfs[Serf.Type.Generic] != 0) &&
                        (state.Resources[Resource.Type.Boat] > 0))
                    {
                        serf = Game.GetSerf(state.Serfs[Serf.Type.Generic]);
                        state.Serfs[Serf.Type.Generic] = 0;
                        --state.Resources[Resource.Type.Boat];
                        serf.SerfType = Serf.Type.Sailor;
                        --state.GenericCount;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (state.Serfs[Serf.Type.Transporter] != 0)
                {
                    serf = Game.GetSerf(state.Serfs[Serf.Type.Transporter]);
                    state.Serfs[Serf.Type.Transporter] = 0;
                }
                else
                {
                    if (state.Serfs[Serf.Type.Generic] != 0)
                    {
                        serf = Game.GetSerf(state.Serfs[Serf.Type.Generic]);
                        state.Serfs[Serf.Type.Generic] = 0;
                        serf.SerfType = Serf.Type.Transporter;
                        --state.GenericCount;
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
            if (serf.SerfType != Serf.Type.Generic)
            {
                return false;
            }

            if (state.Resources[Resource.Type.Sword] == 0 ||
                state.Resources[Resource.Type.Shield] == 0)
            {
                return false;
            }

            PopResource(Resource.Type.Sword);
            PopResource(Resource.Type.Shield);
            --state.GenericCount;
            state.Serfs[Serf.Type.Generic] = 0;

            serf.SerfType = Serf.Type.Knight0;

            return true;
        }


        public Serf SpawnSerfGeneric()
        {
            var serf = Game.GetPlayer(Player).SpawnSerfGeneric();

            if (serf != null)
            {
                serf.InitGeneric(this);

                ++state.GenericCount;

                if (state.Serfs[Serf.Type.Generic] == 0)
                {
                    state.Serfs[Serf.Type.Generic] = serf.Index;
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
            if (serf.SerfType != Serf.Type.Generic)
            {
                return false;
            }

            if (state.Serfs[type] != 0)
            {
                return false;
            }

            if ((ResourcesNeededForSpecializing[(int)type * 2] != Resource.Type.None)
                && (state.Resources[ResourcesNeededForSpecializing[(int)type * 2]] == 0))
            {
                return false;
            }

            if ((ResourcesNeededForSpecializing[(int)type * 2 + 1] != Resource.Type.None)
                && (state.Resources[ResourcesNeededForSpecializing[(int)type * 2 + 1]] == 0))
            {
                return false;
            }

            if (state.Serfs[Serf.Type.Generic] == serf.Index)
            {
                state.Serfs[Serf.Type.Generic] = 0;
            }

            --state.GenericCount;

            if (ResourcesNeededForSpecializing[(int)type * 2] != Resource.Type.None)
            {
                --state.Resources[ResourcesNeededForSpecializing[(int)type * 2]];
            }

            if (ResourcesNeededForSpecializing[(int)type * 2 + 1] != Resource.Type.None)
            {
                --state.Resources[ResourcesNeededForSpecializing[(int)type * 2 + 1]];
            }

            serf.SerfType = type;

            state.Serfs[type] = serf.Index;

            return true;
        }

        public Serf SpecializeFreeSerf(Serf.Type type)
        {
            if (state.Serfs[Serf.Type.Generic] == 0)
            {
                return null;
            }

            var serf = Game.GetSerf(state.Serfs[Serf.Type.Generic]);

            if (!SpecializeSerf(serf, type))
            {
                return null;
            }

            return serf;
        }

        public uint SerfPotentialCount(Serf.Type type)
        {
            uint count = state.GenericCount;

            if (ResourcesNeededForSpecializing[(int)type * 2] != Resource.Type.None)
            {
                count = Math.Min(count, state.Resources[ResourcesNeededForSpecializing[(int)type * 2]]);
            }

            if (ResourcesNeededForSpecializing[(int)type * 2 + 1] != Resource.Type.None)
            {
                count = Math.Min(count, state.Resources[ResourcesNeededForSpecializing[(int)type * 2 + 1]]);
            }

            return count;
        }

        public void SerfIdleInStock(Serf serf)
        {
            state.Serfs[serf.SerfType] = serf.Index;
        }

        public void KnightTraining(Serf serf, int probability)
        {
            var oldType = serf.SerfType;

            if (serf.TrainKnight(probability))
            {
                state.Serfs[oldType] = 0;
            }

            SerfIdleInStock(serf);
        }

        public void ReadFrom(SaveReaderBinary reader)
        {
            Player = reader.ReadByte();  // 0
            state.ResourceDirection = reader.ReadByte();  // 1
            state.Flag = reader.ReadWord(); // 2
            state.Building = reader.ReadWord(); // 4

            for (int j = 0; j < 26; ++j)
            {
                state.Resources[(Resource.Type)j] = reader.ReadWord(); // 6 + 2*j
            }

            for (int j = 0; j < 2; ++j)
            {
                state.OutQueue[j].Type = (Resource.Type)(reader.ReadByte() - 1); // 58 + j
            }

            for (int j = 0; j < 2; ++j)
            {
                state.OutQueue[j].Destination = reader.ReadWord(); // 60 + 2*j
            }

            state.GenericCount = reader.ReadWord(); // 64

            for (int j = 0; j < 27; ++j)
            {
                state.Serfs[(Serf.Type)j] = reader.ReadWord(); // 66 + 2*j
            }
        }

        public void ReadFrom(SaveReaderText reader)
        {
            state.Player = (byte)reader.Value("player").ReadUInt();
            state.ResourceDirection = (byte)reader.Value("res_dir").ReadUInt();
            state.Flag = (word)reader.Value("flag").ReadUInt();
            state.Building = (word)reader.Value("building").ReadUInt();

            for (int i = 0; i < 2; ++i)
            {
                state.OutQueue[i].Type = (Resource.Type)reader.Value("queue.type")[i].ReadInt();
                state.OutQueue[i].Destination = reader.Value("queue.dest")[i].ReadUInt();
            }

            state.GenericCount = reader.Value("generic_count").ReadUInt();

            for (int i = 0; i < 26; ++i)
            {
                state.Resources[(Resource.Type)i] = reader.Value("resources")[i].ReadUInt();
                state.Serfs[(Serf.Type)i] = reader.Value("serfs")[i].ReadUInt();
            }

            state.Serfs[(Serf.Type)26] = reader.Value("serfs")[26].ReadUInt();
        }

        public void WriteTo(SaveWriterText writer)
        {
            writer.Value("player").Write(state.Player);
            writer.Value("res_dir").Write(state.ResourceDirection);
            writer.Value("flag").Write(state.Flag);
            writer.Value("building").Write(state.Building);

            for (int i = 0; i < 2; ++i)
            {
                writer.Value("queue.type").Write((int)state.OutQueue[i].Type);
                writer.Value("queue.dest").Write(state.OutQueue[i].Destination);
            }

            writer.Value("generic_count").Write(state.GenericCount);

            for (int i = 0; i < 26; ++i)
            {
                writer.Value("resources").Write(state.Resources[(Resource.Type)i]);
                writer.Value("serfs").Write(state.Serfs[(Serf.Type)i]);
            }

            writer.Value("serfs").Write(state.Serfs[(Serf.Type)26]);
        }


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    for (int i = 0; i < 2 && state.OutQueue[i].Type != Resource.Type.None; ++i)
                    {
                        var resource = state.OutQueue[i].Type;
                        var destination = state.OutQueue[i].Destination;

                        Game.CancelTransportedResource(resource, destination);
                        Game.LoseResource(resource);
                    }

                    Game.AddGoldTotal(-(int)state.Resources[Resource.Type.GoldBar]);
                    Game.AddGoldTotal(-(int)state.Resources[Resource.Type.GoldOre]);
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
