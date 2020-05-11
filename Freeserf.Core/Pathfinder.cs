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

namespace Freeserf
{
    using System.Collections;
    using MapPos = System.UInt32;

    class SearchNode
    {
        public SearchNode Parent;
        public uint GScore;
        public uint FScore;
        public MapPos Position;
        public Direction Direction;
    }

    class FlagSearchNode
    {
        public FlagSearchNode Parent;
        public uint GScore;
        public uint FScore;
        public Flag Flag;
        public Direction Direction;
    }

    class SimpleSearchNode
    {
        public SimpleSearchNode Parent;
        public uint Distance;
        public uint Cost;
        public MapPos Position;
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

        class FlagSearchNodeComparer : IComparer<FlagSearchNode>
        {
            public int Compare(FlagSearchNode left, FlagSearchNode right)
            {
                return right.FScore.CompareTo(left.FScore);
            }
        }

        class SimpleSearchNodeComparer : IComparer<SimpleSearchNode>
        {
            public int Compare(SimpleSearchNode left, SimpleSearchNode right)
            {
                return (right.Distance + right.Cost).CompareTo(left.Distance + left.Cost);
            }
        }

        public static MapPos FindNearestSpot(Map map, MapPos start, Func<Map, MapPos, bool> searchFunc,
            Func<Map, MapPos, uint> costFunction = null, int maxDistance = int.MaxValue)
        {
            var startTime = DateTime.Now;
            var open = new PriorityQueue<SimpleSearchNode>(new SimpleSearchNodeComparer());
            var visited = new List<MapPos>();

            // Create start node 
            var node = new SimpleSearchNode()
            {
                Position = start,
                Distance = 0,
            };

            open.Push(node);

            while (open.Count != 0)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > 2 * Global.TICK_LENGTH)
                    return Global.INVALID_MAPPOS; // tried too long

                node = open.Pop();

                if (searchFunc(map, node.Position))
                    return node.Position;

                // Put current node on closed list. 
                visited.Insert(0, node.Position);

                if (node.Distance >= maxDistance)
                    continue;

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var newPosition = map.Move(node.Position, direction);
                    var distance = node.Distance + 1;

                    // Check if neighbour was already visited.
                    if (visited.Contains(newPosition))
                        continue;

                    // See if neighbour is already in open list.
                    bool inOpen = false;

                    foreach (var n in open)
                    {
                        if (n.Position == newPosition)
                        {
                            inOpen = true;

                            if (n.Distance > distance)
                            {
                                n.Distance = distance;
                                n.Parent = node;

                                // Move element to the back and heapify
                                open.Push(open.Pop());
                            }

                            break;
                        }
                    }

