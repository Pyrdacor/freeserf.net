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

using System;
using System.Collections.Generic;
using System.Linq;
using Freeserf.Render;

namespace Freeserf
{
    using MapPos = UInt32;

    // TODO: implement fast mapclick and fast building
    internal class Interface : GuiObject
    {
        // Interval between automatic save games
        const int AUTOSAVE_INTERVAL = 10 * 60 * Global.TICKS_PER_SEC;

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
            public uint Sprite;
            // TODO: are X and Y used?
            //public int X, Y;
        }

        GameInitBox initBox;

        MapPos mapCursorPos = 0u;
        CursorType mapCursorType = CursorType.None;
        BuildPossibility buildPossibility = BuildPossibility.None;

        uint lastConstTick;

        Road buildingRoad;
        int buildingRoadValidDir;
        readonly Stack<RenderRoadSegment> buildingRoadSegments = new Stack<RenderRoadSegment>();

        int[] sfxQueue = new int[4];

        Player player;
        // Bit 0: Always 1, is used for messages that should always be notified
        // Bit 1: Unused
        // Bit 2: Fast building
        // Bit 3: Non-important message. Is only set for message setting "all"
        // Bit 4: Important messages. Is set for at least message setting "most"
        // Bit 5: Very important messages. Is set for at least message setting "few"
        // Bit 6: Pathway scrolling
        // Bit 7: Fast map click
        int config = 0x39;
        int msgFlags;

        SpriteLocation[] mapCursorSprites = new SpriteLocation[7];

        int waterInView;
        int treesInView;

        int returnTimeout;
        int returnPos;

        ISprite cursorSprite = null;

        public IRenderView RenderView { get; } = null;
        public Viewport Viewport { get; private set; } = null;
        public PanelBar PanelBar { get; private set; } = null;
        public PopupBox PopupBox { get; private set; } = null;
        public NotificationBox NotificationBox { get; private set; } = null;
        public Game Game { get; private set; } = null;
        public Random Random { get; private set; } = null;
        public TextRenderer TextRenderer { get; } = null;
        public bool Ingame => Game != null && (initBox == null || !initBox.Displayed);
        public Viewer.Access AccessRights => Viewer.AccessRights;
        internal Viewer Viewer { get; set; }

        public Interface(IRenderView renderView, Viewer viewer)
            : base(renderView)
        {
            RenderView = renderView;
            Viewer = viewer; 

            TextRenderer = new TextRenderer(renderView);

            displayed = true;

            mapCursorSprites[0] = new SpriteLocation { Sprite = 31 };
            mapCursorSprites[1] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[2] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[3] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[4] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[5] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[6] = new SpriteLocation { Sprite = 32 };

            cursorSprite = renderView.SpriteFactory.Create(16, 16, 0, 0, false, false);
            cursorSprite.Layer = renderView.GetLayer(Freeserf.Layer.Cursor);
            cursorSprite.Visible = true;

            SetSize(640, 480); // original size

            Viewport = null;
        }

        internal void DrawCursor(int x, int y)
        {
            cursorSprite.X = x - cursorSprite.Width / 2;
            cursorSprite.Y = y - cursorSprite.Height / 2;
        }

        public override bool HandleEvent(Event.EventArgs e)
        {
            if (!Enabled || !Displayed)
            {
                return false;
            }

            bool viewportActive = Ingame && Viewport != null;
            bool viewportEnabled = Viewport != null && Viewport.Enabled;
            bool clickEvent = e.Type == Event.Type.Click ||
                              e.Type == Event.Type.DoubleClick ||
                              e.Type == Event.Type.SpecialClick ||
                              e.Type == Event.Type.Drag;

            // If the viewport is active and it is a click event
            // we will disable the viewport temporary to avoid
            // viewport mouse interaction handling with gui
            // coordinates.
            if (viewportActive && clickEvent)
                Viewport.Enabled = false;

            // Now test if there are gui elements that handle
            // the event.
            if (base.HandleEvent(e))
            {
                // Ensure viewport enable reset
                if (viewportEnabled)
                    Viewport.Enabled = true;

                return true; // handled
            }

            // If not handled we check for viewport mouse interaction
            if (viewportActive && clickEvent && viewportEnabled)
            {
                Viewport.Enabled = true;

                if (e.UntransformedArgs != null)
                {
                    var position = new Position(e.UntransformedArgs.X, e.UntransformedArgs.Y);
                    var delta = new Size(e.UntransformedArgs.Dx, e.UntransformedArgs.Dy);

                    position = Gui.PositionToGame(position, RenderView); // this considers game zoom

                    if (e.Type == Event.Type.Drag)
                        delta = Gui.DeltaToGame(delta, RenderView);

                    // pass game transformed mouse data to the viewport
                    return Viewport.HandleEvent(Event.EventArgs.Transform(e, position.X, position.Y, delta.Width, delta.Height));
                }
                else
                {
                    Debug.NotReached();
                }
            }

            return false;
        }

