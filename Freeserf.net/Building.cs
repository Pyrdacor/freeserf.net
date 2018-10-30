using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class Building : GameObject
    {
        public enum Type
        {
            None = 0,
            Fisher,
            Lumberjack,
            Boatbuilder,
            Stonecutter,
            StoneMine,
            CoalMine,
            IronMine,
            GoldMine,
            Forester,
            Stock,
            Hut,
            Farm,
            Butcher,
            PigFarm,
            Mill,
            Baker,
            Sawmill,
            SteelSmelter,
            ToolMaker,
            WeaponSmith,
            Tower,
            Fortress,
            GoldSmelter,
            Castle
        }

        public Type BuildingType { get; private set; }
    }
}
