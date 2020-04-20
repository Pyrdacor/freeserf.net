using System;
using System.IO;
using System.Linq;

namespace Freeserf
{
    using Serialize;
    using static Serialize.StateSerializer;
    using GameTime = UInt32;
    using MapPos = UInt32;
    using word = UInt16;
    using dword = UInt32;
    using Flags = Collection<Flag>;
    using Inventories = Collection<Inventory>;
    using Buildings = Collection<Building>;
    using Serfs = Collection<Serf>;
    using Players = Collection<Player>;

    [DataClass]
    internal class GameState : State
    {
        [Ignore]
        public const int DEFAULT_GAME_SPEED = 2;

        private word tick = 0;
        private dword constTick = 0;
        private word gameTimeTicksOfSecond = 0;
        private GameTime gameTime = 0; // in seconds
        private dword gameSpeed = DEFAULT_GAME_SPEED;
        private Random random = new Random();

        // TODO: add properties for those later
        /*private dword gameStatsCounter;
        private dword historyCounter;
        private word flagSearchCounter;
        private dword goldTotal = 0;
        private dword mapGoldMoraleFactor = 0;        
        private int knightMoraleCounter = 0;
        private int inventoryScheduleCounter = 0;
        // TODO: I'm not sure what the history stuff is actually doing
        private DirtyArray<int> playerHistoryIndex = new DirtyArray<int>(4);
        private DirtyArray<int> playerHistoryCounter = new DirtyArray<int>(3);
        private int resourceHistoryIndex;*/

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
        /// Speed of game.
        /// </summary>
        public dword GameSpeed
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

        // TODO ...
    }

    public class SavedGameState
    {
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
            // Update last state with update.
            GameStateSerializer.DeserializeInto(lastState.game, updateData, true, full);
            // Serialize the updated state.
            var newState = GameStateSerializer.SerializeFrom(lastState.game, true);
            // Replace the current game state with the updated state.
            GameStateSerializer.DeserializeInto(game, newState, false, full);
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
                updatedObject.PostDeserialize(dataOnly);
                updatedObject.ResetDirtyFlag();
            }

            // 4. Update free indices again
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
