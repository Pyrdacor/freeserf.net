/*
 * Interface.cs - Top-level GUI interface
 *
 * Copyright (C) 2013  Jon Lund Steffensen <jonlst@gmail.com>
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

/*
 * Note:
 * 
 * The gui positions and sizes are the same as in the original game.
 * The gui layer has builtin transformations for positions and sizes
 * so the rendering will scale the gui in relation to the games virtual
 * screen. The only thing that has to be done is to transform mouse
 * inputs to the original gui locations.
 */

// TODO: transform mouse coordinates to orignal gui coordiantes (see note above)

using System;
using Freeserf.Render;

namespace Freeserf
{
    using MapPos = UInt32;

    internal class Interface : GuiObject, GameManager.IHandler
    {
        // Interval between automatic save games
        const int AUTOSAVE_INTERVAL = 10 * 60 * Freeserf.TICKS_PER_SEC;

        static readonly uint[] MapBuildingSprite = new uint[]
        {
            0, 0xa7, 0xa8, 0xae, 0xa9,
            0xa3, 0xa4, 0xa5, 0xa6,
            0xaa, 0xc0, 0xab, 0x9a, 0x9c, 0x9b, 0xbc,
            0xa2, 0xa0, 0xa1, 0x99, 0x9d, 0x9e, 0x98, 0x9f, 0xb2
        };

        public enum CursorType
        {
            None = 0,
            Flag,
            RemovableFlag,
            Building,
            Path,
            ClearByFlag,
            ClearByPath,
            Clear
        }

        public enum BuildPossibility
        {
            None = 0,
            Flag,
            Mine,
            Small,
            Large,
            Castle
        }

        public struct SpriteLocation
        {
            public int Sprite;
            public int X, Y;
        }

        GameInitBox initBox;

        MapPos mapCursorPos = 0u;
        CursorType mapCursorType = CursorType.None;
        BuildPossibility buildPossibility = BuildPossibility.None;

        uint lastConstTick;

        Road buildingRoad;
        int buildingRoadValidDir;

        int[] sfxQueue = new int[4];

        Player player;
        int config = 0x39;
        int msgFlags;

        SpriteLocation[] mapCursorSprites = new SpriteLocation[7];

        int currentStat8Mode = 0;
        int currentStat7Item = 7;

        int waterInView;
        int treesInView;

        int returnTimeout;
        int returnPos;

        public IRenderView RenderView { get; } = null;
        public Viewport Viewport { get; private set; } = null;
        public PanelBar PanelBar { get; private set; } = null;
        public PopupBox PopupBox { get; private set; } = null;
        public NotificationBox NotificationBox { get; private set; } = null;
        public Game Game { get; private set; } = null;
        public Random Random { get; private set; } = null;
        public TextRenderer TextRenderer { get; } = null;
        public bool Ingame => Game != null && (initBox == null || !initBox.Displayed);

        public Interface(IRenderView renderView)
            : base(renderView)
        {
            RenderView = renderView;

            TextRenderer = new TextRenderer(renderView);

            displayed = true;

            mapCursorSprites[0] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[1] = new SpriteLocation { Sprite = 33 };
            mapCursorSprites[2] = new SpriteLocation { Sprite = 33 };
            mapCursorSprites[3] = new SpriteLocation { Sprite = 33 };
            mapCursorSprites[4] = new SpriteLocation { Sprite = 33 };
            mapCursorSprites[5] = new SpriteLocation { Sprite = 33 };
            mapCursorSprites[6] = new SpriteLocation { Sprite = 33 };

            GameManager.Instance.AddHandler(this);

            SetSize(640, 480); // original size

            Viewport = new Viewport(this, null);

            OpenGameInit();
        }

        protected override void InternalDraw()
        {
            // empty
        }

        public void SetGame(Game game)
        {
            if (Viewport != null)
            {
                DeleteChild(Viewport);
                Viewport = null;
            }

            Game = game;
            player = null;

            if (Game != null)
            {
                Viewport = new Viewport(this, Game.Map);
                Viewport.Displayed = true;
                AddChild(Viewport, 0, 0);

                game.Map.AttachToRenderLayer(RenderView.GetLayer(global::Freeserf.Layer.Landscape), RenderView.DataSource);

                var pos = game.GetPlayer(0u).CastlePos;

                if (pos != Global.BadMapPos)
                {
                    uint column = game.Map.PosColumn(pos);
                    uint row = game.Map.PosRow(pos);

                    game.Map.ScrollTo(column, row);
                }
            }

            Layout();
        }

