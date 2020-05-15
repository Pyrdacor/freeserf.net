/*
 * Notification.cs - Player notification messages
 *
 * Copyright (C) 2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf
{
    using Serialize;
    using MapPos = System.UInt32;

    [DataClass]
    public class Notification
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

        public Type NotificationType { get; set; } = Type.None;
        public MapPos Position { get; set; } = 0;
        public uint Data { get; set; } = 0;
    }
}
