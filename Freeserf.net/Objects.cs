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
    public class GameObject
    {
        public uint Index { get; protected set; }
        public Game Game { get; protected set; }

        public GameObject(Game game, uint index)
        {
            Game = game;
            Index = index;
        }
    }

    public class Collection : IEnumerable<KeyValuePair<uint, GameObject>>
    {
        Game game;
        uint firstFreeIndex = 0;
        Dictionary<uint, GameObject> objects = new Dictionary<uint, GameObject>();
        SortedSet<uint> freeIndices = new SortedSet<uint>();

        public Collection(Game game = null)
        {
            this.game = game;
        }

        GameObject Allocate()
        {
            GameObject obj;

            if (freeIndices.Count > 0)
            {
                var index = freeIndices.First();

                obj = new GameObject(game, index);
                freeIndices.Remove(index);
            }
            else
            {
                obj = new GameObject(game, firstFreeIndex++);
            }

            objects.Add(obj.Index, obj);

            return obj;
        }

        GameObject GetOrInsert(uint index)
        {
            if (!objects.ContainsKey(index))
                objects.Add(index, new GameObject(game, index));

            return objects[index];
        }

        void Erase(uint index)
        {
            if (objects.ContainsKey(index))
            {
                if (index == firstFreeIndex - 1)
                    --firstFreeIndex;
                else
                    freeIndices.Add(index);

                objects.Remove(index);                
            }
        }

        public IEnumerator<KeyValuePair<uint, GameObject>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<uint, GameObject>>)objects).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<uint, GameObject>>)objects).GetEnumerator();
        }

        public int Size => objects.Count;

        public bool Exists(uint index) => objects.ContainsKey(index);
    }
}