        public Player.Color GetPlayerColor(uint playerIndex)
        {
            return Game.GetPlayer(playerIndex).GetColor();
        }

        public bool GetConfig(int i)
        {
            return Misc.BitTest(config, i);
        }

        public void SetConfig(int i)
        {
            config |= Misc.Bit(i);
        }

        public void SwitchConfig(int i)
        {
            Misc.BitInvert(config, i);
        }

        public MapPos GetMapCursorPos()
        {
            return mapCursorPos;
        }

        public CursorType GetMapCursorType()
        {
            return mapCursorType;
        }

        public int GetMapCursorSprite(int i)
        {
            return mapCursorSprites[i].Sprite;
        }

        public bool GetMsgFlag(int i)
        {
            return Misc.BitTest(msgFlags, i);
        }

        public void SetMsgFlag(int i)
        {
            msgFlags |= Misc.Bit(i);
        }

        public int GetCurrentStat8Mode()
        {
            return currentStat8Mode;
        }

        public void SetCurrentStat8Mode(int mode)
        {
            currentStat8Mode = mode;
        }

        public int GetCurrentStat7Item()
        {
            return currentStat7Item;
        }

        public void SetCurrentStat7Item(int item)
        {
            currentStat7Item = item;
        }

        public BuildPossibility GetBuildPossibility()
        {
            return buildPossibility;
        }

        /* Open popup box */
        public void OpenPopup(PopupBox.Type box)
        {
            if (PopupBox == null)
            {
                PopupBox = new PopupBox(this);
                AddChild(PopupBox, 0, 0);
            }

            base.Layout();
            PopupBox.Show(box);

            if (PanelBar != null)
            {
                PanelBar.Update();
            }
        }

        /* Close the current popup. */
        public void ClosePopup()
        {
            if (PopupBox == null)
            {
                return;
            }

            PopupBox.Hide();
            DeleteChild(PopupBox);
            PopupBox = null;
            UpdateMapCursorPos(mapCursorPos);
            PanelBar.Update();
        }

        /* Open box for starting a new game */
        public void OpenGameInit()
        {
            RenderView.ResetZoom();

            if (initBox == null)
            {
                initBox = new GameInitBox(this);
                AddChild(initBox, 0, 0);
            }

            initBox.Displayed = true;
            initBox.Enabled = true;

            if (PanelBar != null)
            {
                PanelBar.Displayed = false;
            }

            Viewport.Enabled = false;
            base.Layout();
        }

        public void CloseGameInit()
        {
            if (initBox != null)
            {
                initBox.Displayed = false;
                DeleteChild(initBox);
                initBox = null;
            }

            if (PanelBar != null)
            {
                PanelBar.Displayed = true;
                PanelBar.Enabled = true;
            }

            Viewport.Enabled = true;
            base.Layout();

            UpdateMapCursorPos(mapCursorPos);
        }

        /* Open box for next message in the message queue */
        public void OpenMessage()
        {
            if (!player.HasNotification())
            {
                PlaySound(Audio.TypeSfx.Click);
                return;
            }

            else if (!Misc.BitTest(msgFlags, 3))
            {
                msgFlags |= Misc.Bit(4);
                msgFlags |= Misc.Bit(3);

                MapPos pos = Viewport.GetCurrentMapPos();

                returnPos = (int)pos;
            }

            Message message = player.PopNotification();

            if (message.MessageType == Message.Type.CallToMenu)
            {
                /* TODO */
            }

            if (NotificationBox == null)
            {
                NotificationBox = new NotificationBox(this);
                AddChild(NotificationBox, 0, 0);
            }

            NotificationBox.Show(message);
            base.Layout();

            if (Misc.BitTest(0x8f3fe, (int)message.MessageType))
            {
                /* Move screen to new position */
                Viewport.MoveToMapPos(message.Pos);
                UpdateMapCursorPos(message.Pos);
            }

            msgFlags |= Misc.Bit(1);
            returnTimeout = 60 * Freeserf.TICKS_PER_SEC;
            PlaySound(Audio.TypeSfx.Click);
        }

