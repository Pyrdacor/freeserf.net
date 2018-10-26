using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
