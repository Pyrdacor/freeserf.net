/*
 * Paths.cs - Default freeserf paths
 *
 * Copyright (C) 2019 Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net.freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see<http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Freeserf.FileSystem
{
    public static class Paths
    {
        public static readonly string UserConfigPath = "";
        public static string SaveGameFolder { get; private set; } = "";
        public static readonly string GameDataFolder = "";

        /// <summary>
        /// Folders where older versions stored save games. They are only
        /// searched for save games but new save games are not stored there.
        /// </summary>
        public static IReadOnlyList<string> LegacySaveGameFolders => legacySaveGameFolders;
        static readonly List<string> legacySaveGameFolders = new List<string>();

        /// <summary>
        /// Changes the folder for save games. The previous folder is still searched
        /// for save games so that existing ones are not lost. This must be called
        /// before the game store is used.
        /// </summary>
        public static void ChangeSaveGameFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || folder == SaveGameFolder)
                return;

            if (!legacySaveGameFolders.Contains(SaveGameFolder))
                legacySaveGameFolders.Add(SaveGameFolder);

            SaveGameFolder = folder;
        }


        //https://docs.microsoft.com/en-us/dotnet/api/system.platformid?view=netframework-4.8
        //https://stackoverflow.com/questions/5116977/how-to-check-the-os-version-at-runtime-e-g-windows-or-linux-without-using-a-con
        public static bool IsLinux()
        {
            int p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 128);
        }

        public static bool IsWindows()
        {
            int p = (int)Environment.OSVersion.Platform;
            return p <= 3;
        }

        public static bool IsOSX()
        {
            int p = (int)Environment.OSVersion.Platform;
            return p == 6;
        }

        static Paths()
        {
            if (OperatingSystem.IsAndroid())
            {
                // On Android the app has its own private storage directory.
                // It is not writable outside of it, so we use the app's
                // personal files directory for saves and config. As this
                // directory is not accessible without root, the Android app
                // changes the save game folder to the app's external files
                // directory (see ChangeSaveGameFolder).
                string personalFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                SaveGameFolder = Path.Combine(personalFolder, "freeserf");
                UserConfigPath = Path.Combine(SaveGameFolder, "user.cfg");
                SaveGameFolder = Path.Combine(SaveGameFolder, "saves");
                GameDataFolder = personalFolder;
            }
            else if (IsLinux())
            {
                SaveGameFolder = Environment.GetEnvironmentVariable("HOME");
                SaveGameFolder += "/.local/share";
            }
            else if (IsWindows())
            {
                SaveGameFolder = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "Saved Games");
                UserConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "freeserf", "user.cfg");
            }
            else if (IsOSX())
            {
                SaveGameFolder = Environment.GetEnvironmentVariable("HOME");
                SaveGameFolder += "/Library/Application Support";
            }

            if (SaveGameFolder == "")
            {
                throw new Exception("Unknown platform.");
            }

            if (!OperatingSystem.IsAndroid())
            {
                SaveGameFolder += Path.DirectorySeparatorChar.ToString() + "freeserf";

                if (!IsWindows())
                {
                    UserConfigPath = SaveGameFolder + "/user.cfg";
                    SaveGameFolder += "/saves";
                }

                GameDataFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
        }
    }
}