        protected override void InternalDraw()
        {
            cursorSprite.Visible = Displayed;
        }

        public void SetGame(Game game)
        {
            if (Viewport != null)
            {
                Viewport.CleanUp();
                DeleteChild(Viewport);
                Viewport = null;
            }

            Game = game;
            player = null;

            if (Game != null)
            {
                game.Map.AttachToRenderLayer(RenderView.GetLayer(Freeserf.Layer.Landscape), RenderView.GetLayer(Freeserf.Layer.Waves), RenderView.DataSource);

                // Note: The render map must be created above with AttachToRenderLayer before viewport creation.
                Viewport = new Viewport(this, Game.Map);
                Viewport.Displayed = true;
                AddChild(Viewport, 0, 0);
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
            config = Misc.BitInvert(config, i);
        }

        public MapPos GetMapCursorPos()
        {
            return mapCursorPos;
        }

        public CursorType GetMapCursorType()
        {
            return mapCursorType;
        }

        public uint GetMapCursorSprite(int i)
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

        public BuildPossibility GetBuildPossibility()
        {
            return buildPossibility;
        }

        /* Open popup box */
        public void OpenPopup(PopupBox.Type box)
        {
            if (PopupBox == null)
                PopupBox = new PopupBox(this);

            if (initBox != null)
                initBox.AddChild(PopupBox, 0, 0);
            else
                AddChild(PopupBox, 0, 0);

            Layout();
            PopupBox.Show(box);
            PanelBar?.Update();
        }

        /* Close the current popup. */
        public void ClosePopup()
        {
            if (PopupBox == null)
            {
                return;
            }

            PopupBox.Hide();

            if (initBox != null)
                initBox.DeleteChild(PopupBox);
            else
                DeleteChild(PopupBox);

            UpdateMapCursorPos(mapCursorPos);
            PanelBar?.Update();
        }

        /* Open box for starting a new game */
        public void OpenGameInit()
        {
            // the following code will start the intro mission that is played in the background while the GameInitBox is active
            GameManager.Instance.StartGame(GameInfo.GetIntroMission(), RenderView);

            ClosePopup();

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

            if (Viewport != null)
                Viewport.Enabled = false;

            Layout();
        }

        public void CloseGameInit()
        {
            ClosePopup();
            PopupBox = null;

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
            Layout();

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
            Layout();

            if (Misc.BitTest(0x8f3fe, (int)message.MessageType))
            {
                /* Move screen to new position */
                Viewport.MoveToMapPos(message.Pos, true);
                UpdateMapCursorPos(message.Pos);
            }

            msgFlags |= Misc.Bit(1);
            returnTimeout = 60 * Global.TICKS_PER_SEC;
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
                Viewport.MoveToMapPos((uint)returnPos, false);

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
            Layout();
        }

        public void GotoCastle()
        {
            if (player != null && player.HasCastle())
            {
                GotoMapPos(player.CastlePos);
            }
        }

        public void GotoMapPos(MapPos pos)
        {
            if (Ingame && Viewport != null)
            {
                Viewport.MoveToMapPos(pos, true);
                UpdateMapCursorPos(pos);
            }
        }

        public void UpdateMinimap()
        {
            if (PopupBox != null && PopupBox.MiniMap != null && PopupBox.Displayed && PopupBox.MiniMap.Displayed)
                PopupBox.MiniMap.UpdateMinimap();
        }

        public void TogglePossibleBuilds()
        {
            if (Ingame && Viewport != null)
            {
                Viewport.ShowPossibleBuilds = !Viewport.ShowPossibleBuilds;
            }
        }

        public void ResetPossibleBuilds()
        {
            if (Ingame && Viewport != null)
            {
                Viewport.ShowPossibleBuilds = false;
            }
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
                if (PanelBar == null && Ingame)
                {
                    PanelBar = new PanelBar(this);
                    AddChild(PanelBar, 0, 0, true);
                    Layout();
                }

                if (this.player.CastlePos != Global.BadMapPos)
                    initPos = this.player.CastlePos;
            }

            UpdateMapCursorPos(initPos);
            Viewport.MoveToMapPos(mapCursorPos, true);
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

            if (buildingRoad == null)
                buildingRoad = new Road();

            buildingRoad.Invalidate();
            buildingRoad.Start(mapCursorPos);
            UpdateMapCursorPos(mapCursorPos);

            PanelBar?.Update();
        }

