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
using Freeserf.Localization;
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Freeserf.Test")]
[assembly: InternalsVisibleTo("FreeserfNet")]
[assembly: InternalsVisibleTo("FreeserfNet.Android")]

namespace Freeserf
{
    public static partial class Global
    {
        public const string APP_NAME = "FreeserfNet";
        // The version is set by the app (e.g. FreeserfNet/AssemblyInfo.cs). The entry assembly
        // may be missing in some environments (e.g. tests), then the core version is used.
        public static readonly Version Version = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version;
        public static readonly string VERSION = $"{APP_NAME} v{Version.Major}.{Version.Minor}.{Version.Build}";
        public static readonly string EXTENDED_VERSION = $"{APP_NAME} v{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}";

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
            public int MonitorIndex = -1;
            public int WindowX = -1;
            public int WindowY = -1;
            public int ScreenWidth = -1;
            public int ScreenHeight = -1;
            public bool? Fullscreen = null;
            public bool ConsoleWindow = false;
            public bool LogLevelSet = false;
            public bool MultiplayerTest = false;
            // Split-screen mode: several local players share one game on one device.
            public bool SplitScreen = false;
            // True if given by the command line (then they are used as they are).
            public bool ScreenSizeGiven = false;
            public bool WindowPositionGiven = false;
        }

        public static InitInfo Init(string[] args)
        {
            var initInfo = new InitInfo();
            var commandLine = new CommandLine();

            int minDebugLevelValue = Enums.MinIntValue<Log.Level>();
            int maxDebugLevelValue = Enums.MaxIntValue<Log.Level>();
            string logTypes = string.Join(", ", Enum.GetNames(typeof(ErrorSystemType)));

            commandLine.AddOption('d', Strings.CommandLine_Debug).AddParameter(Strings.CommandLine_ArgumentNumber, (AutoParseableString s) =>
            {
                s.Retrieve(out int d);

                if (d >= (int)Log.Level.Min && d < (int)Log.Level.Max)
                {
                    Log.SetLevel((Log.Level)d);
                    initInfo.LogLevelSet = true;
                    return true;
                }

                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Strings.CommandLine_InvalidDebugLevel, d, minDebugLevelValue, maxDebugLevelValue));

                return false;
            }, $"{ minDebugLevelValue} ~ { maxDebugLevelValue}");

            commandLine.AddOption('f', Strings.CommandLine_Fullscreen, () => { initInfo.Fullscreen = true; });

            commandLine.AddOption('g', Strings.CommandLine_DataDirectory).AddParameter(Strings.CommandLine_ArgumentDataPath, (AutoParseableString s) =>
            {
                s.Retrieve(out initInfo.DataDirectory);

                return true;
            });

            commandLine.AddOption('h', Strings.CommandLine_Help, () =>
            {
                commandLine.ShowHelp();
                Environment.Exit(0);
            });

            commandLine.AddOption('l', Strings.CommandLine_Load).AddParameter(Strings.CommandLine_ArgumentFile, (AutoParseableString s) =>
            {
                initInfo.SaveGame = s.ReadToEnd();

                return true;
            });

            commandLine.AddOption('r', Strings.CommandLine_Resolution).AddParameter(Strings.CommandLine_ArgumentResolution, (AutoParseableString s) =>
            {
                s.Retrieve(out initInfo.ScreenWidth);
                s.Skip(1); // 'x'
                s.Retrieve(out initInfo.ScreenHeight);
                initInfo.ScreenSizeGiven = true;

                return true;
            });

            commandLine.AddOption('p', Strings.CommandLine_Position).AddParameter(Strings.CommandLine_ArgumentPosition, (AutoParseableString s) =>
            {
                s.Retrieve(out initInfo.WindowX);
                s.Skip(1); // ','
                s.Retrieve(out initInfo.WindowY);
                initInfo.WindowPositionGiven = true;

                return true;
            });

            commandLine.AddOption('m', Strings.CommandLine_MultiplayerTest).AddParameter(Strings.CommandLine_ArgumentMode, (AutoParseableString s) =>
            {
                s.Retrieve(out string mode);

                if (!UI.MultiplayerTestDriver.Configure(mode))
                {
                    Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Strings.CommandLine_InvalidMultiplayerMode, mode));
                    return false;
                }

                initInfo.MultiplayerTest = true;
                return true;
            }, Strings.CommandLine_MultiplayerModes);

            commandLine.AddOption('c', Strings.CommandLine_Console, () =>
            {
                initInfo.ConsoleWindow = true;
            });

            commandLine.AddOption('s', Strings.CommandLine_SplitScreen, () =>
            {
                initInfo.SplitScreen = true;
            });

            commandLine.AddOption('o', Strings.CommandLine_IncludeLogType).AddParameter(Strings.CommandLine_ArgumentLogType, (AutoParseableString s) =>
            {
                s.Retrieve(out string type);

                if (Enum.TryParse(typeof(ErrorSystemType), type, true, out object logTypeObject))
                {
                    var logType = (ErrorSystemType)logTypeObject;
                    Log.SetLogFilter((ErrorSystemType type, string _) => type == logType);
                    return true;
                }

                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Strings.CommandLine_InvalidLogType, type, logTypes));

                return false;
            }, $"{logTypes}");

            commandLine.AddOption('x', Strings.CommandLine_ExcludeLogType).AddParameter(Strings.CommandLine_ArgumentLogType, (AutoParseableString s) =>
            {
                s.Retrieve(out string type);

                if (Enum.TryParse(typeof(ErrorSystemType), type, true, out object logTypeObject))
                {
                    var logType = (ErrorSystemType)logTypeObject;
                    Log.SetLogFilter((ErrorSystemType type, string _) => type != logType);
                    return true;
                }

                Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Strings.CommandLine_InvalidLogType, type, logTypes));

                return false;
            }, $"{logTypes}");

            // TODO: email configurable
            commandLine.SetComment(Strings.CommandLine_ReportBugs);

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
