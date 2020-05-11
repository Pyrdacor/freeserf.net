/*
 * Interface.cs - Top-level GUI interface
 *
 * Copyright (C) 2013       Jon Lund Steffensen <jonlst@gmail.com>
 * Copyright (C) 2018-2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Freeserf.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.UI
{
    using MapPos = UInt32;

    // TODO: implement fast mapclick and fast building
    internal class Interface : GuiObject
    {
        // Interval between automatic save games
        const int AUTOSAVE_INTERVAL = 10 * 60 * Global.TICKS_PER_SEC; // TODO: autosaves

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

        readonly object gameLock = new object();
        GameInitBox initBox;

        protected MapPos mapCursorPosition = 0u;
        CursorType mapCursorType = CursorType.None;
        BuildPossibility buildPossibility = BuildPossibility.None;

        uint lastConstTick;

        Road buildingRoad;
        int buildingRoadValidDir;
        readonly Stack<RenderRoadSegment> buildingRoadSegments = new Stack<RenderRoadSegment>();

        int[] sfxQueue = new int[4];

        int msgFlags; // TODO: Create constants/flag enum values for all bits and document possible values

        readonly SpriteLocation[] mapCursorSprites = new SpriteLocation[7];

        int returnTimeout;
        int returnPos;

        readonly ISprite cursorSprite = null;

        public IRenderView RenderView { get; } = null;
        public Audio.IAudioInterface AudioInterface { get; } = null;
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
        public string ServerGameName => initBox?.ServerGameName ?? "";
        public GameInfo ServerGameInfo => initBox?.ServerGameInfo;
        public Network.ILocalServer Server => initBox?.Server;
        public Network.ILocalClient Client => initBox?.Client;

        public Interface(IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer viewer)
            : base(renderView, audioInterface)
        {
            RenderView = renderView;
            AudioInterface = audioInterface;
            Viewer = viewer;

            TextRenderer = new TextRenderer(renderView);

            displayed = true;
            Options = (Option)UserConfig.Game.Options;

            mapCursorSprites[0] = new SpriteLocation { Sprite = 31 };
            mapCursorSprites[1] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[2] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[3] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[4] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[5] = new SpriteLocation { Sprite = 32 };
            mapCursorSprites[6] = new SpriteLocation { Sprite = 32 };

            cursorSprite = renderView.SpriteFactory.Create(16, 16, 0, 0, false, false, 255);
            cursorSprite.Layer = renderView.GetLayer(Freeserf.Layer.Cursor);
            cursorSprite.Visible = true;

            SetSize(640, 480); // original size

            Viewport = null;

            PanelBar = new PanelBar(this);
            AddChild(PanelBar, 0, 0, false);
            Layout();
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
                              e.Type == Event.Type.Drag ||
                              e.Type == Event.Type.StopDrag;

            // If the viewport is active and it is a click event
            // we will disable the viewport temporary to avoid
            // viewport mouse interaction handling with gui
            // coordinates.
            if (viewportActive && clickEvent)
                Viewport.Enabled = false;

            // Now test if there are gui elements that handle
            // the event.
            try
            {
                if (base.HandleEvent(e))
                {
                    // Ensure viewport enable reset
                    if (viewportEnabled)
                        Viewport.Enabled = true;

                    return true; // handled
                }
            }
            catch (Exception ex)
            {
                throw new ExceptionFreeserf(Game, ex);
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
                    {
                        delta = Gui.DeltaToGame(delta, RenderView);

                        if (e.Button == Event.Button.Right && GetOption(Option.HideCursorWhileScrolling))
                            cursorSprite.Visible = false;
                    }

                    try
                    {
                        // pass game transformed mouse data to the viewport
                        return Viewport.HandleEvent(Event.EventArgs.Transform(e, position.X, position.Y, delta.Width, delta.Height));
                    }
                    catch (Exception ex)
                    {
                        throw new ExceptionFreeserf(Game, ex);
                    }
                }
                else if (e.Type == Event.Type.StopDrag)
                {
                    cursorSprite.Visible = Displayed;
                }
                else
                {
                    Debug.NotReached();
                }
            }

            return false;
        }

        public override bool Displayed
        {
            get => base.Displayed;
            set
            {
                if (base.Displayed == value)
                    return;

                base.Displayed = value;
                cursorSprite.Visible = value;
            }
        }

        protected override void InternalDraw()
        {
            // Nothing to do here.
        }

        public void SetGame(Game game)
        {
            lock (gameLock)
            {
                if (Viewport != null)
                {
                    Viewport.CleanUp();
                    DeleteChild(Viewport);
                    Viewport = null;
                }

                Game = game;
                Player = null;

                if (Game != null)
                {
                    if (game.Map == null)
                    {
                        SetGame(null);
                        Log.Debug.Write(ErrorSystemType.Game, "Internal error. Map is null.");
                        return;
                    }

                    game.Map.AttachToRenderLayer(RenderView.GetLayer(Freeserf.Layer.Landscape), RenderView.GetLayer(Freeserf.Layer.Waves), RenderView.DataSource);

                    // Note: The render map must be created above with AttachToRenderLayer before viewport creation.
                    Viewport = new Viewport(this, Game.Map);
                    Viewport.Displayed = true;
                    AddChild(Viewport, 0, 0);

                    if (PanelBar == null)
                        PanelBar = new PanelBar(this);
                    if (!PanelBar.Enabled)
                        PanelBar.Enabled = true;
                }

                Layout();
            }
        }

        public Color GetPlayerColor(uint playerIndex)
        {
            return Game.GetPlayer(playerIndex).Color;
        }

        public Option Options { get; private set; } = Option.Default;

        public bool GetOption(Option option)
        {
            return Options.HasFlag(option);
        }

        public void SetOption(Option option)
        {
            Options |= option;
        }

        public void ResetOption(Option option)
        {
            Options &= ~option;
        }

        public void SwitchOption(Option option)
        {
            if (GetOption(option))
                ResetOption(option);
            else
                SetOption(option);
        }

        public MapPos GetMapCursorPosition()
        {
            return mapCursorPosition;
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

        // Open popup box 
        public void OpenPopup(PopupBox.Type box)
        {
            if (PopupBox == null)
                PopupBox = new PopupBox(this);

            if (initBox != null && initBox.Displayed)
                initBox.AddChild(PopupBox, 0, 0);
            else
                AddChild(PopupBox, 0, 0);

            Layout();
            PopupBox.Show(box);
            PanelBar?.Update();
        }

        // Close the current popup. 
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

            UpdateMapCursorPosition(mapCursorPosition);
            PanelBar?.Update();
        }

        // Open box for starting a new game 
        public void OpenGameInit(GameInitBox.GameType gameType = GameInitBox.GameType.Custom)
        {
            // the following code will start the intro mission that is played in the background while the GameInitBox is active
            GameManager.Instance.StartGame(GameInfo.GetIntroMission(), RenderView, AudioInterface);

            ClosePopup();

            RenderView.ResetZoom();

            if (initBox == null)
            {
                initBox = new GameInitBox(this, gameType);
                AddChild(initBox, 0, 0);
            }
            else
            {
                initBox.UpdateGameType(false);
            }

            initBox.Displayed = true;
            initBox.Enabled = true;

            PanelBar.Displayed = false;
            PanelBar.Enabled = false;

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
            }

            PanelBar.Displayed = true;
            PanelBar.Enabled = true;

            Viewport.Enabled = true;
            Layout();

            UpdateMapCursorPosition(mapCursorPosition);
        }

        public void Destroy()
        {
            ClosePopup();
            PopupBox = null;

            if (initBox != null)
            {
                initBox.Displayed = false;
            }

            if (PanelBar != null)
            {
                PanelBar.Displayed = false;
                PanelBar.Enabled = false;
            }

            if (Viewport != null)
                Viewport.Enabled = false;

            SetRedraw();
        }

        // Open box for next message in the message queue 
        public void OpenMessage()
        {
            if (!Player.HasAnyNotification)
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                return;
            }
            else if (!Misc.BitTest(msgFlags, 3))
            {
                msgFlags |= Misc.Bit(4);
                msgFlags |= Misc.Bit(3);

                var position = Viewport.GetCurrentMapPosition();

                returnPos = (int)position;
            }

            var notification = Player.PopNotification();

            if (notification.NotificationType == Notification.Type.CallToMenu)
            {
                // TODO 
            }

            if (NotificationBox == null)
            {
                NotificationBox = new NotificationBox(this);
                AddChild(NotificationBox, 0, 0);
            }

            NotificationBox.Show(notification);
            Layout();

            if (Misc.BitTest(0x8f3fe, (int)notification.NotificationType))
            {
                // Move screen to new position 
                Viewport.MoveToMapPosition(notification.Position, true);
                UpdateMapCursorPosition(notification.Position);
            }

            msgFlags |= Misc.Bit(1);
            returnTimeout = 60 * Global.TICKS_PER_SEC;
            PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
        }

        public void ReturnFromMessage()
        {
            if (Misc.BitTest(msgFlags, 3))
            {
                // Return arrow present 
                msgFlags |= Misc.Bit(4);
                msgFlags &= ~Misc.Bit(3);

                returnTimeout = 0;
                Viewport.MoveToMapPosition((uint)returnPos, false);

                if (PopupBox != null && PopupBox.Box == PopupBox.Type.Message)
                {
                    ClosePopup();
                }

                PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
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
            if (Player != null && Player.HasCastle)
            {
                GotoMapPosition(Player.CastlePosition);
            }
        }

        public void GotoMapPosition(MapPos position)
        {
            if (Ingame && Viewport != null)
            {
                Viewport.MoveToMapPosition(position, true);
                UpdateMapCursorPosition(position);
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

        public Player Player { get; private set; }

        public void SetPlayer(uint player)
        {
            if (Game == null)
            {
                return;
            }

            if (this.Player != null && player == this.Player.Index)
            {
                return;
            }

            this.Player = Game.GetPlayer(player);

            // Move viewport to initial position 
            var initialPosition = Game.Map.Position(0, 0);

            if (this.Player != null)
            {
                if (Ingame)
                    PanelBar.Displayed = true;
                else
                    PanelBar.Displayed = false;

                if (this.Player.CastlePosition != Global.INVALID_MAPPOS)
                    initialPosition = this.Player.CastlePosition;
            }
            else
            {
                PanelBar.Displayed = false;
            }

            UpdateMapCursorPosition(initialPosition);
            Viewport.MoveToMapPosition(mapCursorPosition, true);
        }

        public void UpdateMapCursorPosition(MapPos position)
        {
            mapCursorPosition = position;

            if (IsBuildingRoad)
            {
                DetermineMapCursorTypeRoad();
            }
            else
            {
                DetermineMapCursorType();
            }

            UpdateInterface();
        }

        public bool IsBuildingRoad => buildingRoad != null && buildingRoad.Valid;

        public Road GetBuildingRoad()
        {
            return buildingRoad;
        }

        // Start road construction mode for player interface. 
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
            buildingRoad.Start(mapCursorPosition);
            UpdateMapCursorPosition(mapCursorPosition);

            PanelBar?.Update();
        }

        // End road construction mode for player interface. 
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
            UpdateMapCursorPosition(mapCursorPosition);

            PanelBar?.Update();
        }

        public void BuildRoadReset()
        {
            BuildRoadEnd();
            BuildRoadBegin();
        }

        /* Build a single road segment. Return -1 on fail, 0 on successful
           construction, and 1 if this segment completed the path. */
        public int BuildRoadSegment(Direction direction, bool roadEndsThere)
        {
            if (!buildingRoad.Extendable)
            {
                // Max length reached 
                return -1;
            }

            buildingRoad.Cost += Pathfinder.ActualCost(Game.Map, buildingRoad.EndPosition, direction);

            AddBuildingRoadSegment(buildingRoad.EndPosition, direction);
            buildingRoad.Extend(Game.Map, direction);

            MapPos destination = 0;
            bool water = false;
            int result = Game.CanBuildRoad(buildingRoad, Player, ref destination, ref water, roadEndsThere);

            if (result <= 0)
            {
                // Invalid construction, undo. 
                return RemoveRoadSegment();
            }

            if (Game.Map.GetObject(destination) == Map.Object.Flag)
            {
                // Existing flag at destination, try to connect. 
                if (!Game.BuildRoad(buildingRoad.Copy(), Player, true))
                {
                    BuildRoadEnd();
                    return -1;
                }
                else
                {
                    BuildRoadEnd();
                    UpdateMapCursorPosition(destination);
                    return 1;
                }
            }
            else if (Game.Map.Paths(destination) == 0)
            {
                // No existing paths at destination, build segment. 
                UpdateMapCursorPosition(destination);

                if (GetOption(Option.PathwayScrolling)) // pathway scrolling
                    Viewport.MoveToMapPosition(destination, true);
            }
            else
            {
                // TODO fast split path and connect on double click 
                return -1;
            }

            return 0;
        }

        public int RemoveRoadSegment()
        {
            var destination = buildingRoad.StartPosition;
            int result = 0;
            bool water = false;
            buildingRoad.Undo(Game.Map);
            RemoveLastBuildingRoadSegment();

            if (buildingRoad.Length == 0 ||
                Game.CanBuildRoad(buildingRoad, Player, ref destination, ref water) == 0)
            {
                // Road construction is no longer valid, abort.
                BuildRoadEnd();
                result = -1;
            }

            UpdateMapCursorPosition(destination);

            if (GetOption(Option.PathwayScrolling)) // pathway scrolling
                Viewport.MoveToMapPosition(destination, true);

            return result;
        }

        // Extend currently constructed road with an array of directions. 
        public int ExtendRoad(Road road)
        {
            var oldRoad = buildingRoad;
            int directionIndex = 0;

            foreach (var direction in road.Directions.Reverse())
            {
                int result = BuildRoadSegment(direction, directionIndex == road.Length - 1);

                if (result < 0)
                {
                    buildingRoad = oldRoad;
                    return -1;
                }
                else if (result == 1)
                {
                    buildingRoad.Invalidate();
                    return 1;
                }

                ++directionIndex;
            }

            return 0;
        }

        public bool BuildRoadIsValidDirection(Direction direction)
        {
            return Misc.BitTest(buildingRoadValidDir, (int)direction);
        }

        public void DemolishObject()
        {
            DetermineMapCursorType();

            if (mapCursorType == CursorType.RemovableFlag)
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.Click);
                Game.DemolishFlag(mapCursorPosition, Player);
                DetermineMapCursorType();
            }
            else if (mapCursorType == CursorType.Building)
            {
                var building = Game.GetBuildingAtPosition(mapCursorPosition);

                if (building.IsDone &&
                    (building.BuildingType == Building.Type.Hut ||
                     building.BuildingType == Building.Type.Tower ||
                     building.BuildingType == Building.Type.Fortress))
                {
                    // TODO 
                }

                PlaySound(Freeserf.Audio.Audio.TypeSfx.Ahhh);
                Game.DemolishBuilding(mapCursorPosition, Player);
                DetermineMapCursorType();
            }
            else
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
            }

            UpdateInterface();
        }

        // Build new flag. 
        public void BuildFlag()
        {
            if (AccessRights != Viewer.Access.Player)
                return;

            if (!Game.BuildFlag(mapCursorPosition, Player))
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                return;
            }

            UpdateMapCursorPosition(mapCursorPosition);
        }

        // Build a new building. 
        public void BuildBuilding(Building.Type type)
        {
            if (AccessRights != Viewer.Access.Player)
                return;

            if (!Game.BuildBuilding(mapCursorPosition, type, Player))
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                return;
            }

            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
            ClosePopup();

            // Move cursor to flag. 
            var flagPosition = Game.Map.MoveDownRight(mapCursorPosition);
            UpdateMapCursorPosition(flagPosition);
        }

        // Build castle. 
        public void BuildCastle()
        {
            if (AccessRights != Viewer.Access.Player)
                return;

            if (!Game.BuildCastle(mapCursorPosition, Player))
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                return;
            }

            PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
            UpdateMapCursorPosition(mapCursorPosition);
        }

        public void BuildRoad()
        {
            if (AccessRights != Viewer.Access.Player)
                return;

            if (!Game.BuildRoad(buildingRoad.Copy(), Player))
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
                Game.DemolishFlag(mapCursorPosition, Player);
            }
            else
            {
                PlaySound(Freeserf.Audio.Audio.TypeSfx.Accepted);
                BuildRoadEnd();
            }
        }

        static readonly int[] MsgCategory = new int[]
        {
            -1, 5, 5, 5, 4, 0, 4, 3, 4, 5,
            5, 5, 4, 4, 4, 4, 0, 0, 0, 0
        };

        // Called periodically when the game progresses. 
        public virtual void Update()
        {
            if (Game == null)
            {
                return;
            }

            lock (gameLock)
            {
                // TODO: rethink this as the client should predict and simulate the game too
                /*if (this is RemoteInterface)
                    Game.UpdateVisuals();
                else*/
                Game.Update();

                UpdateBuildingRoadSegments();

                int tickDifference = (int)Game.ConstTick - (int)lastConstTick;
                lastConstTick = Game.ConstTick;

                // Clear return arrow after a timeout 
                if (returnTimeout < tickDifference)
                {
                    msgFlags |= Misc.Bit(4);
                    msgFlags &= ~Misc.Bit(3);
                    returnTimeout = 0;
                }
                else
                {
                    returnTimeout -= tickDifference;
                }

                // Handle newly enqueued messages 
                if (Player != null && Player.HasNotifications)
                {
                    Player.DropNotifications();

                    while (Player.HasAnyNotification)
                    {
                        var notification = Player.PeekNotification();

                        if (GetOption((Option)MsgCategory[(int)notification.NotificationType]))
                        {
                            PlaySound(Freeserf.Audio.Audio.TypeSfx.Message);
                            msgFlags |= Misc.Bit(0);
                            break;
                        }

                        Player.PopNotification();
                    }
                }

                if (Player != null && Misc.BitTest(msgFlags, 1))
                {
                    msgFlags &= ~Misc.Bit(1);

                    while (true)
                    {
                        if (!Player.HasAnyNotification)
                        {
                            msgFlags &= ~Misc.Bit(0);
                            break;
                        }

                        var notification = Player.PeekNotification();

                        if (GetOption((Option)MsgCategory[(int)notification.NotificationType]))
                            break;

                        Player.PopNotification();
                    }
                }

                Viewport.Update();
                SetRedraw();
            }
        }

        void AddBuildingRoadSegment(MapPos position, Direction direction)
        {
            // We only support road segments in direcitons Right, DownRight and Down.
            // So if it is another direction, we use the reverse direction with the opposite position.
            if (direction > Direction.Down)
            {
                position = Game.Map.Move(position, direction);
                direction = direction.Reverse();
            }

            var segment = new RenderRoadSegment(Game.Map, position, direction,
                RenderView.GetLayer(Freeserf.Layer.Paths), RenderView.SpriteFactory, RenderView.DataSource);

            segment.Visible = true;

            buildingRoadSegments.Push(segment);
        }

        void RemoveLastBuildingRoadSegment()
        {
            if (buildingRoadSegments.Count > 0)
            {
                buildingRoadSegments.Pop()?.Delete();
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
                buildingRoadSegments.Pop()?.Delete();
            }
        }

        void GetMapCursorType(Player player, MapPos position,
                           out BuildPossibility buildPossibility,
                           out CursorType cursorType)
        {
            var map = Game.Map;

            if (player == null || AccessRights != Viewer.Access.Player)
            {
                buildPossibility = BuildPossibility.None;
                cursorType = CursorType.Clear;
                return;
            }

            if (Game.CanBuildCastle(position, player))
            {
                buildPossibility = BuildPossibility.Castle;
            }
            else if (Game.CanPlayerBuild(position, player) &&
                Map.MapSpaceFromObject[(int)map.GetObject(position)] == Map.Space.Open &&
                (Game.CanBuildFlag(map.MoveDownRight(position), player) ||
                map.HasFlag(map.MoveDownRight(position))))
            {
                if (Game.CanBuildMine(position))
                {
                    buildPossibility = BuildPossibility.Mine;
                }
                else if (Game.CanBuildLarge(position))
                {
                    buildPossibility = BuildPossibility.Large;
                }
                else if (Game.CanBuildSmall(position))
                {
                    buildPossibility = BuildPossibility.Small;
                }
                else if (Game.CanBuildFlag(position, player))
                {
                    buildPossibility = BuildPossibility.Flag;
                }
                else
                {
                    buildPossibility = BuildPossibility.None;
                }
            }
            else if (Game.CanBuildFlag(position, player))
            {
                buildPossibility = BuildPossibility.Flag;
            }
            else
            {
                buildPossibility = BuildPossibility.None;
            }

            if (map.GetObject(position) == Map.Object.Flag &&
                map.GetOwner(position) == player.Index)
            {
                if (Game.CanDemolishFlag(position, player))
                {
                    cursorType = CursorType.RemovableFlag;
                }
                else
                {
                    cursorType = CursorType.Flag;
                }
            }
            else if (!map.HasBuilding(position) && !map.HasFlag(position))
            {
                if (map.Paths(position) == 0)
                {
                    if (map.GetObject(map.MoveDownRight(position)) == Map.Object.Flag)
                    {
                        cursorType = CursorType.ClearByFlag;
                    }
                    else if (map.Paths(map.MoveDownRight(position)) == 0)
                    {
                        cursorType = CursorType.Clear;
                    }
                    else
                    {
                        cursorType = CursorType.ClearByPath;
                    }
                }
                else if (map.GetOwner(position) == player.Index)
                {
                    cursorType = CursorType.Path;
                }
                else
                {
                    cursorType = CursorType.None;
                }
            }
            else if ((map.GetObject(position) == Map.Object.SmallBuilding ||
                map.GetObject(position) == Map.Object.LargeBuilding) &&
                map.GetOwner(position) == player.Index)
            {
                var building = Game.GetBuildingAtPosition(position);

                if (!building.IsBurning)
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
            GetMapCursorType(Player, mapCursorPosition, out buildPossibility, out mapCursorType);
        }

        /* Update the interface_t object with the information returned
           in get_map_cursor_type(). This is sets the appropriate values
           when the player interface is in road construction mode. */
        void DetermineMapCursorTypeRoad()
        {
            var map = Game.Map;
            var position = mapCursorPosition;
            int height = (int)map.GetHeight(position);
            int validDirection = 0;
            var cycle = DirectionCycleCW.CreateDefault();

            foreach (var direction in cycle)
            {
                uint sprite = 0;

                if (buildingRoad.IsUndo(direction))
                {
                    sprite = 44; // undo 
                    validDirection |= Misc.Bit((int)direction);
                }
                else if (map.IsRoadSegmentValid(position, direction, true))
                {
                    if (buildingRoad.IsValidExtension(map, direction))
                    {
                        int heightDifference = (int)map.GetHeight(map.Move(position, direction)) - height;
                        sprite = (uint)(38 + heightDifference); // height indicators 
                        validDirection |= Misc.Bit((int)direction);
                    }
                    else
                    {
                        sprite = 43;
                    }
                }
                else
                {
                    sprite = 43; // striped 
                }

                mapCursorSprites[(int)direction + 1].Sprite = sprite;
            }

            buildingRoadValidDir = validDirection;
        }

        // Set the appropriate sprites for the panel buttons and the map cursor. 
        void UpdateInterface()
        {
            if (!IsBuildingRoad)
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

        bool IsRemote => this is RemoteInterface;

        protected override bool HandleSystemKeyPressed(Event.SystemKey key, int modifier)
        {
            if (!Ingame)
                return false;

            if (key == Event.SystemKey.Escape)
            {
                if (NotificationBox != null && NotificationBox.Displayed)
                {
                    CloseMessage();
                }
                else if (PopupBox != null && PopupBox.Displayed)
                {
                    ClosePopup();
                }
                else if (IsBuildingRoad)
                {
                    BuildRoadEnd();
                }
            }

            return true;
        }

        protected override bool HandleKeyPressed(char key, int modifier)
        {
            if (!Ingame)
                return false;

            switch (key)
            {
                // Interface control 
                case Event.SystemKeys.Tab:
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
                // Game speed 
                case '+':
                    {
                        if (!IsRemote)
                            Game.IncreaseSpeed();
                        break;
                    }
                case '-':
                    {
                        if (!IsRemote)
                            Game.DecreaseSpeed();
                        break;
                    }
                case '0':
                    {
                        if (!IsRemote)
                            Game.ResetSpeed();
                        break;
                    }
                case 'P':
                    {
                        if (!IsRemote)
                            Game.TogglePause();
                        break;
                    }

                // Audio 
                case 'S':
                    {
                        var soundPlayer = Audio?.GetSoundPlayer();

                        if (soundPlayer != null)
                        {
                            soundPlayer.Enabled = !soundPlayer.Enabled;
                        }

                        break;
                    }
                case 'M':
                    {
                        var musicPlayer = Audio?.GetMusicPlayer();

                        if (musicPlayer != null)
                        {
                            musicPlayer.Enabled = !musicPlayer.Enabled;
                        }

                        break;
                    }

                // Game control 
                case 'B':
                    {
                        Viewport.ShowPossibleBuilds = !Viewport.ShowPossibleBuilds;
                        break;
                    }
                case 'J':
                    {
                        if (Viewer.AccessRights != Viewer.Access.Player)
                        {
                            uint index = Game.GetNextPlayer(Player).Index;
                            SetPlayer(index);
                            Log.Info.Write(ErrorSystemType.Game, "Switched to player #" + index);
                        }

                        break;
                    }
                case 'Z':
                    if ((modifier & 1) != 0)
                    {
                        if (!IsRemote)
                            GameStore.Instance.QuickSave("quicksave", Game);
                    }
                    break;
                case 'N':
                    if ((modifier & 1) != 0)
                    {
                        if (!IsRemote)
                            OpenGameInit();
                    }
                    break;
                case 'C':
                    if ((modifier & 1) != 0)
                    {
                        OpenPopup(PopupBox.Type.QuitConfirm);
                    }
                    break;
                case Event.SystemKeys.Delete:
                    if (PanelBar != null && PanelBar.Displayed && PanelBar.CanDemolish())
                    {
                        PanelBar.Demolish();
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }
    }

    internal class ServerInterface : Interface
    {
        readonly Network.ILocalServer server = null;

        public ServerInterface(IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer viewer, Network.ILocalServer server)
            : base(renderView, audioInterface, viewer)
        {
            this.server = server;
        }

        public override void Update()
        {
            base.Update();

            if (Game != null)
            {
                // TODO all to all
                foreach (var client in server.Clients)
                {
                    var player = Game.GetPlayer(client.PlayerIndex);

                    if (player.Dirty)
                        client.SendPlayerStateUpdate(player);
                }

                for (uint i = 0; i < Game.PlayerCount; ++i)
                    Game.GetPlayer(i).ResetDirtyFlag();
            }
        }
    }

    internal class RemoteInterface : Interface
    {
        public RemoteInterface(IRenderView renderView, Audio.IAudioInterface audioInterface, Viewer viewer)
            : base(renderView, audioInterface, viewer)
        {

        }

        public void GetMapUpdate()
        {

        }

        public void GetGameUpdate()
        {

        }

        public void GetPlayerUpdate(uint playerIndex)
        {

        }
    }
}