        /* End road construction mode for player interface. */
        public void BuildRoadEnd()
        {
            mapCursorSprites[1].Sprite = 32;
            mapCursorSprites[2].Sprite = 32;
            mapCursorSprites[3].Sprite = 32;
            mapCursorSprites[4].Sprite = 32;
            mapCursorSprites[5].Sprite = 32;
            mapCursorSprites[6].Sprite = 32;

            ClearBuildingRoadSegments();
            buildingRoad.Invalidate();
            UpdateMapCursorPos(mapCursorPos);

            PanelBar?.Update();
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

            AddBuildingRoadSegment(buildingRoad.GetEnd(Game.Map), dir);
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

                if (GetConfig(6)) // pathway scrolling
                    Viewport.MoveToMapPos(dest, true);
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
            RemoveLastBuildingRoadSegment();

            if (buildingRoad.Length == 0 ||
                Game.CanBuildRoad(buildingRoad, player, ref dest, ref water) == 0)
            {
                /* Road construction is no longer valid, abort. */
                BuildRoadEnd();
                res = -1;
            }

            UpdateMapCursorPos(dest);

            if (GetConfig(6)) // pathway scrolling
                Viewport.MoveToMapPos(dest, true);

            return res;
        }

        /* Extend currently constructed road with an array of directions. */
        public int ExtendRoad(Road road)
        {
            Road oldRoad = buildingRoad;

            foreach (Direction dir in road.Dirs.Reverse())
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
                DetermineMapCursorType();
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
                DetermineMapCursorType();
            }
            else
            {
                PlaySound(Audio.TypeSfx.NotAccepted);
            }

