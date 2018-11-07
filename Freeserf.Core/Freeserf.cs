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

using System;

namespace Freeserf
{
    public static class Freeserf
    {
        public static readonly string VERSION = "v0.1"; // TODO: VERSION_VCS;

        /* The length between game updates in miliseconds. */
        public const int TICK_LENGTH = 20;
        public const int TICKS_PER_SEC = 1000 / TICK_LENGTH;
        
        public static void Run(string[] args)
        {
            string dataDir;
            string saveFile;

            uint screenWidth = 0;
            uint screenHeight = 0;
            bool fullscreen = false;

            var commandLine = new CommandLine();

            commandLine.AddOption('d', "Set Debug output level").AddParameter("NUM", (AutoParseableString s) =>
            {
                s.Retrieve(out int d);

                if (d >= 0 && d < (int)Log.Level.Max)
                {
                    Log.SetLevel((Log.Level)d);
                }

                return true;
            });

            commandLine.AddOption('f', "Run in Fullscreen mode", () => { fullscreen = true; });

            commandLine.AddOption('g', "Use specified data directory").AddParameter("DATA-PATH", (AutoParseableString s) =>
            {
                s.Retrieve(out dataDir);

                return true;
            });

            commandLine.AddOption('h', "Show this help text", () =>
            {
                commandLine.ShowHelp();
                Environment.Exit(0);
            });

            commandLine.AddOption('l', "Load saved game").AddParameter("FILE", (AutoParseableString s) =>
            {
                saveFile = s.ReadToEnd();

                return true;
            });

            commandLine.AddOption('r', "Set display resolution (e.g. 800x600)").AddParameter("RES", (AutoParseableString s) =>
            {
                s.Retrieve(out screenWidth);
                s.Skip(1); // 'x'
                s.Retrieve(out screenHeight);

                return true;
            });

            // TODO: email configurable
            commandLine.SetComment("Please report bugs to <robert.schneckenhaus@web.de>");

            if (!commandLine.Process(args))
            {
                Environment.Exit(1);
                return;
            }

            Log.Info.Write("main", "freeserf.net " + VERSION);

//            Data* data = Data::get_instance();
//            if (!data->load(dataDir))
//            {
//                Log::Error["main"] << "Could not load game data.";
//                return EXIT_FAILURE;
//            }

//            Log::Info["main"] << "Initialize graphics...";

//            Graphics* gfx = nullptr;
//            try
//            {
//                gfx = Graphics::get_instance();
//            }
//            catch (ExceptionFreeserf &e) {
//                Log::Error[e.get_system().c_str()] << e.what();
//                return EXIT_FAILURE;
//            }

//            /* TODO move to right place */
//            Audio* audio = Audio::get_instance();
//            Audio::PPlayer player = audio->get_music_player();
//            if (player)
//            {
//                player->play_track(Audio::TypeMidiTrack0);
//            }

//            GameManager* game_manager = GameManager::get_instance();

//            /* Either load a save game if specified or
//               start a new game. */
//            if (!saveFile.empty())
//            {
//                if (!game_manager->load_game(saveFile))
//                {
//                    return EXIT_FAILURE;
//                }
//            }
//            else
//            {
//                if (!game_manager->start_random_game())
//                {
//                    return EXIT_FAILURE;
//                }
//            }

//            /* Initialize interface */
//            Interface *interface = new Interface();
//  if ((screen_width == 0) || (screen_height == 0)) {
//    gfx->get_resolution(&screen_width, &screen_height);
//    }
//    interface->set_size(screen_width, screen_height);
//    interface->set_displayed(true);

//  if (save_file.empty()) {
//    interface->open_game_init();
//}

///* Init game loop */
//EventLoop* event_loop = EventLoop::get_instance();
//event_loop->add_handler(interface);

//  /* Start game loop */
//  event_loop->run();

//event_loop->del_handler(interface);

//  Log::Info["main"] << "Cleaning up...";

//  /* Clean up */
//  delete interface;
//delete audio;
//delete gfx;
//delete event_loop;
//delete game_manager;

//  return EXIT_SUCCESS;
        }
    }
}
