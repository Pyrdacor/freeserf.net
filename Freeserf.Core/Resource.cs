/*
 * Resource.cs - Resources related definitions.
 *
 * Copyright (C) 2014  Wicked_Digger <wicked_digger@mail.ru>
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

namespace Freeserf.Resource
{
    public enum Type : sbyte
    {
        MinValue = None,
        MaxValue = GroupFood,
        MaxValueWithoutFoodGroup = Shield,

        None = -1,

        Fish = 0,
        Pig,
        Meat,
        Wheat,
        Flour,
        Bread,
        Lumber,
        Plank,
        Boat,
        Stone,
        IronOre,
        Steel,
        Coal,
        GoldOre,
        GoldBar,
        Shovel,
        Hammer,
        Rod,
        Cleaver,
        Scythe,
        Axe,
        Saw,
        Pick,
        Pincer,
        Sword,
        Shield,

        GroupFood
    }
}