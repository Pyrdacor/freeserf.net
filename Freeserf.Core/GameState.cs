using System;
using System.IO;
using System.Linq;

namespace Freeserf
{
    using Serialize;
    using GameTime = UInt32;
    using MapPos = UInt32;
    using word = UInt16;
    using dword = UInt32;

    // TODO: map state
    [DataClass]
    internal class GameState : State
    {
        private word tick = 0;
        private dword constTick = 0;
        private word gameTimeTicksOfSecond = 0;
        private GameTime gameTime = 0; // in seconds
        private Random random = new Random();

        // TODO: add properties for those later
        private dword gameStatsCounter;
        private dword historyCounter;
        private word flagSearchCounter;
        private dword goldTotal = 0;
        private dword mapGoldMoraleFactor = 0;        
        private int knightMoraleCounter = 0;
        private int inventoryScheduleCounter = 0;
        // TODO: I'm not sure what the history stuff is actually doing
        private int[] playerHistoryIndex = new int[4];
        private int[] playerHistoryCounter = new int[3];
        private int resourceHistoryIndex;

        /// <summary>
        /// Current game tick (game speed dependent)
        /// </summary>
        public word Tick
        {
            get => tick;
            set
            {
                if (tick != value)
                {
                    tick = value;
                    MarkPropertyAsDirty(nameof(Tick));
                }
            }
        }
        /// <summary>
        /// Current constant game tick (not game speed dependent)
        /// </summary>
        public dword ConstTick 
        {
            get => constTick;
            set
            {
                if (constTick != value)
                {
                    constTick = value;
                    MarkPropertyAsDirty(nameof(ConstTick));
                }
            }
        }
        /// <summary>
        /// Ticks inside the currently started game time second (may span multiple seconds).
        /// </summary>
        public word GameTimeTicksOfSecond
        {
            get => gameTimeTicksOfSecond;
            set
            {
                if (gameTimeTicksOfSecond != value)
                {
                    gameTimeTicksOfSecond = value;
                    MarkPropertyAsDirty(nameof(GameTimeTicksOfSecond));
                }
            }
        }
        /// <summary>
        /// Current game time in seconds (will serve for games that last ~136 years).
        /// </summary>
        public GameTime GameTime
        {
            get => gameTime;
            set
            {
                if (gameTime != value)
                {
                    gameTime = value;
                    MarkPropertyAsDirty(nameof(gameTime));
                }
            }
        }
        /// <summary>
        /// Random number generator.
        /// </summary>
        public Random Random
        {
            get => random;
            set
            {
                if (random != value)
                {
                    random = value;
                    MarkPropertyAsDirty(nameof(Random));
                }
            }
        }
    }

    /// <summary>
    /// Contains the internal game state
    /// and states for players, inventories,
    /// buildings, flags and serfs.
    /// </summary>
    internal static class GameStateSerializer
    {
        static uint ReadCount(Stream stream)
        {
            ulong count = 0;
            int shift = 0;

            while (true)
            {
                int b = stream.ReadByte();

                if (b == -1)
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Invalid game state data.");

                count |= (((ulong)b & 0x7ful) << shift);

                if (count > uint.MaxValue)
                    throw new ExceptionFreeserf(ErrorSystemType.Data, "Invalid game state data.");

                if ((b & 0x80) == 0)
                    break;

                shift += 7;
            }

            return (uint)count;
        }

        static void WriteCount(Stream stream, uint count)
        {
            do
            {
                if ((count & 0x80) == 0)
                {
                    stream.WriteByte((byte)count);
                    break;
                }
                else
                {
                    stream.WriteByte((byte)(count & 0x7f));
                    count >>= 7;
                }
            } while (count != 0);
        }

        static void SerializeCollection<T>(Stream stream, Collection<T> collection, bool full) where T : class, IGameObject, IState
        {
            WriteCount(stream, (uint)collection.FreeIndices.Count);
            foreach (var freeIndex in collection.FreeIndices)
                WriteCount(stream, freeIndex);

            WriteCount(stream, (uint)collection.Size);
            foreach (var obj in collection)
            {
                WriteCount(stream, obj.Index);
            }
            foreach (var obj in collection)
            {
                StateSerializer.Serialize(stream, obj, full, true);
            }
        }

        static void DeserializeIntoCollection<T>(Stream stream, Collection<T> collection, Action<T> deleteAction) where T : class, IGameObject, IState
        {
            uint freeIndexCount = ReadCount(stream);
            uint[] freeIndices = new uint[freeIndexCount];
            for (uint i = 0; i < freeIndexCount; ++i)
                freeIndices[i] = ReadCount(stream);

            uint collectionSize = ReadCount(stream);
            uint[] objectIndices = new uint[collectionSize];
            for (uint i = 0; i < collectionSize; ++i)
            {
                objectIndices[i] = ReadCount(stream);
            }

            // Note: The collection is always serialized full which means it always
            // contains all objects of the current state. The objects themselves may
            // be serialized partially onle.
            // As the collection reflects always the complete state.
            // - Look for matching objects and update them.
            // - Look for new objects and add them.
            // - Look for no longer existent objects and remove them.
            // DO NOT REPLACE OBJECTS OR FIRST DELETE AND THEN RE-ADD THEM!

            // 1. Remove all objects that no longer exist.
            foreach (var oldObject in collection.ToList())
            {
                if (!objectIndices.Contains(oldObject.Index))
                {
                    deleteAction(oldObject); // this will remove it from the collection
                }                    
            }

            // 2. Update free indices
            collection.UpdateFreeIndices(freeIndices);

            // 3. Insert or update the remaining objects
            for (int i = 0; i < objectIndices.Length; ++i)
            {
                var updatedObject = collection.GetOrInsert(objectIndices[i]);
                StateSerializer.Deserialize(updatedObject, stream, true);
            }

            // 4. Update free indices again
            collection.UpdateFreeIndices(freeIndices);
        }

        public static void Serialize(Game game, Stream stream, bool full, bool leaveOpen)
        {
            StateSerializer.Serialize(stream, game, full, true);

            SerializeCollection(stream, game.Players, full);
            SerializeCollection(stream, game.Flags, full);
            SerializeCollection(stream, game.Inventories, full);
            SerializeCollection(stream, game.Buildings, full);
            SerializeCollection(stream, game.Serfs, full);

            if (!leaveOpen)
                stream.Close();
        }

        public static byte[] SerializeFrom(Game game, bool full)
        {
            using var stream = new MemoryStream();
            Serialize(game, stream, full, true);
            return stream.ToArray();
        }

        public static void DeserializeInto(Game game, Stream stream, bool leaveOpen)
        {
            StateSerializer.Deserialize(game, stream, true);

            DeserializeIntoCollection(stream, game.Players, (Player player) => throw new ExceptionFreeserf("A player can't be removed from a game."));
            DeserializeIntoCollection(stream, game.Flags, (Flag flag) => game.DeleteFlag(flag));
            DeserializeIntoCollection(stream, game.Inventories, (Inventory inventory) => game.DeleteInventory(inventory));
            DeserializeIntoCollection(stream, game.Buildings, (Building building) => game.DeleteBuilding(building));
            DeserializeIntoCollection(stream, game.Serfs, (Serf serf) => game.DeleteSerf(serf));

            if (!leaveOpen)
                stream.Close();
        }

        public static void DeserializeInto(Game game, byte[] data)
        {
            using var stream = new MemoryStream(data);
            stream.Position = 0;
            DeserializeInto(game, stream, true);
        }
    }
}
