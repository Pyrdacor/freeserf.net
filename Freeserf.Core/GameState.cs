using System;
using System.IO;
using System.Linq;

namespace Freeserf
{
    using Serialize;
    using static Serialize.StateSerializer;
    using GameTime = UInt32;
    using word = UInt16;
    using dword = UInt32;

    internal class GameState : State
    {
        public const int DEFAULT_GAME_SPEED = 2;

        private word tick = 0;
        private dword constTick = 0;
        private word gameTimeTicksOfSecond = 0;
        private GameTime gameTime = 0; // in seconds
        private word gameSpeed = DEFAULT_GAME_SPEED;
        private Random random = new Random();
        private dword gameStatsCounter = 0;
        private dword historyCounter = 0;
        private word flagSearchCounter = 0;
        private dword goldTotal = 0;
        private dword mapGoldMoraleFactor = 0;        
        private int knightMoraleCounter = 0;
        private int inventoryScheduleCounter = 0;
        private int resourceHistoryIndex = 0;

        public GameState()
        {
            PlayerHistoryIndex.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(PlayerHistoryIndex)); };
            PlayerHistoryCounter.GotDirty += (object sender, EventArgs args) => { MarkPropertyAsDirty(nameof(PlayerHistoryCounter)); };
        }

        public override void ResetDirtyFlag()
        {
            lock (dirtyLock)
            {
                PlayerHistoryIndex.ResetDirtyFlag();
                PlayerHistoryCounter.ResetDirtyFlag();
                ResetDirtyFlagUnlocked();
            }
        }

        /// <summary>
        /// Current game tick (game speed dependent)
        /// </summary>
        [Data]
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
        [Data]
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
        [Data]
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
        [Data]
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
        /// Speed of game.
        /// </summary>
        [Data]
        public word GameSpeed
        {
            get => gameSpeed;
            set
            {
                if (gameSpeed != value)
                {
                    gameSpeed = value;
                    MarkPropertyAsDirty(nameof(GameSpeed));
                }
            }
        }

        /// <summary>
        /// Random number generator.
        /// </summary>
        [Data]
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

        [Data]
        public dword GameStatsCounter
        {
            get => gameStatsCounter;
            set
            {
                if (gameStatsCounter != value)
                {
                    gameStatsCounter = value;
                    MarkPropertyAsDirty(nameof(GameStatsCounter));
                }
            }
        }

        [Data]
        public dword HistoryCounter
        {
            get => historyCounter;
            set
            {
                if (historyCounter != value)
                {
                    historyCounter = value;
                    MarkPropertyAsDirty(nameof(HistoryCounter));
                }
            }
        }

        [Data]
        public word FlagSearchCounter
        {
            get => flagSearchCounter;
            set
            {
                if (flagSearchCounter != value)
                {
                    flagSearchCounter = value;
                    MarkPropertyAsDirty(nameof(FlagSearchCounter));
                }
            }
        }

        [Data]
        public dword GoldTotal
        {
            get => goldTotal;
            set
            {
                if (goldTotal != value)
                {
                    goldTotal = value;
                    MarkPropertyAsDirty(nameof(GoldTotal));
                }
            }
        }

        [Data]
        public dword MapGoldMoraleFactor
        {
            get => mapGoldMoraleFactor;
            set
            {
                if (mapGoldMoraleFactor != value)
                {
                    mapGoldMoraleFactor = value;
                    MarkPropertyAsDirty(nameof(MapGoldMoraleFactor));
                }
            }
        }

        [Data]
        public int KnightMoraleCounter
        {
            get => knightMoraleCounter;
            set
            {
                if (knightMoraleCounter != value)
                {
                    knightMoraleCounter = value;
                    MarkPropertyAsDirty(nameof(KnightMoraleCounter));
                }
            }
        }

        [Data]
        public int InventoryScheduleCounter
        {
            get => inventoryScheduleCounter;
            set
            {
                if (inventoryScheduleCounter != value)
                {
                    inventoryScheduleCounter = value;
                    MarkPropertyAsDirty(nameof(InventoryScheduleCounter));
                }
            }
        }

        [Data]
        public int ResourceHistoryIndex
        {
            get => resourceHistoryIndex;
            set
            {
                if (resourceHistoryIndex != value)
                {
                    resourceHistoryIndex = value;
                    MarkPropertyAsDirty(nameof(ResourceHistoryIndex));
                }
            }
        }

        [Data]
        public DirtyArray<int> PlayerHistoryIndex { get; } = new DirtyArray<int>(4);
        [Data]
        public DirtyArray<int> PlayerHistoryCounter { get; } = new DirtyArray<int>(3);
    }

    public class SavedGameState
    {
        // Sync is only done if the game time is a multiple of this in seconds.
        // But based on the default game speed which is 2.
        private const int SyncDelaySeconds = 10;
        private const int SyncDelayFactor = GameState.DEFAULT_GAME_SPEED;
        /// <summary>
        /// Minimum delay between two necessary syncs in seconds.
        /// </summary>
        public const int SyncDelay = SyncDelaySeconds * SyncDelayFactor;

        public static bool TimeToSync(Game game)
        {
            var nextGameTime = game.NextGameTime;
            return game.GameTime != nextGameTime && nextGameTime % SyncDelay == 0;
        }

        readonly Game game;

        internal SavedGameState(Game game)
        {
            this.game = game;
        }

        public static SavedGameState FromGame(Game game)
        {
            var gameCopy = new Game(game.Map);
            GameStateSerializer.DeserializeInto(gameCopy, GameStateSerializer.SerializeFrom(game, true), true, false);
            return new SavedGameState(gameCopy);
        }

        public static SavedGameState UpdateGameAndLastState(Game game, SavedGameState lastState, byte[] updateData, bool full)
        {
            if (!full)
            {
                // Update last state with update.
                GameStateSerializer.DeserializeInto(lastState.game, updateData, true, full);
                // Serialize the updated state.
                var newState = GameStateSerializer.SerializeFrom(lastState.game, true);
                // Replace the current game state with the updated state.
                GameStateSerializer.DeserializeInto(game, newState, false, false);
            }
            else
            {
                GameStateSerializer.DeserializeInto(game, updateData, false, true);
            }

            // Return the new state of the game.
            return FromGame(game);
        }
    }

    /// <summary>
    /// Contains the internal game state
    /// and states for players, inventories,
    /// buildings, flags and serfs.
    /// </summary>
    public static class GameStateSerializer
    {
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
                SerializeWithoutHeader(stream, obj, full);
            }
        }

        static void DeserializeIntoCollection<T>(Stream stream, Collection<T> collection, Action<T> deleteAction, bool dataOnly) where T : class, IGameObject, IState
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
            // be serialized partially only.
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
                DeserializeWithoutHeader(updatedObject, stream);
                updatedObject.ResetDirtyFlag();
            }

            // 4. Run post-serialization for each object
            for (int i = 0; i < objectIndices.Length; ++i)
            {
                var updatedObject = collection.GetOrInsert(objectIndices[i]);
                updatedObject.PostDeserialize(dataOnly);
            }

            // 5. Update free indices again
            collection.UpdateFreeIndices(freeIndices);
        }

        public static void Serialize(Game game, Stream stream, bool full, bool leaveOpen)
        {
            StateSerializer.Serialize(stream, game, full, true);

            // Note: Keep this order!
            SerializeCollection(stream, game.Players, full);
            SerializeCollection(stream, game.Inventories, full);
            SerializeCollection(stream, game.Buildings, full);
            SerializeCollection(stream, game.Flags, full);           
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

        public static void DeserializeInto(Game game, Stream stream, bool leaveOpen, bool dataOnly, bool fullSync)
        {
            if (!dataOnly && !fullSync)
                game.Map?.PrepareDeserialization();
            else if (!dataOnly && fullSync)
            {
                game.Serfs.Clear();
                game.Flags.Clear();
                game.Buildings.Clear();
                game.Inventories.Clear();

                game.ClearVisuals();
            }

            Deserialize(game, stream, true);

            // Note: Keep this order!
            DeserializeIntoCollection(stream, game.Players, (Player player) => throw new ExceptionFreeserf("A player can't be removed from a game."), dataOnly);
            DeserializeIntoCollection(stream, game.Inventories, (Inventory inventory) => game.DeleteInventory(inventory), dataOnly);
            DeserializeIntoCollection(stream, game.Buildings, (Building building) => game.DeleteBuilding(building), dataOnly);
            DeserializeIntoCollection(stream, game.Flags, (Flag flag) => game.DeleteFlag(flag), dataOnly);
            DeserializeIntoCollection(stream, game.Serfs, (Serf serf) => game.DeleteSerf(serf), dataOnly);

            if (!dataOnly)
            {
                game.Map?.UpdateObjectsAfterDeserialization(fullSync);
                game.ResetDirtyFlag();
            }

            if (!leaveOpen)
                stream.Close();
        }

        public static void DeserializeInto(Game game, byte[] data, bool dataOnly, bool fullSync)
        {
            using var stream = new MemoryStream(data);
            stream.Position = 0;
            DeserializeInto(game, stream, true, dataOnly, fullSync);
        }
    }
}