        public void ReturnFromMessage()
        {
            if (Misc.BitTest(msgFlags, 3))
            {
                /* Return arrow present */
                msgFlags |= Misc.Bit(4);
                msgFlags &= ~Misc.Bit(3);

                returnTimeout = 0;
                Viewport.MoveToMapPos((uint)returnPos);

                if (PopupBox != null && PopupBox.Box == PopupBox.Type.Message)
                {
                    ClosePopup();
                }

                PlaySound(Audio.TypeSfx.Click);
            }
        }

        public void CloseMessage()
        {
            if (NotificationBox == null)
            {
                return;
            }

            NotificationBox.Displayed = false;
            DeleteChild(NotificationBox);
            NotificationBox = null;
            base.Layout();
        }

        public Player GetPlayer()
        {
            return player;
        }

        public void SetPlayer(uint player)
        {
            if (Game == null)
            {
                return;
            }

            if (this.player != null && player == this.player.Index)
            {
                return;
            }

            if (PanelBar != null)
            {
                DeleteChild(PanelBar);
                PanelBar = null;
            }

            this.player = Game.GetPlayer(player);

            /* Move viewport to initial position */
            MapPos initPos = Game.Map.Pos(0, 0);

            if (this.player != null)
            {
                PanelBar = new PanelBar(this);
                PanelBar.Displayed = true;
                AddChild(PanelBar, 0, 0);
                Layout();

                foreach (Building building in Game.GetPlayerBuildings(this.player))
                {
                    if (building.BuildingType == Building.Type.Castle)
                    {
                        initPos = building.Position;
                    }
                }
            }

            UpdateMapCursorPos(initPos);
            Viewport.MoveToMapPos(mapCursorPos);
        }

        public void UpdateMapCursorPos(MapPos pos)
        {
            mapCursorPos = pos;

            if (IsBuildingRoad())
            {
                DetermineMapCursorTypeRoad();
            }
            else
            {
                DetermineMapCursorType();
            }

            UpdateInterface();
        }

        public bool IsBuildingRoad()
        {
            return buildingRoad != null && buildingRoad.Valid;
        }

        public Road GetBuildingRoad()
        {
            return buildingRoad;
        }

        /* Start road construction mode for player interface. */
        public void BuildRoadBegin()
        {
            DetermineMapCursorType();

            if (mapCursorType != CursorType.Flag &&
                mapCursorType != CursorType.RemovableFlag)
            {
                UpdateInterface();
                return;
            }

            buildingRoad.Invalidate();
            buildingRoad.Start(mapCursorPos);
            UpdateMapCursorPos(mapCursorPos);

            PanelBar.Update();
        }

        /* End road construction mode for player interface. */
        public void BuildRoadEnd()
        {
            mapCursorSprites[1].Sprite = 33;
            mapCursorSprites[2].Sprite = 33;
            mapCursorSprites[3].Sprite = 33;
            mapCursorSprites[4].Sprite = 33;
            mapCursorSprites[5].Sprite = 33;
            mapCursorSprites[6].Sprite = 33;

            buildingRoad.Invalidate();
            UpdateMapCursorPos(mapCursorPos);

            PanelBar.Update();
        }

        public void BuildRoadReset()
        {
            BuildRoadEnd();
            BuildRoadBegin();
        }

        /* Build a single road segment. Return -1 on fail, 0 on successful
           construction, and 1 if this segment completed the path. */
        public int BuildRoadSegment(Direction dir)
        {
            if (!buildingRoad.Extendable)
            {
                /* Max length reached */
                return -1;
            }

            buildingRoad.Extend(dir);

            MapPos dest = 0;
            bool water = false;

            int r = Game.CanBuildRoad(buildingRoad, player, ref dest, ref water);

            if (r <= 0)
            {
                /* Invalid construction, undo. */
                return RemoveRoadSegment();
            }

            if (Game.Map.GetObject(dest) == Map.Object.Flag)
            {
                /* Existing flag at destination, try to connect. */
                if (!Game.BuildRoad(buildingRoad, player))
                {
                    BuildRoadEnd();
                    return -1;
                }
                else
                {
                    BuildRoadEnd();
                    UpdateMapCursorPos(dest);
                    return 1;
                }
            }
            else if (Game.Map.Paths(dest) == 0)
            {
                /* No existing paths at destination, build segment. */
                UpdateMapCursorPos(dest);

                /* TODO Pathway scrolling */
            }
            else
            {
                /* TODO fast split path and connect on double click */
                return -1;
            }

            return 0;
        }

