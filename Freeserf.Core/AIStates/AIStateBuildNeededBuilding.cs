using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf.AIStates
{
    class AIStateBuildNeededBuilding : AIState
    {
        enum CheckResult
        {
            NotNeeded,
            Needed,
            NeededButNoSpecialistOrRes,
            NeededButNoGenerics
        }

        public override void Update(AI ai, Game game, Player player, PlayerInfo playerInfo, int tick)
        {
            CheckResult result = CheckResult.NotNeeded;
            int intelligence = (int)playerInfo.Intelligence;

            for (int i = (int)Building.Type.None + 1; i < (int)Building.Type.Castle; ++i)
            {
                var type = (Building.Type)i;

                result = CheckBuilding(ai, game, player, intelligence, type);

                if (result == CheckResult.Needed)
                {
                    GoToState(ai, AI.State.BuildBuilding, type);
                    return;
                }
                else if (result == CheckResult.NeededButNoGenerics)
                {
                    // we can't do much but wait
                    // TODO: Maybe lower the "generic to knights" ratio?
                    continue;
                }
                else if (result == CheckResult.NeededButNoSpecialistOrRes)
                {
                    if (type == Building.Type.Hut ||
                        type == Building.Type.Tower ||
                        type == Building.Type.Fortress)
                    {
                        GoToState(ai, AI.State.CraftWeapons);
                        return;
                    }
                    else
                    {
                        var neededForBuilding = Building.Requests[(int)type];

                        if (neededForBuilding.ResType2 == Resource.Type.None)
                        {
                            GoToState(ai, AI.State.CraftTool, neededForBuilding.ResType1);
                            return;
                        }
                        else
                        {
                            // TODO: In some cases only one tool is missing so we should
                            //       not craft the other one. Or the tools are in different
                            //       inventories. We have to optimize this later.

                            Kill(ai);
                            ai.PushStates
                            (
                                ai.CreateState(AI.State.CraftTool, neededForBuilding.ResType1),
                                ai.CreateState(AI.State.CraftTool, neededForBuilding.ResType2)
                            );
                            return;
                        }
                    }
                }
            }

            Kill(ai);
        }

        CheckResult NeedBuilding(Game game, Player player, Building.Type type)
        {
            var neededForBuilding = Building.Requests[(int)type];

            int inventory = game.FindInventoryWithValidSpecialist(player, neededForBuilding.SerfType,
                neededForBuilding.ResType1, neededForBuilding.ResType2);

            if (inventory != -1)
            {
                if (inventory > 0xffff)
                    return CheckResult.NeededButNoGenerics;

                return CheckResult.Needed;
            }

            return CheckResult.NeededButNoSpecialistOrRes;
        }

        bool CanBuildMilitary(AI ai, Game game, Player player)
        {
            if (ai.MaxMilitaryBuildings == -1)
                return true;

            var militaryBuildings = game.GetPlayerBuildings(player).Where(b =>
                b.BuildingType == Building.Type.Hut ||
                b.BuildingType == Building.Type.Tower ||
                b.BuildingType == Building.Type.Fortress);

            return militaryBuildings.Count() < ai.MaxMilitaryBuildings;
        }

        CheckResult CheckBuilding(AI ai, Game game, Player player, int intelligence, Building.Type type)
        {
            const int minutes = 60 * Global.TICKS_PER_SEC;
            int count = game.GetPlayerBuildings(player, type).Count();

            if (count < 1)
            {
                switch (type)
                {
                    // these are essential buildings
                    case Building.Type.Lumberjack:
                    case Building.Type.Sawmill:
                    case Building.Type.Stonecutter:
                        return NeedBuilding(game, player, type);
                    case Building.Type.Hut:
                        if (CanBuildMilitary(ai, game, player) && !player.EmergencyProgramActive && ai.HasResourcesForBuilding(game, type) &&
                            ai.GameTime > (60 - intelligence / 2 - ai.MilitaryFocus * 15) * Global.TICKS_PER_SEC)
                        {
                            return NeedBuilding(game, player, type);
                        }
                        break;
                }
            }

            switch (type)
            {
                case Building.Type.Lumberjack:
                    if (count < 2 && ai.GameTime > (30 - ai.ConstructionMaterialFocus * 13 + game.RandomInt() % 6) * minutes)
                    {
                        return NeedBuilding(game, player, type);
                    }
                    else if (count < 3 && ai.GameTime > (45 - ai.ConstructionMaterialFocus * 13 + game.RandomInt() % 11) * minutes)
                    {
                        return NeedBuilding(game, player, type);
                    }
                    // TODO ...
                    break;
                case Building.Type.ToolMaker:
                    if (count < 1 && ai.GameTime > 30 * Global.TICKS_PER_SEC + (2 - intelligence / 20) * minutes)
                        return NeedBuilding(game, player, type);
                    // TODO ...
                    break;
                case Building.Type.Hut:
                    if (CanBuildMilitary(ai, game, player) && count < player.GetLandArea() / 120 + ai.MilitaryFocus + ai.GameTime / (30 * minutes) - 1 &&
                        ai.GameTime > (90 - intelligence - ai.MilitaryFocus * 15) * Global.TICKS_PER_SEC)
                    {
                        return NeedBuilding(game, player, type);
                    }
                    // TODO ...
                    break;
                // TODO ...
            }

            return CheckResult.NotNeeded;
        }
    }
}
