﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIEvtcParser.EncounterLogic
{
    internal class River : HallOfChains
    {
        public River(int triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>
            {
                new HitOnPlayerMechanic(48272, "Bombshell", new MechanicPlotlySetting("circle","rgb(255,125,0)"),"Bomb Hit", "Hit by Hollowed Bomber Exlosion", "Hit by Bomb", 0 ),
                new HitOnPlayerMechanic(47258, "Timed Bomb", new MechanicPlotlySetting("square","rgb(255,125,0)"),"Stun Bomb", "Stunned by Mini Bomb", "Stun Bomb", 0, (de, log) => !de.To.HasBuff(log, 1122, de.Time)),
            }
            );
            GenericFallBackMethod = FallBackMethod.None;
            Extension = "river";
            Targetless = true;
            Icon = "https://wiki.guildwars2.com/images/thumb/7/7b/Gold_River_of_Souls_Trophy.jpg/220px-Gold_River_of_Souls_Trophy.jpg";
            EncounterCategoryInformation.InSubCategoryOrder = 1;
        }

        protected override CombatReplayMap GetCombatMapInternal(ParsedEvtcLog log)
        {
            return new CombatReplayMap("https://i.imgur.com/BMqQKqb.png",
                            (1000, 387),
                            (-12201, -4866, 7742, 2851)/*,
                            (-21504, -12288, 24576, 12288),
                            (19072, 15484, 20992, 16508)*/);
        }

        protected override List<ArcDPSEnums.TrashID> GetTrashMobsIDS()
        {
            return new List<ArcDPSEnums.TrashID>
            {
                ArcDPSEnums.TrashID.Enervator,
                ArcDPSEnums.TrashID.HollowedBomber,
                ArcDPSEnums.TrashID.RiverOfSouls,
                ArcDPSEnums.TrashID.SpiritHorde1,
                ArcDPSEnums.TrashID.SpiritHorde2,
                ArcDPSEnums.TrashID.SpiritHorde3
            };
        }

        internal override void CheckSuccess(CombatData combatData, AgentData agentData, FightData fightData, HashSet<AgentItem> playerAgents)
        {
            base.CheckSuccess(combatData, agentData, fightData, playerAgents);
            if (!fightData.Success)
            {
                AgentItem desmina = agentData.GetNPCsByID((int)ArcDPSEnums.TargetID.Desmina).FirstOrDefault();
                if (desmina == null)
                {
                    throw new MissingKeyActorsException("Desmina not found");
                }
                ExitCombatEvent ooc = combatData.GetExitCombatEvents(desmina).LastOrDefault();
                if (ooc != null)
                {
                    long time = 0;
                    foreach (NPC mob in TrashMobs)
                    {
                        time = Math.Max(mob.LastAware, time);
                    }
                    DespawnEvent dspwn = combatData.GetDespawnEvents(desmina).LastOrDefault();
                    if (time != 0 && dspwn == null && time + 500 <= desmina.LastAware)
                    {
                        if (!AtLeastOnePlayerAlive(combatData, fightData, time, playerAgents))
                        {
                            return;
                        }
                        fightData.SetSuccess(true, time);
                    }
                }
            }
        }

        internal override void EIEvtcParse(FightData fightData, AgentData agentData, List<CombatItem> combatData, List<Player> playerList)
        {
            agentData.AddCustomAgent(fightData.FightStart, fightData.FightEnd, AgentItem.AgentType.NPC, "River of Souls", "", (int)ArcDPSEnums.TargetID.DummyTarget);
            // The walls and bombers spawn at the start of the encounter, we fix it by overriding their first aware to the first velocity change event
            var agentsToOverrideFirstAware = new List<AgentItem>(agentData.GetNPCsByID((int)ArcDPSEnums.TrashID.RiverOfSouls));
            agentsToOverrideFirstAware.AddRange(agentData.GetNPCsByID((int)ArcDPSEnums.TrashID.HollowedBomber));
            bool sortCombatList = false;
            foreach (AgentItem agentToOverrideFirstAware in agentsToOverrideFirstAware)
            {
                CombatItem firstMovement = combatData.FirstOrDefault(x => x.IsStateChange == ArcDPSEnums.StateChange.Velocity && x.SrcAgent == agentToOverrideFirstAware.Agent && x.DstAgent != 0);
                if (firstMovement != null)
                {
                    // update start
                    agentToOverrideFirstAware.OverrideAwareTimes(firstMovement.Time - ParserHelper.ServerDelayConstant, agentToOverrideFirstAware.LastAware);
                    foreach (CombatItem c in combatData)
                    {
                        if (c.SrcAgent == agentToOverrideFirstAware.Agent && (c.IsStateChange == ArcDPSEnums.StateChange.Position || c.IsStateChange == ArcDPSEnums.StateChange.Rotation) && c.Time <= agentToOverrideFirstAware.FirstAware)
                        {
                            sortCombatList = true;
                            c.OverrideTime(agentToOverrideFirstAware.FirstAware);
                        }
                    }
                }
            }
            // make sure the list is still sorted by time after overrides
            if (sortCombatList)
            {
                combatData.Sort((x, y) => x.Time.CompareTo(y.Time));
            }
            ComputeFightTargets(agentData, combatData);
            AgentItem desmina = agentData.GetNPCsByID((int)ArcDPSEnums.TargetID.Desmina).FirstOrDefault();
            if (desmina != null)
            {
                playerList.Add(new Player(desmina, "Desmina", GetNPCIcon((int)ArcDPSEnums.TargetID.Desmina)));
            }
        }

        internal override void ComputePlayerCombatReplayActors(Player p, ParsedEvtcLog log, CombatReplay replay)
        {
            // TODO bombs dual following circle actor (one growing, other static) + dual static circle actor (one growing with min radius the final radius of the previous, other static). Missing buff id
        }

        internal override void ComputeNPCCombatReplayActors(NPC target, ParsedEvtcLog log, CombatReplay replay)
        {
            int start = (int)replay.TimeOffsets.start;
            int end = (int)replay.TimeOffsets.end;
            switch (target.ID)
            {
                case (int)ArcDPSEnums.TrashID.HollowedBomber:
                    var bomberman = target.GetCastEvents(log, 0, log.FightData.FightEnd).Where(x => x.SkillId == 48272).ToList();
                    foreach (AbstractCastEvent bomb in bomberman)
                    {
                        int startCast = (int)bomb.Time;
                        int endCast = (int)bomb.EndTime;
                        int expectedEnd = Math.Max(startCast + bomb.ExpectedDuration, endCast);
                        replay.Decorations.Add(new CircleDecoration(true, 0, 480, (startCast, endCast), "rgba(180,250,0,0.3)", new AgentConnector(target)));
                        replay.Decorations.Add(new CircleDecoration(true, expectedEnd, 480, (startCast, endCast), "rgba(180,250,0,0.3)", new AgentConnector(target)));
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.RiverOfSouls:
                    if (replay.Rotations.Count > 0)
                    {
                        replay.Decorations.Add(new FacingRectangleDecoration((start, end), new AgentConnector(target), replay.PolledRotations, 160, 390, "rgba(255,100,0,0.5)"));
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.Enervator:
                // TODO Line actor between desmina and enervator. Missing skillID
                case (int)ArcDPSEnums.TrashID.SpiritHorde1:
                case (int)ArcDPSEnums.TrashID.SpiritHorde2:
                case (int)ArcDPSEnums.TrashID.SpiritHorde3:
                    break;
                default:
                    break;
            }

        }

        internal override string GetLogicName(ParsedEvtcLog log)
        {
            return "River of Souls";
        }
    }
}
