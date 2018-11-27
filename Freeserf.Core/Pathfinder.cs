/*
 * Pathfinder.cs - Path finder functions
 *
 * Copyright (C) 2012  Jon Lund Steffensen <jonlst@gmail.com>
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

namespace Freeserf
{
    using System.Collections;
    using MapPos = System.UInt32;

    class SearchNode
    {
        public SearchNode Parent;
        public uint GScore;
        public uint FScore;
        public MapPos Pos;
        public Direction Dir;
    }

    class PriorityQueue<T> : IEnumerable<T> where T : class
    {
        IComparer<T> comparer;
        T[] heap;

        public int Count { get; private set; }

        public PriorityQueue() 
            : this(null)
        {
        }

        public PriorityQueue(int capacity) 
            : this(capacity, null)
        {
        }
        public PriorityQueue(IComparer<T> comparer)
            : this(128, comparer)
        {
        }

        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            this.comparer = (comparer == null) ? Comparer<T>.Default : comparer;
            heap = new T[capacity];
        }

        public void Push(T v)
        {
            if (Count >= heap.Length)
                Array.Resize(ref heap, Count * 2);

            heap[Count] = v;

            SiftUp(Count++);
        }

        public T Pop()
        {
            var v = Top();
            heap[0] = heap[--Count];

            if (Count > 0)
                SiftDown(0);

            return v;
        }

        public T Top()
        {
            if (Count > 0)
                return heap[0];

            throw new InvalidOperationException("Heap is empty");
        }

        void SiftUp(int n)
        {
            var v = heap[n];

            for (var n2 = n / 2; n > 0 && comparer.Compare(v, heap[n2]) > 0; n = n2, n2 /= 2)
                heap[n] = heap[n2];

            heap[n] = v;
        }

        void SiftDown(int n)
        {
            var v = heap[n];

            for (var n2 = n * 2; n2 < Count; n = n2, n2 *= 2)
            {
                if (n2 + 1 < Count && comparer.Compare(heap[n2 + 1], heap[n2]) > 0)
                    ++n2;

                if (comparer.Compare(v, heap[n2]) >= 0)
                    break;

                heap[n] = heap[n2];
            }

            heap[n] = v;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return heap[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class Pathfinder
    {
        // A search node is considered less than the other if
        // it has a larger f-score. This means that in the max-heap
        // the lower score will go to the top.
        class SearchNodeComparer : IComparer<SearchNode>
        {
            public int Compare(SearchNode left, SearchNode right)
            {
                return right.FScore.CompareTo(left.FScore);
            }
        }

        /* Find the shortest path from start to end (using A*) considering that
           the walking time for a serf walking in any direction of the path
           should be minimized. Returns a malloc'ed array of directions and
           the size of this array in length. */
        public static Road Map(Map map, MapPos start, MapPos end, Road buildingRoad = null)
        {
            DateTime startTime = DateTime.Now;
            PriorityQueue<SearchNode> open = new PriorityQueue<SearchNode>(new SearchNodeComparer());
            List<SearchNode> closed = new List<SearchNode>();

            /* Create start node */
            SearchNode node = new SearchNode()
            {
                Pos = end,
                GScore = 0,
                FScore = HeuristicCost(map, start, end)
            };

            open.Push(node);

            while (open.Count != 0)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > 2 * Global.TICK_LENGTH)
                    return new Road(); // tried too long

                node = open.Pop();

                if (node.Pos == start)
                {
                    /* Construct solution */
                    Road solution = new Road();
                    solution.Start(start);

                    while (node.Parent != null)
                    {
                        Direction dir = node.Dir;
                        solution.Extend(dir.Reverse());
                        node = node.Parent;
                    }

                    return solution;
                }

                /* Put current node on closed list. */
                closed.Insert(0, node);

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (Direction d in cycle)
                {
                    MapPos newPos = map.Move(node.Pos, d);
                    uint cost = ActualCost(map, node.Pos, d);

                    /* Check if neighbour is valid. */
                    if (!map.IsRoadSegmentValid(node.Pos, d) ||
                        (map.GetObject(newPos) == Freeserf.Map.Object.Flag && newPos != start))
                    {
                        continue;
                    }

                    if ((buildingRoad != null) && buildingRoad.HasPos(map, newPos) &&
                        (newPos != end) && (newPos != start))
                    {
                        continue;
                    }

                    /* Check if neighbour is in closed list. */
                    if (closed.Any(n => n.Pos == newPos))
                        continue;

                    /* See if neighbour is already in open list. */
                    bool inOpen = false;
                    
                    foreach (var n in open)
                    {
                        if (n.Pos == newPos)
                        {
                            inOpen = true;

                            if (n.GScore >= node.GScore + cost)
                            {
                                n.GScore = node.GScore + cost;
                                n.FScore = n.GScore + HeuristicCost(map, newPos, start);
                                n.Parent = node;
                                n.Dir = d;

                                // Move element to the back and heapify
                                open.Push(open.Pop());
                            }

                            break;
                        }
                    }

                    /* If not found in the open set, create a new node. */
                    if (!inOpen)
                    {
                        SearchNode newNode = new SearchNode();

                        newNode.Pos = newPos;
                        newNode.GScore = node.GScore + cost;
                        newNode.FScore = newNode.GScore + HeuristicCost(map, newPos, start);
                        newNode.Parent = node;
                        newNode.Dir = d;

                        open.Push(newNode);
                    }
                }
            }

            return new Road();
        }

        static readonly uint[] walkCost = new uint[]
        {
            255u, 319u, 383u, 447u, 511u
        };

        static uint HeuristicCost(Map map, MapPos start, MapPos end)
        {
            /* Calculate distance to target. */
            int distColumn = map.DistX(start, end);
            int distRow = map.DistY(start, end);

            int hDiff = Math.Abs((int)map.GetHeight(start) - (int)map.GetHeight(end));
            int dist = 0;

            if ((distColumn > 0 && distRow > 0) || (distColumn < 0 && distRow < 0))
            {
                dist = Math.Max(Math.Abs(distColumn), Math.Abs(distRow));
            }
            else
            {
                dist = Math.Abs(distColumn) + Math.Abs(distRow);
            }

            return (dist > 0) ? (uint)dist * walkCost[hDiff / dist] : 0u;
        }

        static uint ActualCost(Map map, MapPos pos, Direction dir)
        {
            MapPos otherPos = map.Move(pos, dir);

            int hDiff = Math.Abs((int)map.GetHeight(pos) - (int)map.GetHeight(otherPos));

            return walkCost[hDiff];
        }
    }
}
