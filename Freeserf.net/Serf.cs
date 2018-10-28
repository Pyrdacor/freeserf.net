using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class Serf : GameObject
    {
        public enum Type
        {
            None = -1,
            Transporter = 0,
            Sailor,
            Digger,
            Builder,
            TransporterInventory,
            Lumberjack,
            Sawmiller,
            Stonecutter,
            Forester,
            Miner,
            Smelter,
            Fisher,
            PigFarmer,
            Butcher,
            Farmer,
            Miller,
            Baker,
            BoatBuilder,
            Toolmaker,
            WeaponSmith,
            Geologist,
            Generic,
            Knight0,
            Knight1,
            Knight2,
            Knight3,
            Knight4,
            Dead
        }

        /* The term FREE is used loosely in the following
         names to denote a state where the serf is not
         bound to a road or a flag. */
        public enum State
        {
            Null = 0,
            IdleInStock,
            Walking,
            Transporting,
            EnteringBuilding,
            LeavingBuilding, /* 5 */
            ReadyToEnter,
            ReadyToLeave,
            Digging,
            Building,
            BuildingCastle, /* 10 */
            MoveResourceOut,
            WaitForResourceOut,
            DropResourceOut,
            Delivering,
            ReadyToLeaveInventory, /* 15 */
            FreeWalking,
            Logging,
            PlanningLogging,
            PlanningPlanting,
            Planting, /* 20 */
            PlanningStoneCutting,
            StoneCutterFreeWalking,
            StoneCutting,
            Sawing,
            Lost, /* 25 */
            LostSailor,
            FreeSailing,
            EscapeBuilding,
            Mining,
            Smelting, /* 30 */
            PlanningFishing,
            Fishing,
            PlanningFarming,
            Farming,
            Milling, /* 35 */
            Baking,
            PigFarming,
            Butchering,
            MakingWeapon,
            MakingTool, /* 40 */
            BuildingBoat,
            LookingForGeoSpot,
            SamplingGeoSpot,
            KnightEngagingBuilding,
            KnightPrepareAttacking, /* 45 */
            KnightLeaveForFight,
            KnightPrepareDefending,
            KnightAttacking,
            KnightDefending,
            KnightAttackingVictory, /* 50 */
            KnightAttackingDefeat,
            KnightOccupyEnemyBuilding,
            KnightFreeWalking,
            KnightEngageDefendingFree,
            KnightEngageAttackingFree, /* 55 */
            KnightEngageAttackingFreeJoin,
            KnightPrepareAttackingFree,
            KnightPrepareDefendingFree,
            KnightPrepareDefendingFreeWait,
            KnightAttackingFree, /* 60 */
            KnightDefendingFree,
            KnightAttackingVictoryFree,
            KnightDefendingVictoryFree,
            KnightAttackingFreeWait,
            KnightLeaveForWalkToFight, /* 65 */
            IdleOnPath,
            WaitIdleOnPath,
            WakeAtFlag,
            WakeOnPath,
            DefendingHut, /* 70 */
            DefendingTower,
            DefendingFortress,
            Scatter,
            FinishedBuilding,
            DefendingCastle, /* 75 */

            /* Additional state: goes at the end to ease loading of
             original save game. */
            KnightAttackingDefeatFree
        }
    }
}
