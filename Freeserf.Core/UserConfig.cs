/*
 * UserConfig.cs - User settings storage
 *
 * Copyright (C) 2019-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Freeserf
{
    public static class UserConfig
    {
        // Game
        const Option DefaultOptions = Option.MessagesImportant | Option.MessagesAll | Option.MessagesMost | Option.MessagesFew;
        const DataSourceMixed.DataUsage DefaultGraphicDataUsage = DataSourceMixed.DataUsage.PreferDos;
        const DataSourceMixed.DataUsage DefaultSoundDataUsage = DataSourceMixed.DataUsage.PreferDos;
        const DataSourceMixed.DataUsage DefaultMusicDataUsage = DataSourceMixed.DataUsage.PreferAmiga;
        // Audio
        const bool DefaultMusic = true;
        const bool DefaultSound = true;
        const float DefaultVolume = 1.0f;
        // Video
        public const int DefaultMonitorIndex = 0;
        public const int DefaultWindowX = 20;
        public const int DefaultWindowY = 40;
        const int DefaultResolutionX = 1280;
        const int DefaultResolutionY = 960;
        const bool DefaultFullscreen = false;
        // Logging
#if DEBUG
        public const Log.Level DefaultLogLevel = Log.Level.Verbose;
#else
        public const Log.Level DefaultLogLevel = Log.Level.Error;
#endif
        public const long DefaultMaxLogSize = 10 * 1024 * 1024; // 10 MB
        public const long MinLogSize = 1024;
        public const string DefaultLogFile = "freeserf.log";
        const bool DefaultLogToConsole = false;
        // Multiplayer
        public const string DefaultServerName = "Freeserf";
        public const int MinServerNameLength = 3;
        public const int MaxServerNameLength = 12;

        public static class Game
        {
            public static Option Options { get; set; } = DefaultOptions;
            public static DataSourceMixed.DataUsage GraphicDataUsage { get; set; } = DefaultGraphicDataUsage;
            public static DataSourceMixed.DataUsage SoundDataUsage { get; set; } = DefaultSoundDataUsage;
            public static DataSourceMixed.DataUsage MusicDataUsage { get; set; } = DefaultMusicDataUsage;
            // TODO: language
        }

        public static class Audio
        {
            public static bool Music { get; set; } = DefaultMusic;
            public static bool Sound { get; set; } = DefaultSound;
            public static float Volume { get; set; } = DefaultVolume;
        }

        public static class Video
        {
            public static int MonitorIndex { get; set; } = DefaultMonitorIndex;
            public static int WindowX { get; set; } = DefaultWindowX;
            public static int WindowY { get; set; } = DefaultWindowY;
            public static int ResolutionWidth { get; set; } = DefaultResolutionX;
            public static int ResolutionHeight { get; set; } = DefaultResolutionY;
            public static bool Fullscreen { get; set; } = DefaultFullscreen;
        }

        public static class Logging
        {
            public static Log.Level LogLevel { get; set; } = DefaultLogLevel;
            public static long MaxLogSize { get; set; } = DefaultMaxLogSize;
            public static string LogFileName { get; set; } = DefaultLogFile;
            public static bool LogToConsole { get; set; } = DefaultLogToConsole;
        }

        public static class Multiplayer
        {
            static string serverName = DefaultServerName;

            /// <summary>
            /// Name of the own server which other players see.
            /// </summary>
            public static string ServerName
            {
                get => serverName;
                set => serverName = IsValidServerName(value) ? value : DefaultServerName;
            }

            /// <summary>
            /// Server addresses (IPs or host names) which were entered manually.
            /// They are checked for running servers in addition to the local network.
            /// </summary>
            public static List<string> ServerAddresses { get; } = new List<string>();

            /// <summary>
            /// Server names must have 3 to 12 characters and only contain letters and digits.
            /// </summary>
            public static bool IsValidServerName(string name)
            {
                return name != null &&
                    name.Length >= MinServerNameLength &&
                    name.Length <= MaxServerNameLength &&
                    name.All(IsValidServerNameCharacter);
            }

            public static bool IsValidServerNameCharacter(char c)
            {
                return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            }
        }

        public static bool Load(string filename)
        {
            try
            {
                var configFile = new ConfigFile();

                if (!configFile.Load(filename))
                {
                    Log.Info.Write(ErrorSystemType.Config, "Fallback to default settings.");
                    SetDefaults();
                    
                    try
                    {
                        if (Save(filename))
                        {
                            Log.Info.Write(ErrorSystemType.Config, $"Saved default settings to config file '{filename}'.");
                            return false;
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    Log.Info.Write(ErrorSystemType.Config, $"Failed to save settings to config file '{filename}'.");
                    return false;
                }


                // Game
                const string game = "game";
                if (int.TryParse(configFile.Value(game, "options", ((int)DefaultOptions).ToString()), out int options))
                    Game.Options = (Option)options | Option.MessagesImportant; // MessagesImportant must be always set
                Game.GraphicDataUsage = configFile.Value(game, "graphic_data_usage", DefaultGraphicDataUsage);
                Game.SoundDataUsage = configFile.Value(game, "sound_data_usage", DefaultSoundDataUsage);
                Game.MusicDataUsage = configFile.Value(game, "music_data_usage", DefaultMusicDataUsage);


                // Audio
                const string audio = "audio";
                Audio.Music = configFile.Value(audio, "music", DefaultMusic);
                Audio.Sound = configFile.Value(audio, "sound", DefaultSound);

                if (float.TryParse(configFile.Value(audio, "volume", DefaultVolume.ToString()), NumberStyles.Float, CultureInfo.InvariantCulture, out float volume))
                    Audio.Volume = volume;


                // Video
                const string video = "video";
                if (int.TryParse(configFile.Value(video, "monitor_index", DefaultMonitorIndex.ToString()), out int monitorIndex))
                    Video.MonitorIndex = monitorIndex;
                if (int.TryParse(configFile.Value(video, "window_x", DefaultWindowX.ToString()), out int windowX))
                    Video.WindowX = windowX;
                if (int.TryParse(configFile.Value(video, "window_y", DefaultWindowY.ToString()), out int windowY))
                    Video.WindowY = windowY;
                if (int.TryParse(configFile.Value(video, "resolution_width", DefaultResolutionX.ToString()), out int resolutionWidth))
                    Video.ResolutionWidth = resolutionWidth;
                if (int.TryParse(configFile.Value(video, "resolution_height", DefaultResolutionY.ToString()), out int resolutionHeight))
                    Video.ResolutionHeight = resolutionHeight;
                Video.Fullscreen = configFile.Value(video, "fullscreen", DefaultFullscreen);

                // Logging
                const string logging = "logging";
                Logging.LogLevel = configFile.Value(logging, "level", DefaultLogLevel);
                if (long.TryParse(configFile.Value(logging, "max_log_size", DefaultMaxLogSize.ToString()), out long maxLogSize))
                    Logging.MaxLogSize = maxLogSize;
                Logging.LogFileName = configFile.Value(logging, "log_file", DefaultLogFile);
                Logging.LogToConsole = configFile.Value(logging, "log_to_console", DefaultLogToConsole);

                if (Logging.MaxLogSize < MinLogSize)
                    Logging.MaxLogSize = MinLogSize;

                // Multiplayer
                const string multiplayer = "multiplayer";
                Multiplayer.ServerName = configFile.Value(multiplayer, "server_name", DefaultServerName);
                Multiplayer.ServerAddresses.Clear();
                Multiplayer.ServerAddresses.AddRange(configFile.Value(multiplayer, "server_addresses", "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct());

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(string filename)
        {
            try
            {
                var configFile = new ConfigFile();

                // Game
                const string game = "game";
                configFile.SetValue(game, "options", Game.Options);
                configFile.SetValue(game, "graphic_data_usage", Game.GraphicDataUsage);
                configFile.SetValue(game, "sound_data_usage", Game.SoundDataUsage);
                configFile.SetValue(game, "music_data_usage", Game.MusicDataUsage);

                // Audio
                const string audio = "audio";
                configFile.SetValue(audio, "music", Audio.Music);
                configFile.SetValue(audio, "sound", Audio.Sound);
                configFile.SetValue(audio, "volume", Audio.Volume);

                // Video
                const string video = "video";
                configFile.SetValue(video, "monitor_index", Video.MonitorIndex);
                configFile.SetValue(video, "window_x", Video.WindowX);
                configFile.SetValue(video, "window_y", Video.WindowY);
                configFile.SetValue(video, "resolution_width", Video.ResolutionWidth);
                configFile.SetValue(video, "resolution_height", Video.ResolutionHeight);
                configFile.SetValue(video, "fullscreen", Video.Fullscreen);

                // Logging
                const string logging = "logging";
                configFile.SetValue(logging, "level", Logging.LogLevel);
                configFile.SetValue(logging, "max_log_size", Logging.MaxLogSize);
                configFile.SetValue(logging, "log_file", Logging.LogFileName);
                configFile.SetValue(logging, "log_to_console", Logging.LogToConsole);

                // Multiplayer
                const string multiplayer = "multiplayer";
                configFile.SetValue(multiplayer, "server_name", Multiplayer.ServerName);
                configFile.SetValue(multiplayer, "server_addresses", string.Join(",", Multiplayer.ServerAddresses));

                return configFile.Save(filename);
            }
            catch
            {
                return false;
            }
        }

        public static void SetDefaults()
        {
            Game.Options = DefaultOptions;
            Game.GraphicDataUsage = DefaultGraphicDataUsage;
            Game.SoundDataUsage = DefaultSoundDataUsage;
            Game.MusicDataUsage = DefaultMusicDataUsage;

            Audio.Music = DefaultMusic;
            Audio.Sound = DefaultSound;
            Audio.Volume = DefaultVolume;

            Video.MonitorIndex = DefaultMonitorIndex;
            Video.WindowX = DefaultWindowX;
            Video.WindowY = DefaultWindowY;
            Video.ResolutionWidth = DefaultResolutionX;
            Video.ResolutionHeight = DefaultResolutionY;
            Video.Fullscreen = DefaultFullscreen;

            Logging.LogLevel = DefaultLogLevel;
            Logging.MaxLogSize = DefaultMaxLogSize;
            Logging.LogFileName = DefaultLogFile;
            Logging.LogToConsole = DefaultLogToConsole;

            Multiplayer.ServerName = DefaultServerName;
            Multiplayer.ServerAddresses.Clear();
        }
    }
}