        public int RemoveRoadSegment()
        {
            MapPos dest = buildingRoad.Source;
            int res = 0;
            bool water = false;
            buildingRoad.Undo();

            if (buildingRoad.Length == 0 ||
                Game.CanBuildRoad(buildingRoad, player, ref dest, ref water) == 0)
            {
                /* Road construction is no longer valid, abort. */
                BuildRoadEnd();
                res = -1;
            }

            UpdateMapCursorPos(dest);

            /* TODO Pathway scrolling */

            return res;
        }

        /* Extend currently constructed road with an array of directions. */
        public int ExtendRoad(Road road)
        {
            Road oldRoad = buildingRoad;

            foreach (Direction dir in road.Dirs)
            {
                int r = BuildRoadSegment(dir);

                if (r < 0)
                {
                    buildingRoad = oldRoad;
                    return -1;
                }
                else if (r == 1)
                {
                    buildingRoad.Invalidate();
                    return 1;
                }
            }

            return 0;
        }

        public bool BuildRoadIsValidDir(Direction dir)
        {
            return Misc.BitTest(buildingRoadValidDir, (int)dir);
        }

        public void DemolishObject()
        {
            DetermineMapCursorType();

            if (mapCursorType == CursorType.RemovableFlag)
            {
                PlaySound(Audio.TypeSfx.Click);
                Game.DemolishFlag(mapCursorPos, player);
            }
            else if (mapCursorType == CursorType.Building)
            {
                Building building = Game.GetBuildingAtPos(mapCursorPos);

                if (building.IsDone() &&
                    (building.BuildingType == Building.Type.Hut ||
                     building.BuildingType == Building.Type.Tower ||
                     building.BuildingType == Building.Type.Fortress))
                {
                    /* TODO */
                }

                PlaySound(Audio.TypeSfx.Ahhh);
                Game.DemolishBuilding(mapCursorPos, player);
            }
            else
            {
                PlaySound(Audio.TypeSfx.NotAccepted);
                UpdateInterface();
            }
        }

        /* Build new flag. */
        public void BuildFlag()
        {
            if (!Game.BuildFlag(mapCursorPos, player))
            {
                PlaySound(Audio.TypeSfx.NotAccepted);
                return;
            }

            UpdateMapCursorPos(mapCursorPos);
        }

        /* Build a new building. */
        public void BuildBuilding(Building.Type type)
        {
            if (!Game.BuildBuilding(mapCursorPos, type, player))
            {
                PlaySound(Audio.TypeSfx.NotAccepted);
                return;
            }

            PlaySound(Audio.TypeSfx.Accepted);
            ClosePopup();

            /* Move cursor to flag. */
            MapPos flagPos = Game.Map.MoveDownRight(mapCursorPos);
            UpdateMapCursorPos(flagPos);
        }

        /* Build castle. */
        public void BuildCastle()
        {
            if (!Game.BuildCastle(mapCursorPos, player))
            {
                PlaySound(Audio.TypeSfx.NotAccepted);
                return;
            }

            PlaySound(Audio.TypeSfx.Accepted);
            UpdateMapCursorPos(mapCursorPos);
        }

        public void BuildRoad()
        {
            if (!Game.BuildRoad(buildingRoad, player))
            {
                PlaySound(Audio.TypeSfx.NotAccepted);
                Game.DemolishFlag(mapCursorPos, player);
            }
            else
            {
                PlaySound(Audio.TypeSfx.Accepted);
                BuildRoadEnd();
            }
        }

        static readonly int[] MsgCategory = new int[]
        {
            -1, 5, 5, 5, 4, 0, 4, 3, 4, 5,
            5, 5, 4, 4, 4, 4, 0, 0, 0, 0
        };

