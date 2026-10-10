/*
 * MultiplayerTestDriver.cs - Automates multiplayer games for local testing
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Freeserf.UI
{
    using MapPos = UInt32;

    /// <summary>
    /// Drives the multiplayer menus and performs some scripted user actions
    /// so that a server and several clients can be tested on one machine
    /// without manual input. It is activated by the command line option -m:
    ///
    /// -m host:CLIENTS[:AI]  Create a server, wait for CLIENTS clients, add AI players and start.
    /// -m join:ADDRESS       Join the server at ADDRESS.
    ///
    /// Every player builds a castle, a lumberjack and a road to it through the
    /// regular interface methods (so the same code as for mouse input is used).
    ///
    /// At each game checkpoint (see <see cref="Game.CheckpointInterval"/>) the hash
    /// of the game state is logged as "[SyncCheck]" line. The last hash of a tick
    /// must be equal on all participants. Moreover the links between the game
    /// objects are checked each frame.
    ///
    /// If the environment variable FREESERF_MPTEST_DUMP is set to 1, the members
    /// of all game objects are written to logs/multiplayer/state-*.txt at each
    /// checkpoint. Comparing these files shows what differs between participants.
    /// </summary>
    internal static class MultiplayerTestDriver
    {
        enum Role
        {
            None,
            Host,
            Join
        }

        enum Step
        {
            OpenMenu,
            Lobby,
            WaitForGame,
            BuildCastle,
            BuildLumberjack,
            BuildRoad,
            Done
        }

        const string LogPrefix = "[MPTest] ";

        static Role role = Role.None;
        static int expectedClients = 0;
        static int aiPlayers = 0;
        static string serverAddress = "localhost";
        static Step step = Step.OpenMenu;
        static uint nextActionGameTime = 0;
        static MapPos lumberjackPosition = Global.INVALID_MAPPOS;
        static bool linkErrorLogged = false;
        static readonly bool dumpStates = Environment.GetEnvironmentVariable("FREESERF_MPTEST_DUMP") == "1";

        public static bool Active => role != Role.None;

        public static bool Configure(string mode)
        {
            var parts = mode.Split(':');

            switch (parts[0].ToLower())
            {
                case "host":
                    role = Role.Host;
                    expectedClients = parts.Length > 1 && int.TryParse(parts[1], out int clients) ? clients : 1;
                    aiPlayers = parts.Length > 2 && int.TryParse(parts[2], out int ai) ? ai : 0;
                    break;
                case "join":
                    role = Role.Join;
                    if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                        serverAddress = parts[1];
                    break;
                default:
                    return false;
            }

            Game.CheckpointReached += Game_CheckpointReached;

            return role == Role.Join || (expectedClients >= 1 && expectedClients + aiPlayers < Game.MAX_PLAYER_COUNT);
        }

        /// <summary>
        /// Called each frame before the game is updated (and after network data was processed).
        /// </summary>
        public static void PreUpdate(Gui gui)
        {
            var game = gui.ActiveViewer?.MainInterface?.Game;

            if (Active && game != null && step >= Step.WaitForGame)
                CheckObjectLinks(game);
        }

        /// <summary>
        /// Called each frame after the game was updated.
        /// </summary>
        public static void Update(Gui gui)
        {
            if (!Active)
                return;

            var interf = gui.ActiveViewer?.MainInterface;

            if (interf == null)
                return;

            try
            {
                UpdateMenu(interf);
                UpdateGame(interf);
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Application, LogPrefix + "Error: " + ex);
            }
        }

        static void UpdateMenu(Interface interf)
        {
            var initBox = interf.GameInitBox;

            switch (step)
            {
                case Step.OpenMenu:
                    if (initBox == null || !initBox.Displayed)
                        return;

                    initBox.SelectGameType(GameInitBox.GameType.MultiplayerClient);

                    if (role == Role.Host)
                    {
                        initBox.HandleAction(GameInitBox.Action.CreateServer);

                        for (int i = 0; i < aiPlayers; ++i)
                            initBox.AddAIPlayer();

                        Log.Info.Write(ErrorSystemType.Application, LogPrefix + $"Server created. Waiting for {expectedClients} client(s).");
                    }
                    else
                    {
                        initBox.ServerAddress = serverAddress;
                        initBox.HandleAction(GameInitBox.Action.StartGame);
                        Log.Info.Write(ErrorSystemType.Application, LogPrefix + $"Joining server '{serverAddress}'.");
                    }

                    step = Step.Lobby;
                    break;
                case Step.Lobby:
                    if (role == Role.Host)
                    {
                        // The joined clients must have been added to the lobby (not only connected).
                        if (interf.Server != null && initBox.JoinedClientCount >= expectedClients)
                        {
                            Log.Info.Write(ErrorSystemType.Application, LogPrefix + "All clients joined. Starting game.");
                            initBox.HandleAction(GameInitBox.Action.StartGame);
                            step = Step.WaitForGame;
                        }
                    }
                    else
                    {
                        step = Step.WaitForGame;
                    }
                    break;
            }
        }

        static void UpdateGame(Interface interf)
        {
            var game = interf.Game;

            if (step < Step.WaitForGame || game == null || interf.Player == null || !interf.Ingame)
                return;

            if (interf.Viewer.ViewerType != Viewer.Type.Server && interf.Viewer.ViewerType != Viewer.Type.Client)
                return;

            if (game.GameTime < nextActionGameTime)
                return;

            var player = interf.Player;
            var map = game.Map;

            switch (step)
            {
                case Step.WaitForGame:
                    Log.Info.Write(ErrorSystemType.Application, LogPrefix + $"Game started as player {player.Index} ({interf.Viewer.ViewerType}).");
                    step = Step.BuildCastle;
                    nextActionGameTime = game.GameTime + 2;
                    break;
                case Step.BuildCastle:
                    {
                        if (player.HasCastle)
                        {
                            step = Step.BuildLumberjack;
                            break;
                        }

                        // Each player searches in another quarter of the map.
                        uint column = map.Columns / 4 + (player.Index % 2) * map.Columns / 2;
                        uint row = map.Rows / 4 + (player.Index / 2) * map.Rows / 2;
                        var position = FindSpot(map, map.Position(column, row), pos => game.CanBuildCastle(pos, player));

                        if (position == Global.INVALID_MAPPOS)
                        {
                            Log.Error.Write(ErrorSystemType.Application, LogPrefix + "No castle spot found.");
                            step = Step.Done;
                            break;
                        }

                        interf.UpdateMapCursorPosition(position);
                        interf.BuildCastle();
                        Log.Info.Write(ErrorSystemType.Application, LogPrefix + $"Castle placed at {position}: {player.HasCastle}.");
                        nextActionGameTime = game.GameTime + 5;
                        break;
                    }
                case Step.BuildLumberjack:
                    {
                        var position = FindSpot(map, player.CastlePosition, pos =>
                            pos != player.CastlePosition && game.CanBuildBuilding(pos, Building.Type.Lumberjack, player));

                        if (position == Global.INVALID_MAPPOS)
                        {
                            Log.Error.Write(ErrorSystemType.Application, LogPrefix + "No lumberjack spot found.");
                            step = Step.Done;
                            break;
                        }

                        interf.UpdateMapCursorPosition(position);
                        interf.BuildBuilding(Building.Type.Lumberjack);
                        lumberjackPosition = position;
                        Log.Info.Write(ErrorSystemType.Application, LogPrefix + $"Lumberjack placed at {position}: {map.GetObject(position) != Map.Object.None}.");
                        step = Step.BuildRoad;
                        nextActionGameTime = game.GameTime + 5;
                        break;
                    }
                case Step.BuildRoad:
                    {
                        var start = map.MoveDownRight(lumberjackPosition);
                        var end = map.MoveDownRight(player.CastlePosition);
                        var road = Pathfinder.FindShortestPath(map, start, end);

                        if (road == null || !road.Valid || road.Length == 0)
                        {
                            Log.Error.Write(ErrorSystemType.Application, LogPrefix + "No road found.");
                            step = Step.Done;
                            break;
                        }

                        if (road.StartPosition != start)
                            road = road.Reverse(map);

                        var directions = road.Directions.Reverse().ToArray();
                        int result = -1;

                        interf.UpdateMapCursorPosition(start);
                        interf.BuildRoadBegin();

                        for (int i = 0; i < directions.Length; ++i)
                        {
                            result = interf.BuildRoadSegment(directions[i], i == directions.Length - 1);

                            if (result != 0)
                                break;
                        }

                        Log.Info.Write(ErrorSystemType.Application, LogPrefix + $"Road with {directions.Length} segments built: {result == 1}.");
                        step = Step.Done;
                        break;
                    }
            }
        }

        static MapPos FindSpot(Map map, MapPos center, Func<MapPos, bool> predicate)
        {
            for (uint i = 0; i < 295; ++i) // size of the spiral pattern
            {
                var position = map.PositionAddSpirally(center, i);

                if (predicate(position))
                    return position;
            }

            return Global.INVALID_MAPPOS;
        }

        static void Game_CheckpointReached(Game game)
        {
            if (step < Step.WaitForGame || game != GameManager.Instance.GetCurrentGame())
                return;

            // Note: A tick may be logged several times when a game state update
            // has reset the game to an earlier tick. The last hash is relevant.
            var hash = Convert.ToHexString(GameStateSerializer.ComputeHash(game));
            Log.Info.Write(ErrorSystemType.Application, $"[SyncCheck] tick={game.ConstTick} time={game.GameTime} hash={hash}");

            if (dumpStates)
                DumpState(game);
        }

        static void CheckObjectLinks(Game game)
        {
            if (linkErrorLogged)
                return;

            foreach (var building in game.Buildings)
            {
                if (building.Index == 0u)
                    continue; // placeholder

                var flag = game.GetFlag(building.FlagIndex);

                if (flag == null || flag.Building != building)
                {
                    Log.Error.Write(ErrorSystemType.Application, LogPrefix + $"Building {building.Index} at {building.Position} is not linked to its flag {building.FlagIndex}: " +
                        (flag == null ? "flag missing" : $"flag at {flag.Position}, has building {flag.HasBuilding}, linked building {flag.Building?.Index.ToString() ?? "none"}"));
                    linkErrorLogged = true;
                }
            }

            foreach (var flag in game.Flags)
            {
                if (flag.Index == 0u)
                    continue; // placeholder

                foreach (var direction in AllDirections)
                {
                    if (direction == Direction.UpLeft && flag.HasBuilding)
                        continue;

                    if (flag.HasPath(direction) && flag.GetOtherEndFlag(direction) == null)
                    {
                        Log.Error.Write(ErrorSystemType.Application, LogPrefix + $"Flag {flag.Index} at {flag.Position} has a path in direction {direction} without other end flag (tick {game.ConstTick}).");
                        linkErrorLogged = true;
                    }
                }
            }

            foreach (var inventory in game.Inventories)
            {
                if (inventory.Index == 0u)
                    continue; // placeholder

                foreach (Serf.Type type in Enum.GetValues(typeof(Serf.Type)))
                {
                    uint serfIndex = inventory.GetSerfIndex(type);

                    if (type == Serf.Type.None || serfIndex == 0u)
                        continue;

                    var serf = game.GetSerf(serfIndex);

                    if (serf == null || serf.SerfType != type)
                    {
                        Log.Error.Write(ErrorSystemType.Application, LogPrefix + $"Inventory {inventory.Index} holds serf {serfIndex} as {type} but it is " +
                            (serf == null ? "missing" : $"{serf.SerfType} in state {serf.SerfState}") + $" (tick {game.ConstTick}).");
                        linkErrorLogged = true;
                    }
                }
            }
        }

        static readonly Direction[] AllDirections = new[]
        {
            Direction.Right, Direction.DownRight, Direction.Down, Direction.Left, Direction.UpLeft, Direction.Up
        };

        static string ObjectHash(Serialize.IState state)
        {
            using var stream = new MemoryStream();
            Serialize.StateSerializer.Serialize(stream, state, true, true);
            return Convert.ToHexString(SHA1.HashData(stream.ToArray())).Substring(0, 12);
        }

        /// <summary>
        /// Writes the members of all game objects and map tiles so that a
        /// divergence can be located by comparing the files of the participants.
        /// </summary>
        static void DumpState(Game game)
        {
            var directory = Path.Combine(Environment.CurrentDirectory, "logs", "multiplayer");

            if (!Directory.Exists(directory))
                return;

            const BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            var lines = new List<string>
            {
                "game " + ObjectHash(game),
                "map " + ObjectHash(game.Map)
            };

            void addObjects<T>(string name, IEnumerable<T> objects) where T : IGameObject, Serialize.IState
            {
                foreach (var obj in objects)
                {
                    lines.Add($"{name} {obj.Index} {ObjectHash(obj)}");
                    DumpMembers(lines, name + obj.Index, obj, 0);
                }
            }

            DumpMembers(lines, "game.state", typeof(Game).GetField("state", privateFlags).GetValue(game), 0);
            DumpMembers(lines, "map.updateState", typeof(Map).GetField("updateState", privateFlags).GetValue(game.Map), 0);

            foreach (var tileArrayName in new[] { "landscapeTiles", "gameTiles" })
            {
                var tiles = typeof(Map).GetField(tileArrayName, privateFlags).GetValue(game.Map) as System.Collections.IEnumerable;
                int tileIndex = 0;

                foreach (var tile in tiles)
                {
                    var tileLines = new List<string>();
                    DumpMembers(tileLines, "", tile, 0);
                    lines.Add($"{tileArrayName}[{tileIndex++}] {string.Join(" ", tileLines)}");
                }
            }

            addObjects("player", game.Players);
            addObjects("inventory", game.Inventories);
            addObjects("building", game.Buildings);
            addObjects("flag", game.Flags);
            addObjects("serf", game.Serfs);

            // Runtime links of the flags (not part of the state)
            foreach (var flag in game.Flags)
            {
                var links = AllDirections.Select(d => $"{d}:{flag.GetOtherEndFlag(d)?.Index.ToString() ?? "-"}");
                lines.Add($"links flag{flag.Index} building={flag.Building?.Index.ToString() ?? "-"} {string.Join(" ", links)}");
            }

            File.WriteAllLines(Path.Combine(directory, $"state-{role.ToString().ToLower()}{Environment.ProcessId}-{game.ConstTick}.txt"), lines);
        }

        static void DumpMembers(List<string> lines, string path, object obj, int depth)
        {
            if (obj == null)
            {
                lines.Add(path + " = null");
                return;
            }

            if (depth > 4)
                return;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = obj.GetType();
            var members = type.GetProperties(flags).Where(p => p.GetCustomAttributes(typeof(Serialize.DataAttribute), true).Length != 0).Cast<MemberInfo>()
                .Concat(type.GetFields(flags).Where(f => f.GetCustomAttributes(typeof(Serialize.DataAttribute), true).Length != 0));

            foreach (var member in members)
            {
                object value = member is PropertyInfo property ? property.GetValue(obj) : ((FieldInfo)member).GetValue(obj);
                string memberPath = path + "." + member.Name;

                if (value is Map)
                    continue;
                else if (value is Serialize.IState)
                    DumpMembers(lines, memberPath, value, depth + 1);
                else if (value is System.Collections.IEnumerable enumerable && value is not string)
                    lines.Add(memberPath + " = [" + string.Join(",", enumerable.Cast<object>()) + "]");
                else
                    lines.Add(memberPath + " = " + value);
            }
        }
    }
}
