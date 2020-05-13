/*
 * Freeserf.cs - Main program source.
 *
 * Copyright (C) 2013-2017  Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018       Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Freeserf.Test")]

namespace Freeserf
{
    public static partial class Global
    {
        public static readonly Version Version = Assembly.GetEntryAssembly().GetName().Version;
        public static readonly string VERSION = $"{Assembly.GetEntryAssembly().GetName().Name} v{Version.Major}.{Version.Minor}.{Version.Build}";
        public static readonly string EXTENDED_VERSION = $"{Assembly.GetEntryAssembly().GetName().Name} v{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}";

        // The length between game updates in milliseconds. 
        public const int TICK_LENGTH = 20;
        public const int TICKS_PER_SEC = 1000 / TICK_LENGTH;
        public const int TICKS_PER_MIN = 60 * TICKS_PER_SEC;

        public const int MAX_VIRTUAL_SCREEN_WIDTH = 1920;
        public const int MAX_VIRTUAL_SCREEN_HEIGHT = 1440;

        public class InitInfo
        {
            public CommandLine CommandLine;
            public string DataDirectory;
            public string SaveGame;
            public int ScreenWidth = -1;
            public int ScreenHeight = -1;
            public bool? Fullscreen = null;
            public bool ConsoleWindow = false;
            public bool LogLevelSet = false;
        }

        public static InitInfo Init(string[] args)
        {
            var initInfo = new InitInfo();
            var commandLine = new CommandLine();

            commandLine.AddOption('d', "Set debug output level").AddParameter("NUM", (AutoParseableString s) =>
            {
                s.Retrieve(out int d);

                if (d >= (int)Log.Level.Min && d < (int)Log.Level.Max)
                {
                    Log.SetLevel((Log.Level)d);
                    initInfo.LogLevelSet = true;
                }

                return true;
            });

            commandLine.AddOption('f', "Run in fullscreen mode", () => { initInfo.Fullscreen = true; });

            commandLine.AddOption('g', "Use specified data directory").AddParameter("DATA-PATH", (AutoParseableString s) =>
            {
                s.Retrieve(out initInfo.DataDirectory);

                return true;
            });

            commandLine.AddOption('h', "Show this help text", () =>
            {
                commandLine.ShowHelp();
                Environment.Exit(0);
            });

            commandLine.AddOption('l', "Load saved game").AddParameter("FILE", (AutoParseableString s) =>
            {
                initInfo.SaveGame = s.ReadToEnd();

                return true;
            });

            commandLine.AddOption('r', "Set display resolution (e.g. 800x600)").AddParameter("RES", (AutoParseableString s) =>
            {
                s.Retrieve(out initInfo.ScreenWidth);
                s.Skip(1); // 'x'
                s.Retrieve(out initInfo.ScreenHeight);

                return true;
            });

            commandLine.AddOption('c', "Log to console window", () =>
            {
                initInfo.ConsoleWindow = true;
            });

            // TODO: email configurable
            commandLine.SetComment("Please report bugs to <robert.schneckenhaus@web.de>");

            Log.Info.Write(ErrorSystemType.Application, EXTENDED_VERSION);

            if (!commandLine.Process(args))
            {
                if (args.Length > 0)
                    Log.Warn.Write(ErrorSystemType.Application, "Invalid command line options");

                return new InitInfo(); // default values
            }

            initInfo.CommandLine = commandLine;

            return initInfo;
        }
    }
}