        /* Called periodically when the game progresses. */
        public void Update()
        {
            if (Game == null)
            {
                return;
            }

            Game.Update();

            int tickDiff = (int)Game.ConstTick - (int)lastConstTick;
            lastConstTick = Game.ConstTick;

            /* Clear return arrow after a timeout */
            if (returnTimeout < tickDiff)
            {
                msgFlags |= Misc.Bit(4);
                msgFlags &= ~Misc.Bit(3);
                returnTimeout = 0;
            }
            else
            {
                returnTimeout -= tickDiff;
            }

            /* Handle newly enqueued messages */
            if (player != null && player.HasMessage())
            {
                player.DropMessage();

                while (player.HasNotification())
                {
                    Message message = player.PeekNotification();

                    if (Misc.BitTest(config, MsgCategory[(int)message.MessageType]))
                    {
                        PlaySound(Audio.TypeSfx.Message);
                        msgFlags |= Misc.Bit(0);
                        break;
                    }

                    player.PopNotification();
                }
            }

            if (player != null && Misc.BitTest(msgFlags, 1))
            {
                msgFlags &= ~Misc.Bit(1);

                while (true)
                {
                    if (!player.HasNotification())
                    {
                        msgFlags &= ~Misc.Bit(0);
                        break;
                    }

                    Message message = player.PeekNotification();

                    if (Misc.BitTest(config, MsgCategory[(int)message.MessageType]))
                        break;

                    player.PopNotification();
                }
            }

            Viewport.Update();
            SetRedraw();
        }

        void GetMapCursorType(Player player, MapPos pos,
                           out BuildPossibility buildPossibility,
                           out CursorType cursorType)
        {
            Map map = Game.Map;

            if (player == null)
            {
                buildPossibility = BuildPossibility.None;
                cursorType = CursorType.Clear;
                return;
            }

            if (Game.CanBuildCastle(pos, player))
            {
                buildPossibility = BuildPossibility.Castle;
            }
            else if (Game.CanPlayerBuild(pos, player) &&
                Map.MapSpaceFromObject[(int)map.GetObject(pos)] == Map.Space.Open &&
                (Game.CanBuildFlag(map.MoveDownRight(pos), player) ||
                map.HasFlag(map.MoveDownRight(pos))))
            {
                if (Game.CanBuildMine(pos))
                {
                    buildPossibility = BuildPossibility.Mine;
                }
                else if (Game.CanBuildLarge(pos))
                {
                    buildPossibility = BuildPossibility.Large;
                }
                else if (Game.CanBuildSmall(pos))
                {
                    buildPossibility = BuildPossibility.Small;
                }
                else if (Game.CanBuildFlag(pos, player))
                {
                    buildPossibility = BuildPossibility.Flag;
                }
                else
                {
                    buildPossibility = BuildPossibility.None;
                }
            }
            else if (Game.CanBuildFlag(pos, player))
            {
                buildPossibility = BuildPossibility.Flag;
            }
            else
            {
                buildPossibility = BuildPossibility.None;
            }

            if (map.GetObject(pos) == Map.Object.Flag &&
                map.GetOwner(pos) == player.Index)
            {
                if (Game.CanDemolishFlag(pos, player))
                {
                    cursorType = CursorType.RemovableFlag;
                }
                else
                {
                    cursorType = CursorType.Flag;
                }
            }
            else if (!map.HasBuilding(pos) && !map.HasFlag(pos))
            {
                if (map.Paths(pos) == 0)
                {
                    if (map.GetObject(map.MoveDownRight(pos)) == Map.Object.Flag)
                    {
                        cursorType = CursorType.ClearByFlag;
                    }
                    else if (map.Paths(map.MoveDownRight(pos)) == 0)
                    {
                        cursorType = CursorType.Clear;
                    }
                    else
                    {
                        cursorType = CursorType.ClearByPath;
                    }
                }
                else if (map.GetOwner(pos) == player.Index)
                {
                    cursorType = CursorType.Path;
                }
                else
                {
                    cursorType = CursorType.None;
                }
            }
            else if ((map.GetObject(pos) == Map.Object.SmallBuilding ||
                map.GetObject(pos) == Map.Object.LargeBuilding) &&
                map.GetOwner(pos) == player.Index)
            {
                Building building = Game.GetBuildingAtPos(pos);

                if (!building.IsBurning())
                {
                    cursorType = CursorType.Building;
                }
                else
                {
                    cursorType = CursorType.None;
                }
            }
            else
            {
                cursorType = CursorType.None;
            }
        }

        /* Update the interface_t object with the information returned
           in get_map_cursor_type(). */
        void DetermineMapCursorType()
        {
            GetMapCursorType(player, mapCursorPos, out buildPossibility, out mapCursorType);
        }