            UpdateInterface();
        }

        /* Build new flag. */
        public void BuildFlag()
        {
            if (AccessRights != Viewer.Access.Player)
                return;

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
            if (AccessRights != Viewer.Access.Player)
                return;

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
            if (AccessRights != Viewer.Access.Player)
                return;

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
            if (AccessRights != Viewer.Access.Player)
                return;

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

            UpdateBuildingRoadSegments();

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

        void AddBuildingRoadSegment(MapPos pos, Direction dir)
        {
            // We only support road segments in direcitons Right, DownRight and Down.
            // So if it is another direction, we use the reverse direction with the opposite position.
            if (dir > Direction.Down)
            {
                pos = Game.Map.Move(pos, dir);
                dir = dir.Reverse();
            }

            var segment = new RenderRoadSegment(Game.Map, pos, dir, RenderView.GetLayer(Freeserf.Layer.Paths), RenderView.SpriteFactory, RenderView.DataSource);

            segment.Visible = true;

            buildingRoadSegments.Push(segment);
        }

        void RemoveLastBuildingRoadSegment()
        {
            if (buildingRoadSegments.Count > 0)
            {
                buildingRoadSegments.Pop().Delete();
            }
        }

        void UpdateBuildingRoadSegments()
        {
            foreach (var segment in buildingRoadSegments)
            {
                segment.Update(Game.Map.RenderMap);
            }
        }

        void ClearBuildingRoadSegments()
        {
            while (buildingRoadSegments.Count > 0)
            {
                buildingRoadSegments.Pop().Delete();
            }
        }

        void GetMapCursorType(Player player, MapPos pos,
                           out BuildPossibility buildPossibility,
                           out CursorType cursorType)
        {
            Map map = Game.Map;

            if (player == null || AccessRights != Viewer.Access.Player)
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
                uint sprite = 0;

                if (buildingRoad.IsUndo(d))
                {
                    sprite = 44; /* undo */
                    validDir |= Misc.Bit((int)d);
                }
                else if (map.IsRoadSegmentValid(pos, d))
                {
                    if (buildingRoad.IsValidExtension(map, d))
                    {
                        int hDiff = (int)map.GetHeight(map.Move(pos, d)) - h;
                        sprite = (uint)(38 + hDiff); /* height indicators */
                        validDir |= Misc.Bit((int)d);
                    }
                    else
                    {
                        sprite = 43;
                    }
                }
                else
                {
                    sprite = 43; /* striped */
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
                        mapCursorSprites[0].Sprite = 31u;
                        mapCursorSprites[2].Sprite = 32u;
                        break;
                    case CursorType.Flag:
                        mapCursorSprites[0].Sprite = 50u;
                        mapCursorSprites[2].Sprite = 32u;
                        break;
                    case CursorType.RemovableFlag:
                        mapCursorSprites[0].Sprite = 50u;
                        mapCursorSprites[2].Sprite = 32u;
                        break;
                    case CursorType.Building:
                        mapCursorSprites[0].Sprite = 31u;
                        mapCursorSprites[2].Sprite = 32u;
                        break;
                    case CursorType.Path:
                        mapCursorSprites[0].Sprite = 51u;
                        mapCursorSprites[2].Sprite = 32u;
                        if (buildPossibility != BuildPossibility.None)
                        {
                            mapCursorSprites[0].Sprite = 46u;
                        }
                        break;
                    case CursorType.ClearByFlag:
                        if (buildPossibility < BuildPossibility.Mine)
                        {
                            mapCursorSprites[0].Sprite = 31u;
                            mapCursorSprites[2].Sprite = 32u;
                        }
                        else
                        {
                            mapCursorSprites[0].Sprite = 45u + (uint)buildPossibility;
                            mapCursorSprites[2].Sprite = 32u;
                        }
                        break;
                    case CursorType.ClearByPath:
                        if (buildPossibility != BuildPossibility.None)
                        {
                            mapCursorSprites[0].Sprite = 45u + (uint)buildPossibility;

                            if (buildPossibility == BuildPossibility.Flag)
                            {
                                mapCursorSprites[2].Sprite = 32u;
                            }
                            else
                            {
                                mapCursorSprites[2].Sprite = 46u;
                            }
                        }
                        else
                        {
                            mapCursorSprites[0].Sprite = 31u;
                            mapCursorSprites[2].Sprite = 32u;
                        }
                        break;
                    case CursorType.Clear:
                        if (buildPossibility != BuildPossibility.None)
                        {
                            if (buildPossibility == BuildPossibility.Castle)
                            {
                                mapCursorSprites[0].Sprite = 49u;
                            }
                            else
                            {
                                mapCursorSprites[0].Sprite = 45u + (uint)buildPossibility;
                            }
                            if (buildPossibility == BuildPossibility.Flag)
                            {
                                mapCursorSprites[2].Sprite = 32u;
                            }
                            else
                            {
                                mapCursorSprites[2].Sprite = 46u;
                            }
                        }
                        else
                        {
                            mapCursorSprites[0].Sprite = 31u;
                            mapCursorSprites[2].Sprite = 32u;
                        }
                        break;
                    default:
                        Debug.NotReached();
                        break;
                }
            }

            PanelBar?.Update();
        }

        static void UpdateMapHeight(MapPos pos, object data)
        {
            Interface i = data as Interface;

            i.Viewport.RedrawMapPos(pos);
        }

        protected override void Layout()
        {
            if (PanelBar != null)
            {
                int panelWidth = 352;
                int panelHeight = 40;
                PanelBar.MoveTo((Width - panelWidth) / 2, Height - panelHeight);
                PanelBar.SetSize(panelWidth, panelHeight);
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

            if (PopupBox != null && PopupBox.Parent != null)
            {
                int popupWidth = 144;
                int popupHeight = 160;
                int popupX = (PopupBox.Parent.Width - popupWidth) / 2;
                int popupY = (PopupBox.Parent.Height - popupHeight) / 2;
                PopupBox.MoveTo(popupX, popupY);
                PopupBox.SetSize(popupWidth, popupHeight);
            }

            if (NotificationBox != null)
            {
                int notificationBoxWidth = 144; // was 200
                int notificationBoxHeight = 160; // was 88
                int notificationBoxX = (Width - notificationBoxWidth) / 2;
                int notificationBoxY = (Height - notificationBoxHeight) / 2;
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
            if (!Ingame)
                return false;

            switch (key)
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
                case (char)27:
                    {
                        if (NotificationBox != null && NotificationBox.Displayed)
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
                case 'P':
                    {
                        Game.Pause();
                        break;
                    }

                /* Audio */
                case 'S':
                    {
                        Audio.Player audioPlayer = Audio?.GetSoundPlayer();

                        if (audioPlayer != null)
                        {
                            audioPlayer.Enabled = !audioPlayer.Enabled;
                        }

                        break;
                    }
                case 'M':
                    {
                        Audio.Player audioPlayer = Audio?.GetMusicPlayer();

                        if (audioPlayer != null)
                        {
                            audioPlayer.Enabled = !audioPlayer.Enabled;
                        }

                        break;
                    }

                /* Debug */
                case 'G':
                    {
                        Viewport.ShowGrid = !Viewport.ShowGrid;
                        break;
                    }

                /* Game control */
                case 'B':
                    {
                        Viewport.ShowPossibleBuilds = !Viewport.ShowPossibleBuilds;
                        break;
                    }
                case 'J':
                    {
                        uint index = Game.GetNextPlayer(player).Index;
                        SetPlayer(index);
                        Log.Debug.Write("main", "Switched to player #" + index);
                        break;
                    }
                case 'Z':
                    if ((modifier & 1) != 0)
                    {
                        GameStore.Instance.QuickSave("quicksave", Game);
                    }
                    break;
                case 'N':
                    if ((modifier & 1) != 0)
                    {
                        OpenGameInit();
                    }
                    break;
                case 'C':
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
    }
}
