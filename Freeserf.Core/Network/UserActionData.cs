/*
 * InSyncData.cs - User action message data
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.Network
{
    using MapPos = UInt32;

    public enum UserActionGameSetting
    {
        /// <summary>
        /// Unknown game setting.
        /// </summary>
        Unknown,
        /// <summary>
        /// The priority of a tool was changed.
        /// Byte 0: Tool index (0xff -> tool priority reset)
        /// Byte 1-2: New value (ignored for tool priority reset)
        /// </summary>
        ToolPriority,
        /// <summary>
        /// The transport priority of a resource was changed.
        /// Byte 0: Resource index (0xff -> transport priority reset)
        /// Byte 1: Ignored for transport priority reset
        ///     Bit 0: 0: Move down, 1 Move up
        ///     Bit 1: 0: Move one step, 1: Move to end
        /// </summary>
        FlagPriority,
        /// <summary>
        /// The 'move out of inventory' priority of a resource was changed.
        /// Byte 0: Resource index (0xff -> inventory priority reset)
        /// Byte 1: Ignored for inventory priority reset
        ///     Bit 0: 0: Move down, 1 Move up
        ///     Bit 1: 0: Move one step, 1: Move to end
        /// </summary>
        InventoryPriority,
        /// <summary>
        /// Knight occupation setting was changed.
        /// Byte 0: Farthest occupation
        /// Byte 1: Far occupation
        /// Byte 2: Close occupation
        /// Byte 3: Closest occupation
        /// </summary>
        KnightOccupation,
        /// <summary>
        /// Send strongest was activated.
        /// No parameters.
        /// </summary>
        SendStrongest,
        /// <summary>
        /// Send strongest was deactivated.
        /// No parameters.
        /// </summary>
        SendWeakest,
        /// <summary>
        /// Serf to knight rate was changed.
        /// Byte 0-1: New rate
        /// </summary>
        SerfToKnightRate,
        /// <summary>
        /// Number of wanted castle knights changed.
        /// Byte 0: New number
        /// </summary>
        CastleKnightsWanted,
        /// <summary>
        /// Food setting for stone mines was changed.
        /// Byte 0-1: New value
        /// </summary>
        FoodStonemine,
        /// <summary>
        /// Food setting for coal mines was changed.
        /// Byte 0-1: New value
        /// </summary>
        FoodCoalmine,
        /// <summary>
        /// Food setting for iron mines was changed.
        /// Byte 0-1: New value
        /// </summary>
        FoodIronmine,
        /// <summary>
        /// Food setting for gold mines was changed.
        /// Byte 0-1: New value
        /// </summary>
        FoodGoldmine,
        /// <summary>
        /// Plank setting for construction sites was changed.
        /// Byte 0-1: New value
        /// </summary>
        PlanksConstruction,
        /// <summary>
        /// Plank setting for boat builders was changed.
        /// Byte 0-1: New value
        /// </summary>
        PlanksBoatbuilder,
        /// <summary>
        /// Plank setting for toolmakers was changed.
        /// Byte 0-1: New value
        /// </summary>
        PlanksToolmaker,
        /// <summary>
        /// Steel setting for toolmakers was changed.
        /// Byte 0-1: New value
        /// </summary>
        SteelToolmaker,
        /// <summary>
        /// Steel setting for weaponsmiths was changed.
        /// Byte 0-1: New value
        /// </summary>
        SteelWeaponsmith,
        /// <summary>
        /// Coal setting for steelsmelters was changed.
        /// Byte 0-1: New value
        /// </summary>
        CoalSteelsmelter,
        /// <summary>
        /// Coal setting for goldsmelters was changed.
        /// Byte 0-1: New value
        /// </summary>
        CoalGoldsmelter,
        /// <summary>
        /// Coal setting for weaponsmiths was changed.
        /// Byte 0-1: New value
        /// </summary>
        CoalWeaponsmith,
        /// <summary>
        /// Wheat setting for pigfarms was changed.
        /// Byte 0-1: New value
        /// </summary>
        WheatPigfarm,
        /// <summary>
        /// Wheat setting for mills was changed.
        /// Byte 0-1: New value
        /// </summary>
        WheatMill,
        /// <summary>
        /// Reset food distribution.
        /// </summary>
        ResetFoodDistribution,
        /// <summary>
        /// Reset plank distribution.
        /// </summary>
        ResetPlankDistribution,
        /// <summary>
        /// Reset steel distribution.
        /// </summary>
        ResetSteelDistribution,
        /// <summary>
        /// Reset coal distribution.
        /// </summary>
        ResetCoalDistribution,
        /// <summary>
        /// Reset wheat distribution.
        /// </summary>
        ResetWheatDistribution
    }

    public enum UserAction
    {
        /// <summary>
        /// Unknown user action.
        /// </summary>
        Unknown,
        /// <summary>
        /// Change a game-relevant settings.
        /// Byte 0: The setting (see <see cref="UserActionGameSetting"/>)
        /// See <see cref="UserActionGameSetting"/> for additional bytes/parameters.
        /// </summary>
        ChangeSetting,
        /// <summary>
        /// Start an attack on the given building.
        /// Byte 0-3: Map position of the building
        /// Byte 4-7: Building index (also transferred to be on the safe side)
        /// Byte 8-11: Building player index (also transferred to be on the safe side)
        /// Byte 12-15: Number of total knights to send (see <see cref="Player.TotalKnightsAttacking"/>)
        /// The last value is what the client selected in StartAttack UI window and is limited
        /// to the max possible amount. The server will prepare the attack itself and use this
        /// value as a maximum for the number of knights to send.
        /// </summary>
        Attack,
        /// <summary>
        /// Send a geologist to a given flag.
        /// Byte 0-3: Map position of the flag
        /// Byte 4-7: Flag index (also transferred to be on the safe side)
        /// </summary>
        SendGeologist,
        /// <summary>
        /// Cycle knights (exchange weaker knights with stronger ones).
        /// </summary>
        CycleKnights,
        /// <summary>
        /// Train some knights.
        /// Byte 0: Amount (max 100).
        /// </summary>
        TrainKnights,
        /// <summary>
        /// Place a building at a given spot.
        /// Byte 0-3: Map position
        /// Byte 4: Building type
        /// </summary>
        PlaceBuilding,
        /// <summary>
        /// Demolish an existing building at a given spot.
        /// Byte 0-3: Map position
        /// Byte 4-7: Building index (also transferred to be on the safe side)
        /// </summary>
        DemolishBuilding,
        /// <summary>
        /// Place a flag at a given spot.
        /// Byte 0-3: Map position
        /// </summary>
        PlaceFlag,
        /// <summary>
        /// Demolish an existing flag at a given spot.
        /// Byte 0-3: Map position
        /// Byte 4-7: Flag index (also transferred to be on the safe side)
        /// </summary>
        DemolishFlag,
        /// <summary>
        /// Place a road.
        /// Byte 0-3: Start map position
        /// Byte 4-7: Start flag index (also transferred to be on the safe side)
        /// Byte 8-11: End map position
        /// Byte 12-15: End flag index (may be 0 but must match if there is already a flag)
        /// Byte 16: Number of map positions in between (at least 1).
        /// Byte 17+: Directions starting at start flag. Each direction is given as a 4bit nibble.
        /// So two directions can be transferred as 1 byte (higher nibble is first, then lower nibble).
        /// For x in-between positions the following amount of bytes is needed: ceil((x + 1) / 2).
        /// </summary>
        PlaceRoad,
        /// <summary>
        /// Demolish an existing road at a given spot.
        /// Byte 0-3: Map position
        /// </summary>
        DemolishRoad,
        /// <summary>
        /// Surrender the game.
        /// No parameters.
        /// </summary>
        Surrender,
        /// <summary>
        /// Merge a path.
        /// TODO
        /// </summary>
        MergePaths, // TODO
        /// <summary>
        /// Sets the mode of an inventory.
        /// - Serfs in
        /// - Serfs stop
        /// - Serfs out
        /// - Resources in
        /// - Resources stop
        /// - Resources out
        /// TODO
        /// </summary>
        SetInventoryMode // TODO
    }

    public class UserActionData : INetworkData
    {
        private const int MinDataSize = 9;
        public NetworkDataType Type => NetworkDataType.UserActionData;

        public byte MessageIndex
        {
            get;
            private set;
        } = 0;

        public UInt32 GameTime
        {
            get;
            private set;
        } = 0u;

        public UserAction UserAction
        {
            get;
            private set;
        } = UserAction.Unknown;

        public byte[] Parameters
        {
            get;
            private set;
        }

        public UserActionData()
        {
            // use when parsing the data
        }

        private UserActionData(byte number, uint gameTime, UserAction userAction, byte[] parameters)
        {
            MessageIndex = number;
            GameTime = gameTime;
            UserAction = userAction;
            Parameters = parameters;
        }

        public int Size { get; private set; }

        public INetworkData Parse(byte[] rawData, ref int offset)
        {
            if (rawData.Length - offset < MinDataSize)
                throw new ExceptionFreeserf($"User action data length must be at least {MinDataSize}.");

            MessageIndex = rawData[offset + 2];
            GameTime = BitConverter.ToUInt32(rawData, offset + 3);
            UserAction = (UserAction)rawData[offset + 7];
            int parameterByteCount = rawData[offset + 8];
            Size = MinDataSize + parameterByteCount;

            if (rawData.Length - offset < Size)
                throw new ExceptionFreeserf($"User action data length must be at least {Size}.");

            if (parameterByteCount > 0)
            {
                Parameters = new byte[parameterByteCount];
                Buffer.BlockCopy(rawData, offset + 9, Parameters, 0, Parameters.Length);
            }

            offset += Size;

            return this;
        }

        public void Send(IRemote destination)
        {
            List<byte> rawData = new List<byte>(Size);

            rawData.AddRange(BitConverter.GetBytes((UInt16)Type));
            rawData.Add(MessageIndex);
            rawData.AddRange(BitConverter.GetBytes(GameTime));
            rawData.Add((byte)UserAction);

            if (Parameters != null && Parameters.Length > 0)
            {
                if (Parameters.Length > 255)
                    throw new ArgumentOutOfRangeException("More than 255 parameter bytes are not allowed.");

                rawData.Add((byte)Parameters.Length);
                rawData.AddRange(Parameters);
            }
            else
                rawData.Add(0);

            destination.Send(rawData.ToArray());
        }

        internal static UserActionData CreateChangeSettingUserAction(byte number, Game game, UserActionGameSetting setting, params byte[] values)
        {
            byte[] parameters = new byte[values.Length + 1];
            parameters[0] = (byte)setting;
            if (values.Length > 0)
                Buffer.BlockCopy(values, 0, parameters, 1, values.Length);
            return new UserActionData(number, game.GameTime, UserAction.ChangeSetting, parameters);
        }

        internal static UserActionData CreateChangeSettingUserAction(byte number, Game game, UserActionGameSetting setting, UInt16 value)
        {
            return new UserActionData(number, game.GameTime, UserAction.ChangeSetting, CreateParameters((byte)setting, value));
        }

        internal static UserActionData CreateAttackUserAction(byte number, Game game, MapPos mapPosition, int numberOfKnightsToSend)
        {
            var building = game.GetBuildingAtPosition(mapPosition);

            return new UserActionData(number, game.GameTime, UserAction.Attack,
                CreateParameters
                (
                    mapPosition,
                    building.Index,
                    building.Player,
                    (UInt32)numberOfKnightsToSend
                )
            );
        }

        internal static UserActionData CreateSendGeologistUserAction(byte number, Game game, MapPos mapPosition)
        {
            return new UserActionData(number, game.GameTime, UserAction.SendGeologist,
                CreateParameters
                (
                    mapPosition,
                    game.GetFlagAtPosition(mapPosition).Index
                )
            );
        }

        internal static UserActionData CreateCycleKnightsUserAction(byte number, Game game)
        {
            return new UserActionData(number, game.GameTime, UserAction.CycleKnights, null);
        }

        internal static UserActionData CreateTrainKnightsUserAction(byte number, Game game, byte amount)
        {
            return new UserActionData(number, game.GameTime, UserAction.TrainKnights, new byte[1] { amount });
        }

        internal static UserActionData CreatePlaceBuildingUserAction(byte number, Game game, MapPos mapPosition)
        {
            return new UserActionData(number, game.GameTime, UserAction.PlaceBuilding,
                CreateParameters
                (
                    mapPosition,
                    game.GetBuildingAtPosition(mapPosition).BuildingType
                )
            );
        }

        internal static UserActionData CreateDemolishBuildingUserAction(byte number, Game game, MapPos mapPosition)
        {
            return new UserActionData(number, game.GameTime, UserAction.DemolishBuilding,
                CreateParameters
                (
                    mapPosition,
                    game.GetBuildingAtPosition(mapPosition).Index
                )
            );
        }

        internal static UserActionData CreatePlaceFlagUserAction(byte number, Game game, MapPos mapPosition)
        {
            return new UserActionData(number, game.GameTime, UserAction.PlaceFlag, BitConverter.GetBytes(mapPosition));
        }

        internal static UserActionData CreateDemolishFlagUserAction(byte number, Game game, MapPos mapPosition)
        {
            return new UserActionData(number, game.GameTime, UserAction.DemolishFlag,
                CreateParameters
                (
                    mapPosition,
                    game.GetFlagAtPosition(mapPosition).Index
                )
            );
        }

        internal static UserActionData CreatePlaceRoadUserAction(byte number, Game game, Road road, bool endFlagWasJustCreated)
        {
            int numDirectionPairEntries = ((int)road.Length + 1) / 2;
            var directionPairs = new byte[numDirectionPairEntries];
            var roadDirections = road.Directions.Reverse().ToArray();

            for (int i = 0; i < numDirectionPairEntries; ++i)
            {
                directionPairs[i] = (byte)((int)roadDirections[i * 2] << 4);

                if (i * 2 + 1 < roadDirections.Length)
                    directionPairs[i] |= (byte)roadDirections[i * 2 + 1];
            }

            return new UserActionData(number, game.GameTime, UserAction.PlaceRoad,
                MergeParameters
                (
                    CreateParameters
                    (
                        road.StartPosition,
                        game.GetFlagAtPosition(road.StartPosition).Index,
                        road.EndPosition,
                        endFlagWasJustCreated ? 0u : game.GetFlagAtPosition(road.EndPosition).Index,
                        (byte)(road.Length - 1)
                    ),
                    directionPairs
                )
            );
        }

        internal static UserActionData CreateDemolishRoadUserAction(byte number, Game game, MapPos mapPosition)
        {
            return new UserActionData(number, game.GameTime, UserAction.DemolishRoad, BitConverter.GetBytes(mapPosition));
        }

        internal static UserActionData CreateSurrenderUserAction(byte number, Game game)
        {
            return new UserActionData(number, game.GameTime, UserAction.Surrender, null);
        }

        private static byte[] CreateParameters(params object[] parameters)
        {
            var result = new List<byte>(20);

            foreach (var param in parameters)
            {
                if (param.GetType().IsEnum)
                    result.Add((byte)param);
                else if (param is byte b)
                    result.Add(b);
                else
                {
                    result.AddRange(param switch
                    {
                        UInt16 us => BitConverter.GetBytes(us),
                        UInt32 ul => BitConverter.GetBytes(ul),
                        _ => throw new ArgumentException($"Invalid parameter type {param.GetType()}.")
                    });
                }
            }

            return result.ToArray();
        }

        private static byte[] MergeParameters(byte[] parameters1, byte[] parameters2)
        {
            var result = new List<byte>(parameters1.Length + 20);

            result.AddRange(parameters1);
            result.AddRange(parameters2);

            return result.ToArray();
        }

        private void EnsureParameters(int size)
        {
            if (Parameters == null)
                throw new ArgumentNullException();

            if (Parameters.Length < size)
                throw new ArgumentOutOfRangeException();
        }

        private ResponseType ApplySettingToGame(Game game, Player source, UserActionGameSetting setting)
        {
            // Note: Parameters[0] contains the setting so start at offset 1 for setting parameters.

            try
            {
                switch (setting)
                {
                    case UserActionGameSetting.ToolPriority:
                        {
                            EnsureParameters(4);
                            var toolIndex = Parameters[1];
                            if (toolIndex == 0xff)
                            {
                                source.ResetToolPriority();
                                return ResponseType.Ok;
                            }
                            if (toolIndex >= 9)
                                return ResponseType.BadRequest;
                            var value = BitConverter.ToUInt16(Parameters, 2);
                            source.SetToolPriority(toolIndex, value);
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.FlagPriority:
                        {
                            EnsureParameters(3);
                            var resourceIndex = Parameters[1];
                            if (resourceIndex == 0xff)
                            {
                                source.ResetFlagPriority();
                                return ResponseType.Ok;
                            }
                            if (resourceIndex > (byte)Resource.Type.MaxValue)
                                return ResponseType.BadRequest;
                            var value = Parameters[2];
                            bool up = (value & 0x01) != 0;
                            bool toEnd = (value & 0x02) != 0;
                            source.MoveTransportItemPriority(up, toEnd, source.GetFlagPriorities(), (Resource.Type)resourceIndex);
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.InventoryPriority:
                        {
                            EnsureParameters(3);
                            var resourceIndex = Parameters[1];
                            if (resourceIndex == 0xff)
                            {
                                source.ResetInventoryPriority();
                                return ResponseType.Ok;
                            }
                            if (resourceIndex > (byte)Resource.Type.MaxValue)
                                return ResponseType.BadRequest;
                            var value = Parameters[2];
                            bool up = (value & 0x01) != 0;
                            bool toEnd = (value & 0x02) != 0;
                            source.MoveTransportItemPriority(up, toEnd, source.GetInventoryPriorities(), (Resource.Type)resourceIndex);
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.KnightOccupation:
                        {
                            EnsureParameters(5);
                            for (int i = 0; i < 4; ++i)
                            {
                                if ((Parameters[1 + i] & 0x0f) > 0x04)
                                    return ResponseType.BadRequest;
                                if ((Parameters[1 + i] & 0xf0) > 0x40)
                                    return ResponseType.BadRequest;
                            }
                            source.SetKnightOccupation(new byte[4] { Parameters[1], Parameters[2], Parameters[3], Parameters[4] });
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.SendStrongest:
                        source.SendStrongest = true;
                        return ResponseType.Ok;
                    case UserActionGameSetting.SendWeakest:
                        source.SendStrongest = false;
                        return ResponseType.Ok;
                    case UserActionGameSetting.SerfToKnightRate:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.SerfToKnightRate = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.CastleKnightsWanted:
                        {
                            EnsureParameters(2);
                            var amount = Parameters[1];
                            if (amount < 1 || amount > 99)
                                return ResponseType.BadRequest;
                            source.SetCastleKnightsWanted(amount);
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.FoodStonemine:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.FoodStonemine = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.FoodCoalmine:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.FoodCoalmine = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.FoodIronmine:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.FoodIronmine = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.FoodGoldmine:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.FoodGoldmine = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.PlanksConstruction:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.PlanksConstruction = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.PlanksBoatbuilder:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.PlanksBoatbuilder = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.PlanksToolmaker:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.PlanksToolmaker = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.SteelToolmaker:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.SteelToolmaker = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.SteelWeaponsmith:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.SteelWeaponsmith = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.CoalSteelsmelter:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.CoalSteelsmelter = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.CoalGoldsmelter:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.CoalGoldsmelter = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.CoalWeaponsmith:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.CoalWeaponsmith = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.WheatPigfarm:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.WheatPigfarm = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.WheatMill:
                        {
                            EnsureParameters(3);
                            var value = BitConverter.ToUInt16(Parameters, 1);
                            source.WheatMill = value;
                            return ResponseType.Ok;
                        }
                    case UserActionGameSetting.ResetFoodDistribution:
                        source.ResetFoodPriority();
                        return ResponseType.Ok;
                    case UserActionGameSetting.ResetPlankDistribution:
                        source.ResetPlanksPriority();
                        return ResponseType.Ok;
                    case UserActionGameSetting.ResetSteelDistribution:
                        source.ResetSteelPriority();
                        return ResponseType.Ok;
                    case UserActionGameSetting.ResetCoalDistribution:
                        source.ResetCoalPriority();
                        return ResponseType.Ok;
                    case UserActionGameSetting.ResetWheatDistribution:
                        source.ResetWheatPriority();
                        return ResponseType.Ok;
                }
            }
            catch
            {
                return ResponseType.BadRequest;
            }

            return ResponseType.BadRequest;
        }

        /// <summary>
        /// Applies a user action to the game.
        /// 
        /// Note that bad state has to be caught before.
        /// I.e. user input disallowing or game pause.
        /// </summary>
        public ResponseType ApplyToGame(Game game, uint sourcePlayer)
        {
            var source = game.GetPlayer(sourcePlayer);

            try
            {
                switch (UserAction)
                {
                    case UserAction.ChangeSetting:
                        EnsureParameters(1);
                        var setting = (UserActionGameSetting)Parameters[0];
                        return ApplySettingToGame(game, source, setting);
                    case UserAction.Attack:
                        {
                            EnsureParameters(16);
                            MapPos buildingPosition = BitConverter.ToUInt32(Parameters, 0);
                            MapPos buildingIndex = BitConverter.ToUInt32(Parameters, 4);
                            MapPos buildingPlayerIndex = BitConverter.ToUInt32(Parameters, 8);
                            var building = game.GetBuildingAtPosition(buildingPosition);
                            if (building == null || building.Index != buildingIndex || building.Player == source.Index || building.Player != buildingPlayerIndex)
                                return ResponseType.BadRequest;
                            MapPos maxKnightsToSend = BitConverter.ToUInt32(Parameters, 12);
                            if (maxKnightsToSend == 0 || maxKnightsToSend > int.MaxValue)
                                return ResponseType.BadRequest;
                            if (!source.PrepareAttack(buildingPosition, (int)maxKnightsToSend))
                                return ResponseType.Failed;
                            if (source.TotalKnightsAttacking != maxKnightsToSend)
                                return ResponseType.Failed;
                            source.StartAttack();
                            return ResponseType.Ok;
                        }
                    case UserAction.SendGeologist:
                        {
                            EnsureParameters(8);
                            // The player is determined internally by the source flag's owner.
                            MapPos flagPosition = BitConverter.ToUInt32(Parameters, 0);
                            MapPos flagIndex = BitConverter.ToUInt32(Parameters, 4);
                            var flag = game.GetFlagAtPosition(flagPosition);
                            if (flag == null || flag.Index != flagIndex || flag.Player != source.Index)
                                return ResponseType.BadRequest;
                            game.SendGeologist(flag);
                            return ResponseType.Ok;
                        }
                    case UserAction.CycleKnights:
                        source.CycleKnights();
                        return ResponseType.Ok;
                    case UserAction.TrainKnights:
                        {
                            EnsureParameters(1);
                            byte amount = Parameters[0];

                            if (amount == 0 || amount > 100)
                                return ResponseType.BadRequest;

                            source.PromoteSerfsToKnights(amount);
                            return ResponseType.Ok;
                        }
                    case UserAction.PlaceBuilding:
                        {
                            EnsureParameters(5);
                            MapPos mapPosition = BitConverter.ToUInt32(Parameters, 0);
                            Building.Type type = (Building.Type)Parameters[4];

                            if (type == Building.Type.None)
                                return ResponseType.BadRequest;

                            return type switch
                            {
                                Building.Type.Castle => game.BuildCastle(mapPosition, source),
                                _ => game.BuildBuilding(mapPosition, type, source)
                            } ? ResponseType.Ok : ResponseType.Failed;
                        }
                    case UserAction.DemolishBuilding:
                        {
                            EnsureParameters(8);
                            MapPos buildingPosition = BitConverter.ToUInt32(Parameters, 0);
                            MapPos buildingIndex = BitConverter.ToUInt32(Parameters, 4);
                            var building = game.GetBuildingAtPosition(buildingPosition);
                            if (building == null || building.Index != buildingIndex || building.Player != source.Index)
                                return ResponseType.BadRequest;
                            return game.DemolishBuilding(buildingPosition, source) ? ResponseType.Ok : ResponseType.Failed;
                        }
                    case UserAction.PlaceFlag:
                        {
                            EnsureParameters(4);
                            MapPos mapPosition = BitConverter.ToUInt32(Parameters, 0);
                            return game.BuildFlag(mapPosition, source) ? ResponseType.Ok : ResponseType.Failed;
                        }
                    case UserAction.DemolishFlag:
                        {
                            EnsureParameters(8);
                            MapPos flagPosition = BitConverter.ToUInt32(Parameters, 0);
                            MapPos flagIndex = BitConverter.ToUInt32(Parameters, 4);
                            var flag = game.GetFlagAtPosition(flagPosition);
                            if (flag == null || flag.Index != flagIndex || flag.Player != source.Index)
                                return ResponseType.BadRequest;
                            return game.DemolishFlag(flagPosition, source) ? ResponseType.Ok : ResponseType.Failed;
                        }
                    case UserAction.PlaceRoad:
                        {
                            EnsureParameters(18);
                            MapPos startFlagPosition = BitConverter.ToUInt32(Parameters, 0);
                            MapPos startFlagIndex = BitConverter.ToUInt32(Parameters, 4);
                            MapPos endFagPosition = BitConverter.ToUInt32(Parameters, 8);
                            MapPos endFlagIndex = BitConverter.ToUInt32(Parameters, 12);
                            var startFlag = game.GetFlagAtPosition(startFlagPosition);
                            if (startFlag == null || startFlag.Index != startFlagIndex || startFlag.Player != source.Index)
                                return ResponseType.BadRequest;
                            var endFlag = game.GetFlagAtPosition(endFagPosition);
                            if (endFlag == null && endFlagIndex != 0u)
                                return ResponseType.BadRequest;
                            if (endFlag != null && (endFlag.Index != endFlagIndex || endFlag.Player != source.Index))
                                return ResponseType.BadRequest;
                            var positionsInBetween = Parameters[16];
                            if (positionsInBetween < 1)
                                return ResponseType.BadRequest;
                            int numDirectionEntries = (positionsInBetween + 2) / 2; // +1 for end position, +1 for ceiling the value
                            if (Parameters.Length != 17 + numDirectionEntries)
                                return ResponseType.BadRequest;
                            Road road = new Road();
                            road.Start(startFlagPosition);
                            for (int i = 0; i < positionsInBetween + 1; ++i)
                            {
                                var direction = (Direction)((Parameters[17 + i / 2] >> ((1 - (i % 2)) * 4)) & 0x0f);

                                if (direction == Direction.None)
                                    return ResponseType.BadRequest;

                                if (!road.Extend(game.Map, direction))
                                    return ResponseType.Failed;
                            }
                            if (road.EndPosition != endFagPosition)
                                return ResponseType.BadRequest;
                            return game.BuildRoad(road, source, true) ? ResponseType.Ok : ResponseType.Failed;
                        }
                    case UserAction.DemolishRoad:
                        {
                            EnsureParameters(4);
                            MapPos mapPosition = BitConverter.ToUInt32(Parameters, 0);
                            return game.DemolishRoad(mapPosition, source) ? ResponseType.Ok : ResponseType.Failed;
                        }
                    case UserAction.Surrender:
                        // TODO
                        return ResponseType.Ok;
                }
            }
            catch
            {
                return ResponseType.BadRequest;
            }

            return ResponseType.BadRequest;
        }
    }
}