        /* Update the interface_t object with the information returned
           in get_map_cursor_type(). This is sets the appropriate values
           when the player interface is in road construction mode. */
        void DetermineMapCursorTypeRoad()
        {
            Map map = Game.Map;
            MapPos pos = mapCursorPos;
            int h = (int)map.GetHeight(pos);
            int validDir = 0;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (Direction d in cycle)
            {
                int sprite = 0;

                if (buildingRoad.IsUndo(d))
                {
                    sprite = 45; /* undo */
                    validDir |= Misc.Bit((int)d);
                }
                else if (map.IsRoadSegmentValid(pos, d))
                {
                    if (buildingRoad.IsValidExtension(map, d))
                    {
                        int hDiff = (int)map.GetHeight(map.Move(pos, d)) - h;
                        sprite = 39 + hDiff; /* height indicators */
                        validDir |= Misc.Bit((int)d);
                    }
                    else
                    {
                        sprite = 44;
                    }
                }
                else
                {
                    sprite = 44; /* striped */
                }

                mapCursorSprites[(int)d + 1].Sprite = sprite;
            }

            buildingRoadValidDir = validDir;
        }

        /* Set the appropriate sprites for the panel buttons and the map cursor. */
        void UpdateInterface()
        {
            if (!IsBuildingRoad())
            {
                switch (mapCursorType)
                {
                    case CursorType.None:
                        mapCursorSprites[0].Sprite = 32;
                        mapCursorSprites[2].Sprite = 33;
                        break;
                    case CursorType.Flag:
                        mapCursorSprites[0].Sprite = 51;
                        mapCursorSprites[2].Sprite = 33;
                        break;
                    case CursorType.RemovableFlag:
                        mapCursorSprites[0].Sprite = 51;
                        mapCursorSprites[2].Sprite = 33;
                        break;
                    case CursorType.Building:
                        mapCursorSprites[0].Sprite = 32;
                        mapCursorSprites[2].Sprite = 33;
                        break;
                    case CursorType.Path:
                        mapCursorSprites[0].Sprite = 52;
                        mapCursorSprites[2].Sprite = 33;
                        if (buildPossibility != BuildPossibility.None)
                        {
                            mapCursorSprites[0].Sprite = 47;
                        }
                        break;
                    case CursorType.ClearByFlag:
                        if (buildPossibility < BuildPossibility.Mine)
                        {
                            mapCursorSprites[0].Sprite = 32;
                            mapCursorSprites[2].Sprite = 33;
                        }
                        else
                        {
                            mapCursorSprites[0].Sprite = 46 + (int)buildPossibility;
                            mapCursorSprites[2].Sprite = 33;
                        }
                        break;
                    case CursorType.ClearByPath:
                        if (buildPossibility != BuildPossibility.None)
                        {
                            mapCursorSprites[0].Sprite = 46 + (int)buildPossibility;

                            if (buildPossibility == BuildPossibility.Flag)
                            {
                                mapCursorSprites[2].Sprite = 33;
                            }
                            else
                            {
                                mapCursorSprites[2].Sprite = 47;
                            }
                        }
                        else
                        {
                            mapCursorSprites[0].Sprite = 32;
                            mapCursorSprites[2].Sprite = 33;
                        }
                        break;
                    case CursorType.Clear:
                        if (buildPossibility != BuildPossibility.None)
                        {
                            if (buildPossibility == BuildPossibility.Castle)
                            {
                                mapCursorSprites[0].Sprite = 50;
                            }
                            else
                            {
                                mapCursorSprites[0].Sprite = 46 + (int)buildPossibility;
                            }
                            if (buildPossibility == BuildPossibility.Flag)
                            {
                                mapCursorSprites[2].Sprite = 33;
                            }
                            else
                            {
                                mapCursorSprites[2].Sprite = 47;
                            }
                        }
                        else
                        {
                            mapCursorSprites[0].Sprite = 32;
                            mapCursorSprites[2].Sprite = 33;
                        }
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }
            }

            if (PanelBar != null)
            {
                PanelBar.Update();
            }
        }

        static void UpdateMapHeight(MapPos pos, object data)
        {
            Interface i = data as Interface;

            i.Viewport.RedrawMapPos(pos);
        }

