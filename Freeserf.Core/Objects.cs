/*
 * Objects.cs - Game objects collection template
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf
{
    internal interface IGameObject
    {
        uint Index { get; }
        Game Game { get; }
        /// <summary>
        /// This is called after the game object has been
        /// deserialized.
        /// </summary>
        /// <param name="onlyData">If true the game object is only deserialized for data storage (i.e. as the last game state backup). Otherwise this is the real game state (with visuals etc).</param>
        void PostDeserialize(bool onlyData);
    }

    internal abstract class GameObject : IGameObject
    {
        public uint Index { get; protected set; }
        public Game Game { get; protected set; }

        public GameObject(Game game, uint index)
        {
            Game = game;
            Index = index;
        }

        public virtual void PostDeserialize(bool onlyData)
        {
            // Override in derived classes if needed.
        }
    }

    static class ObjectFactory<T> where T : class, IGameObject
    {
        static readonly System.Type type = typeof(T);

        static bool IsType<U>()
        {
            return type == typeof(U);
        }

        public static T Create(Game game, uint index)
        {
            if (IsType<Serf>())
                return (T)(object)new Serf(game, index);
            else if (IsType<Building>())
                return (T)(object)new Building(game, index);
            else if (IsType<Flag>())
                return (T)(object)new Flag(game, index);
            else if (IsType<Player>())
                return (T)(object)new Player(game, index);
            else if (IsType<Inventory>())
                return (T)(object)new Inventory(game, index);

            return null;
        }
    }

    internal class Collection<T> : IEnumerable<T> where T : class, IGameObject, Serialize.IState
    {
        readonly object objectsLock = new object();
        internal Game Game { get; }
        uint firstFreeIndex = 0;
        readonly Dictionary<uint, T> objects = new Dictionary<uint, T>();
        internal SortedSet<uint> FreeIndices { get; } = new SortedSet<uint>();

        public Collection(Game game)
        {
            Game = game;
        }

        // used by state deserialization
        internal void UpdateFreeIndices(uint[] freeIndices)
        {
            FreeIndices.Clear();

            foreach (var freeIndex in freeIndices)
                FreeIndices.Add(freeIndex);
        }

        public T Allocate()
        {
            T obj;

            if (FreeIndices.Count > 0)
            {
                var index = FreeIndices.First();

                obj = ObjectFactory<T>.Create(Game, index);
                FreeIndices.Remove(index);
            }
            else
            {
                obj = ObjectFactory<T>.Create(Game, firstFreeIndex++);
            }

            lock (objectsLock)
            {
                objects.Add(obj.Index, obj);
            }

            return obj;
        }

        public T GetOrInsert(uint index)
        {
            if (!objects.ContainsKey(index))
            {
                lock (objectsLock)
                {
                    objects.Add(index, ObjectFactory<T>.Create(Game, index));
                }

                if (FreeIndices.Contains(index))
                    FreeIndices.Remove(index);

                if (index >= firstFreeIndex)
                {
                    for (uint i = firstFreeIndex; i < index; ++i)
                        FreeIndices.Add(i);

                    firstFreeIndex = index + 1;
                }
            }

            return objects[index];
        }

        public T this[uint index]
        {
            get
            {
                lock (objectsLock)
                {
                    if (!objects.ContainsKey(index))
                        return null;

                    return objects[index];
                }
            }
        }

        public void Erase(uint index)
        {
            if (objects.ContainsKey(index))
            {
                if (index == firstFreeIndex - 1)
                    --firstFreeIndex;
                else
                    FreeIndices.Add(index);

                lock (objectsLock)
                {
                    objects.Remove(index);
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (objectsLock)
            {
                foreach (var entry in objects)
                    yield return entry.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<T> ToList()
        {
            lock (objectsLock)
            {
                return new List<T>(objects.Values);
            }
        }

        public T First => (objects.Count == 0) ? null : objects.First().Value;

        public int Size => objects.Count;

        public bool Exists(uint index) => objects.ContainsKey(index);
    }
}