                    // If not found in the open set, create a new node.
                    if (!inOpen)
                    {
                        var newNode = new SimpleSearchNode();

                        newNode.Position = newPosition;
                        newNode.Distance = distance;
                        newNode.Parent = node;
                        newNode.Cost = (costFunction == null) ? 0u : costFunction(map, newPosition);

                        open.Push(newNode);
                    }
                }
            }

            return Global.INVALID_MAPPOS;
        }

        /// <summary>
        /// Find the shortest path from start to end (using A*) considering that
        /// the walking time for a serf walking in any direction of the path
        /// should be minimized.Returns a malloc'ed array of directions and
        /// the size of this array in length.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="buildingRoad"></param>
        /// <returns></returns>
        public static Road FindShortestPath(Map map, MapPos start, MapPos end, Road buildingRoad = null, int maxLength = int.MaxValue, bool endThere = false)
        {
            if (maxLength < 1)
                return new Road();

            var startTime = DateTime.Now;
            var open = new PriorityQueue<SearchNode>(new SearchNodeComparer());
            var closed = new Dictionary<MapPos, SearchNode>();
            var maxCost = (uint)maxLength * 511u; // 511 is the max road segment cost

            // Create start node 
            var node = new SearchNode()
            {
                Position = end,
                GScore = 0,
                FScore = HeuristicCost(map, start, end)
            };

            open.Push(node);

            while (open.Count != 0)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > 2 * Global.TICK_LENGTH)
                    return new Road(); // tried too long

                node = open.Pop();

                if (node.Position == start)
                {
                    // Construct solution 
                    var solution = new Road();
                    solution.Start(start);

                    solution.Cost = node.GScore;

                    while (node.Parent != null)
                    {
                        var direction = node.Direction;
                        solution.Extend(map, direction.Reverse());
                        node = node.Parent;
                    }

                    return solution;
                }

                // Put current node on closed list. 
                closed.Add(node.Position, node);

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var newPosition = map.Move(node.Position, direction);
                    var cost = ActualCost(map, node.Position, direction);

                    if (node.GScore + cost > maxCost)
                        continue; // exceeded max length / max cost

                    // Check if neighbour is valid. 
                    if (!map.IsRoadSegmentValid(node.Position, direction, endThere && node.Position == end) ||
                        (map.GetObject(newPosition) == Map.Object.Flag && newPosition != start))
                    {
                        continue;
                    }

                    if (buildingRoad != null && buildingRoad.HasPosition(map, newPosition) &&
                        newPosition != end && newPosition != start)
                    {
                        continue;
                    }

                    // Check if neighbour is in closed list. 
                    if (closed.ContainsKey(newPosition))
                        continue;

                    // See if neighbour is already in open list. 
                    bool inOpen = false;

                    foreach (var n in open)
                    {
                        if (n.Position == newPosition)
                        {
                            inOpen = true;

                            if (n.GScore > node.GScore + cost)
                            {
                                n.GScore = node.GScore + cost;
                                n.FScore = n.GScore + HeuristicCost(map, newPosition, start);
                                n.Parent = node;
                                n.Direction = direction;

                                // Move element to the back and heapify
                                open.Push(open.Pop());
                            }

                            break;
                        }
                    }

                    // If not found in the open set, create a new node. 
                    if (!inOpen)
                    {
                        var newNode = new SearchNode();

                        newNode.Position = newPosition;
                        newNode.GScore = node.GScore + cost;
                        newNode.FScore = newNode.GScore + HeuristicCost(map, newPosition, start);
                        newNode.Parent = node;
                        newNode.Direction = direction;

                        open.Push(newNode);
                    }
                }
            }

            return new Road();
        }


        /// <summary>
        /// Find the shortest existing connection between two arbitrary flags.
        /// 
        /// The returned collection contains the directions at each flag on
        /// the path.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static List<Direction> FindShortestRoad(Map map, Flag start, Flag end, out uint totalCost)
        {
            if (start == end)
            {
                totalCost = 0;
                return new List<Direction>();
            }

            totalCost = uint.MaxValue;
            var startTime = DateTime.Now;
            var open = new PriorityQueue<FlagSearchNode>(new FlagSearchNodeComparer());
            var closed = new Dictionary<Flag, FlagSearchNode>();

            // Create start node 
            var node = new FlagSearchNode()
            {
                Flag = end,
                GScore = 0,
                FScore = HeuristicCost(map, start.Position, end.Position)
            };

            open.Push(node);

            while (open.Count != 0)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > 2 * Global.TICK_LENGTH)
                    return null; // tried too long

                node = open.Pop();

                if (node.Flag == start)
                {
                    // Construct solution 
                    var solution = new List<Direction>();

                    totalCost = node.GScore;

                    while (node.Parent != null)
                    {
                        solution.Add(node.Direction.Reverse());
                        node = node.Parent;
                    }

                    return solution;
                }

                // Put current node on closed list. 
                closed.Add(node.Flag, node);

                var cycle = DirectionCycleCW.CreateDefault();

                foreach (var direction in cycle)
                {
                    var newRoad = node.Flag.GetRoad(direction);
                    var newFlag = node.Flag.GetOtherEndFlag(direction);

                    // Check if neighbour is valid. 
                    if (newFlag == null || newRoad == null)
                    {
                        continue;
                    }

                    uint cost = newRoad.Cost;

                    // Check if neighbour is in closed list. 
                    if (closed.ContainsKey(newFlag))
                        continue;

                    // See if neighbour is already in open list. 
                    bool inOpen = false;

                    foreach (var n in open)
                    {
                        if (n.Flag == newFlag)
                        {
                            inOpen = true;

                            if (n.GScore > node.GScore + cost)
                            {
                                n.GScore = node.GScore + cost;
                                n.FScore = n.GScore + HeuristicCost(map, newFlag, start);
                                n.Parent = node;
                                n.Direction = direction;

                                // Move element to the back and heapify
                                open.Push(open.Pop());
                            }

                            break;
                        }
                    }

                    // If not found in the open set, create a new node. 
                    if (!inOpen)
                    {
                        var newNode = new FlagSearchNode();

                        newNode.Flag = newFlag;
                        newNode.GScore = node.GScore + cost;
                        newNode.FScore = newNode.GScore + HeuristicCost(map, newFlag, start);
                        newNode.Parent = node;
                        newNode.Direction = direction;

                        open.Push(newNode);
                    }
                }
            }

            return null;
        }

        static readonly uint[] walkCost = new uint[]
        {
            255u, 319u, 383u, 447u, 511u
        };

        static uint HeuristicCost(Map map, MapPos start, MapPos end)
        {
            // Calculate distance to target. 
            int heightDifference = Math.Abs((int)map.GetHeight(start) - (int)map.GetHeight(end));
            int distance = map.Distance(start, end);

            return (distance > 0) ? (uint)distance * walkCost[heightDifference / distance] : 0u;
        }

        internal static uint ActualCost(Map map, MapPos position, Direction direction)
        {
            var otherPosition = map.Move(position, direction);
            int heightDifference = Math.Abs((int)map.GetHeight(position) - (int)map.GetHeight(otherPosition));

            return walkCost[heightDifference];
        }

        static uint HeuristicCost(Map map, Flag start, Flag end)
        {
            // Calculate distance to target. 
            return (uint)map.Distance(start.Position, end.Position);
        }
    }
}
