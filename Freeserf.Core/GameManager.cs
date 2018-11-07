﻿/*
 * GameManager.cs - Gameplay related functions
 *
 * Copyright (C) 2017  Wicked_Digger <wicked_digger@mail.ru>
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
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class GameManager
    {
        public interface IHandler
        {
            void OnNewGame(Game game);
            void OnEndGame(Game game);
        }

        Game currentGame = null;
        readonly List<IHandler> handlers = new List<IHandler>();

        static GameManager instance = null;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameManager();

                return instance;
            }
        }

        GameManager()
        {

        }

        public void AddHandler(IHandler handler)
        {
            handlers.Add(handler);
        }

        public void DeleteHandler(IHandler handler)
        {
            handlers.Remove(handler);
        }

        public Game GetCurrentGame()
        {
            return currentGame;
        }

        public bool StartRandomGame()
        {
            return StartGame(new GameInfo(new Random()));
        }

        public bool StartGame(GameInfo gameInfo)
        {
            Game newGame = gameInfo.Instantiate();

            if (newGame == null)
            {
                return false;
            }

            SetCurrentGame(newGame);

            return true;
        }

        public bool LoadGame(string path)
        {
            Game newGame = new Game();

            if (!GameStore.Instance.Load(path, newGame))
            {
                return false;
            }

            SetCurrentGame(newGame);
            newGame.Pause();

            return true;
        }

        void SetCurrentGame(Game newGame)
        {
            if (currentGame != null)
            {
                foreach (IHandler handler in handlers)
                {
                    handler.OnEndGame(currentGame);
                }
            }

            currentGame = newGame;

            if (currentGame == null)
            {
                return;
            }

            foreach (IHandler handler in handlers)
            {
                handler.OnNewGame(currentGame);
            }
        }
    }
}