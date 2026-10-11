/*
 * MultiplayerSave.cs - Saving and loading of multiplayer games
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Freeserf
{
    /// <summary>
    /// Multiplayer data of a save game. It is stored in the section "multiplayer"
    /// of a regular save game, so the players can get their players back when the
    /// game is loaded as a multiplayer game again.
    /// </summary>
    public class MultiplayerSaveInfo
    {
        const string SectionName = "multiplayer";

        /// <summary>
        /// Session of the game. Clients use it together with their token to identify their player.
        /// </summary>
        public ulong SessionId { get; set; } = 0;
        public string ServerName { get; set; } = "";
        /// <summary>
        /// The players of the clients (the host is always player 0 and not part of it).
        /// </summary>
        public HashSet<uint> HumanPlayers { get; } = new HashSet<uint>();
        /// <summary>
        /// Tokens of the players of the clients.
        /// </summary>
        public Dictionary<uint, ulong> PlayerTokens { get; } = new Dictionary<uint, ulong>();

        internal void WriteTo(SaveWriterText writer)
        {
            var section = writer.AddSection(SectionName, 0);

            section.Value("session").Write(SessionId.ToString(CultureInfo.InvariantCulture));
            section.Value("server_name").Write(ServerName);
            // Note: Commas would split the value into a list.
            section.Value("human_players").Write(string.Join(":", HumanPlayers.OrderBy(player => player)));

            foreach (var playerToken in PlayerTokens.OrderBy(playerToken => playerToken.Key))
                section.Value($"token_{playerToken.Key}").Write(playerToken.Value.ToString(CultureInfo.InvariantCulture));
        }

        static MultiplayerSaveInfo ReadFrom(SaveReaderText reader)
        {
            var info = new MultiplayerSaveInfo
            {
                SessionId = ulong.Parse(reader.Value("session").ReadString(), CultureInfo.InvariantCulture),
                ServerName = reader.HasValue("server_name") ? reader.Value("server_name").ReadString() : ""
            };

            foreach (var player in reader.Value("human_players").ReadString().Split(':', StringSplitOptions.RemoveEmptyEntries))
            {
                uint playerIndex = uint.Parse(player, CultureInfo.InvariantCulture);
                info.HumanPlayers.Add(playerIndex);

                if (reader.HasValue($"token_{playerIndex}"))
                    info.PlayerTokens[playerIndex] = ulong.Parse(reader.Value($"token_{playerIndex}").ReadString(), CultureInfo.InvariantCulture);
            }

            return info;
        }

        /// <summary>
        /// Reads the multiplayer data of a save game. Returns null if it is no multiplayer save game.
        /// </summary>
        public static MultiplayerSaveInfo Read(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;

                using var stream = File.OpenRead(path);
                using var reader = new StreamReader(stream, Encoding.ASCII);
                var sections = new SaveReaderTextFile(reader).GetSections(SectionName);

                if (sections == null || sections.Count == 0 || sections[0] == null)
                    return null;

                return ReadFrom(sections[0]);
            }
            catch (Exception ex)
            {
                Log.Warn.Write(ErrorSystemType.Savegame, $"Unable to read multiplayer data of save game '{path}': {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Saves multiplayer games on the server.
    /// </summary>
    public static class MultiplayerSaves
    {
        /// <summary>
        /// Creates the game of a loaded multiplayer game in the same way as the clients do:
        /// A new game is created from the game info and the full game state of the loaded
        /// game is applied to it. Only the AI players (which only run on the server) are
        /// read from the save game. Otherwise some data which is not part of the game state
        /// (and created in another way when loading a game) would differ from the clients.
        /// Must be called when the new game is the current game.
        /// </summary>
        public static bool ApplyLoadedGame(Game game, Game loadedGame, string path)
        {
            try
            {
                var gameState = GameStateSerializer.SerializeFrom(loadedGame, true);

                lock (game)
                {
                    GameStateSerializer.DeserializeInto(game, gameState, false, true);
                }

                foreach (var player in game.Players)
                    player.AI = null;

                using var stream = File.OpenRead(path);
                using var reader = new StreamReader(stream, Encoding.ASCII);
                game.ReadAIFrom(new SaveReaderTextFile(reader));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Savegame, "Failed to apply the loaded multiplayer game: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// The server saves the game automatically in this interval.
        /// </summary>
        public static TimeSpan AutosaveInterval { get; set; } = TimeSpan.FromMinutes(10);
        /// <summary>
        /// Number of automatic save games which are kept per server name.
        /// </summary>
        public const int AutosaveCount = 3;
        const string FilePrefix = "mp-";
        const string AutosaveMarker = "-auto-";

        static string GetFilePrefix(string serverName, bool autosave) =>
            FilePrefix + serverName + (autosave ? AutosaveMarker : "-");

        /// <summary>
        /// Saves the game (the multiplayer data is added by the game, see <see cref="Game.MultiplayerSaveInfoProvider"/>).
        /// </summary>
        public static bool Save(Game game, string serverName, bool autosave, out string path)
        {
            string time = DateTime.Now.ToString("yy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            path = Path.Combine(GameStore.Instance.FolderPath, GetFilePrefix(serverName, autosave) + time + ".save");

            bool success;

            try
            {
                success = GameStore.Instance.Save(path, game);
            }
            catch (Exception ex)
            {
                Log.Error.Write(ErrorSystemType.Savegame, "Failed to save multiplayer game: " + ex.Message);
                success = false;
            }

            if (success && autosave)
                DeleteOldAutosaves(serverName);

            return success;
        }

        static void DeleteOldAutosaves(string serverName)
        {
            try
            {
                var autosaves = Directory.GetFiles(GameStore.Instance.FolderPath, GetFilePrefix(serverName, true) + "*.save")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .Skip(AutosaveCount);

                foreach (var autosave in autosaves)
                    File.Delete(autosave);
            }
            catch (Exception ex)
            {
                Log.Warn.Write(ErrorSystemType.Savegame, "Failed to delete old automatic save games: " + ex.Message);
            }
        }
    }
}