        protected override void Layout()
        {
            int panelX = 0;
            int panelY = (int)Height;

            if (PanelBar != null)
            {
                int panelWidth = 352;
                int panelHeight = 40;
                panelX = (Width - panelWidth) / 2;
                panelY = Height - panelHeight;
                PanelBar.MoveTo(panelX, panelY);
                PanelBar.SetSize(panelWidth, panelHeight);
            }

            if (PopupBox != null)
            {
                int popupWidth = 144;
                int popupHeight = 160;
                int popupX = (Width - popupWidth) / 2;
                int popupY = (Height - popupHeight) / 2;
                PopupBox.MoveTo(popupX, popupY);
                PopupBox.SetSize(popupWidth, popupHeight);
            }

            if (initBox != null)
            {
                int initBoxWidth = 16 + 320 + 16;
                int initBoxHeight = 200;
                int initBoxX = (Width - initBoxWidth) / 2;
                int initBoxY = (Height - initBoxHeight) / 2;
                initBox.MoveTo(initBoxX, initBoxY);
                initBox.SetSize(initBoxWidth, initBoxHeight);
            }

            if (NotificationBox != null)
            {
                int notificationBoxWidth = 200;
                int notificationBoxHeight = 88;
                int notificationBoxX = panelX + 40;
                int notificationBoxY = panelY - notificationBoxHeight;
                NotificationBox.MoveTo(notificationBoxX, notificationBoxY);
                NotificationBox.SetSize(notificationBoxWidth, notificationBoxHeight);
            }

            if (Viewport != null)
            {
                Viewport.SetSize(Width, Height);
            }

            SetRedraw();
        }

        protected override bool HandleKeyPressed(char key, int modifier)
        {
            switch ((int)key)
            {
                /* Interface control */
                case '\t':
                    {
                        if ((modifier & 2) != 0)
                        {
                            ReturnFromMessage();
                        }
                        else
                        {
                            OpenMessage();
                        }
                        break;
                    }
                case 27:
                    {
                        if ((NotificationBox != null) && NotificationBox.Displayed)
                        {
                            CloseMessage();
                        }
                        else if (PopupBox != null && PopupBox.Displayed)
                        {
                            ClosePopup();
                        }
                        else if (IsBuildingRoad())
                        {
                            BuildRoadEnd();
                        }

                        break;
                    }

                /* Game speed */
                case '+':
                    {
                        Game.IncreaseSpeed();
                        break;
                    }
                case '-':
                    {
                        Game.DecreaseSpeed();
                        break;
                    }
                case '0':
                    {
                        Game.ResetSpeed();
                        break;
                    }
                case 'p':
                    {
                        Game.Pause();
                        break;
                    }

                /* Audio */
                case 's':
                    {
                        Audio audio = Audio.Instance;
                        Audio.Player audioPlayer = audio.GetSoundPlayer();

                        if (audioPlayer != null)
                        {
                            audioPlayer.Enable(!audioPlayer.IsEnabled);
                        }

                        break;
                    }
                case 'm':
                    {
                        Audio audio = Audio.Instance;
                        Audio.Player audioPlayer = audio.GetMusicPlayer();

                        if (audioPlayer != null)
                        {
                            audioPlayer.Enable(!audioPlayer.IsEnabled);
                        }

                        break;
                    }

                /* Debug */
                case 'g':
                    {
                        Viewport.SwitchLayer(global::Freeserf.Layer.Grid);
                        break;
                    }

                /* Game control */
                case 'b':
                    {
                        Viewport.SwitchLayer(global::Freeserf.Layer.Builds);
                        break;
                    }
                case 'j':
                    {
                        uint index = Game.GetNextPlayer(player).Index;
                        SetPlayer(index);
                        Log.Debug.Write("main", "Switched to player #" + index);
                        break;
                    }
                case 'z':
                    if ((modifier & 1) != 0)
                    {
                        GameStore.Instance.QuickSave("quicksave", Game);
                    }
                    break;
                case 'n':
                    if ((modifier & 1) != 0)
                    {
                        OpenGameInit();
                    }
                    break;
                case 'c':
                    if ((modifier & 1) != 0)
                    {
                        OpenPopup(PopupBox.Type.QuitConfirm);
                    }
                    break;

                default:
                    return false;
            }

            return true;
        }

        // TODO: override
        public void OnNewGame(Game game)
        {
            SetGame(game);
            SetPlayer(0);
        }

        public void OnEndGame(Game game)
        {
            SetGame(null);
        }

        protected internal override void UpdateParent()
        {
            Viewport?.UpdateParent();
            PanelBar?.UpdateParent();
            PopupBox?.UpdateParent();
            NotificationBox?.UpdateParent();
        }
    }
}
