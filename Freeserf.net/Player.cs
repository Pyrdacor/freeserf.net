/*
 * Player.cs - Player related functions
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    using MapPos = UInt32;
    using Messages = Queue<Message>;

    public class Message
    {
        public enum Type
        {
            None = 0,
            UnderAttack = 1,
            LoseFight = 2,
            WinFight = 3,
            MineEmpty = 4,
            CallToLocation = 5,
            KnightOccupied = 6,
            NewStock = 7,
            LostLand = 8,
            LostBuildings = 9,
            EmergencyActive = 10,
            EmergencyNeutral = 11,
            FoundGold = 12,
            FoundIron = 13,
            FoundCoal = 14,
            FoundStone = 15,
            CallToMenu = 16,
            ThirtyMinutesSinceSave = 17,
            OneHourSinceSave = 18,
            CallToStock = 19
        }

        public Type MessageType { get; set; } = Type.None;
        public MapPos Pos { get; set; } = 0;
        public uint Data { get; set; } = 0;
    }

    public class Player : GameObject
    {
    }
}
