/*
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

namespace Freeserf
{
    public class GameManager
    {
        public interface IHandler
        {
            void OnNewGame(Game game);
            void OnEndGame(Game game);
        }

        const int MinTimeForSaveConcern = 10000;
        Game currentGame = null;
        DateTime lastSaveTime = DateTime.MinValue;
        string currentGameSaveFile = null;
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

        public bool StartRandomGame(Render.IRenderView renderView, Audio.IAudioInterface audioInterface, bool aiPlayersOnly)
        {
            return StartGame(new GameInfo(new Random(), aiPlayersOnly), renderView, audioInterface);
        }

        public bool StartGame(GameInfo gameInfo, Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
        {
            var newGame = gameInfo.Instantiate(renderView, audioInterface);

            if (newGame == null)
            {
                return false;
            }

            lastSaveTime = DateTime.MinValue;
            currentGameSaveFile = null;
            SetCurrentGame(newGame);

            return true;
        }

        public Game PrepareMultiplayerGame(GameInfo gameInfo, Render.IRenderView renderView, Audio.IAudioInterface audioInterface)
        {
            var newGame = gameInfo.Instantiate(renderView, audioInterface);

            if (newGame == null)
            {
                return null;
            }

            return newGame;
        }

        public bool StartMultiplayerGame(Game game)
        {
            if (game == null)
            {
                return false;
            }

            lastSaveTime = DateTime.MinValue;
            currentGameSaveFile = null;
            SetCurrentGame(game);

            return true;
        }

        public void CloseGame()
        {
            if (currentGame == null)
                return;

            lastSaveTime = DateTime.MinValue;
            currentGameSaveFile = null;
            SetCurrentGame(null);
        }

        public bool LoadGame(string path, Render.IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer viewer)
        {
            var newGame = new Game(renderView, audioInterface);

            if (!GameStore.Instance.Load(path, newGame))
            {
                return false;
            }

            CloseGame();

            if (newGame.GetPlayer(0).IsAI)
                viewer.ChangeTo(Viewer.Type.LocalSpectator);

            lastSaveTime = DateTime.Now;
            currentGameSaveFile = path;
            SetCurrentGame(newGame);
            newGame.Pause();

            return true;
        }

        public bool SaveCurrentGame(string path = null)
        {
            if (currentGame == null)
                return false;

            if (!string.IsNullOrWhiteSpace(path))
            {
                currentGameSaveFile = path;
            }

            if (string.IsNullOrWhiteSpace(currentGameSaveFile))
            {
                if (!GameStore.Instance.QuickSave("quicksave", currentGame))
                    return false;
            }
            else
            {
                if (!GameStore.Instance.Save(currentGameSaveFile, currentGame))
                    return false;
            }

            lastSaveTime = DateTime.Now;

            return true;
        }

        void SetCurrentGame(Game newGame)
        {
            if (currentGame != null)
            {
                for (int i = handlers.Count - 1; i >= 0; --i)
                {
                    handlers[i].OnEndGame(currentGame);
                }

                // Note: Call this after the handlers are notified so the game and render
                //       data is valid while destroying things like the viewport.
                currentGame.Close();
            }

            currentGame = newGame;

            if (currentGame == null)
            {
                return;
            }

            foreach (var handler in handlers)
            {
                handler.OnNewGame(currentGame);
            }
        }

        public bool NeedSave()
        {
            if (currentGame == null)
                return false;

            return (DateTime.Now - lastSaveTime).TotalMilliseconds >= MinTimeForSaveConcern;
        }

        public string GetCurrentGameSaveFile()
        {
            return currentGameSaveFile;
        }
    }
}
