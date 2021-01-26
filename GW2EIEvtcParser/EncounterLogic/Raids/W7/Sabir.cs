﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIEvtcParser.EncounterLogic
{
    internal class Sabir : TheKeyOfAhdashim
    {
        public Sabir(int triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>()
            {
                new SkillOnPlayerMechanic(56202, "Dire Drafts", new MechanicPlotlySetting("circle","rgb(255,120,0)"), "B.Tornado", "Hit by big tornado", "Big Tornado Hit", 500, (de, log) => de.HasDowned || de.HasKilled),
                new SkillOnPlayerMechanic(56643, "Unbridled Tempest", new MechanicPlotlySetting("hexagon","rgb(255,0,150)"), "Shockwave", "Hit by Shockwave", "Shockwave Hit", 0, (de, log) => de.HasDowned || de.HasKilled),
                new SkillOnPlayerMechanic(56372, "Fury of the Storm", new MechanicPlotlySetting("circle","rgb(50,0,200)"), "Arena AoE", "Hit by Arena wide AoE", "Arena AoE hit", 0, (de, log) => de.HasDowned || de.HasKilled ),
                new HitOnPlayerMechanic(56055, "Dynamic Deterrent", new MechanicPlotlySetting("y-up-open","rgb(255,0,150)"), "Pushed", "Pushed by rotating breakbar", "Pushed", 0, (de, log) => !de.To.HasBuff(log, 1122, de.Time)),
                new EnemyCastStartMechanic(56349, "Regenerative Breakbar", new MechanicPlotlySetting("diamond-wide","rgb(255,0,255)"), "Reg.Breakbar","Regenerating Breakbar", "Regenerative Breakbar", 0),
                new EnemyBuffRemoveMechanic(56100, "Regenerative Breakbar Broken", new MechanicPlotlySetting("diamond-wide","rgb(0,160,150)"), "Reg.Breakbar Brkn", "Regenerative Breakbar Broken", "Regenerative Breakbar Broken", 2000),
                new EnemyBuffApplyMechanic(56172, "Rotating Breakbar", new MechanicPlotlySetting("diamond-tall","rgb(255,0,255)"), "Rot.Breakbar","Rotating Breakbar", "Rotating Breakbar", 0),
                new EnemyBuffRemoveMechanic(56172, "Rotating Breakbar Broken", new MechanicPlotlySetting("diamond-tall","rgb(0,160,150)"), "Rot.Breakbar Brkn","Rotating Breakbar Broken", "Rotating Breakbar Broken", 0),
            });
            // rotating cc 56403
            // interesting stuff 56372 (big AoE?)
            Extension = "sabir";
            Icon = "https://wiki.guildwars2.com/images/f/fc/Mini_Air_Djinn.png";
            EncounterCategoryInformation.InSubCategoryOrder = 0;
        }

        protected override List<ArcDPSEnums.TrashID> GetTrashMobsIDS()
        {
            return new List<ArcDPSEnums.TrashID>()
            {
                ArcDPSEnums.TrashID.ParalyzingWisp,
                ArcDPSEnums.TrashID.VoltaicWisp,
                ArcDPSEnums.TrashID.SmallKillerTornado,
                ArcDPSEnums.TrashID.SmallJumpyTornado,
                ArcDPSEnums.TrashID.BigKillerTornado
            };
        }

        internal override List<AbstractHealthDamageEvent> SpecialDamageEventProcess(Dictionary<AgentItem, List<AbstractHealthDamageEvent>> damageBySrc, Dictionary<AgentItem, List<AbstractHealthDamageEvent>> damageByDst, Dictionary<long, List<AbstractHealthDamageEvent>> damageById, SkillData skillData)
        {
            NegateDamageAgainstBarrier(Targets.Select(x => x.AgentItem).ToList(), damageByDst);
            return new List<AbstractHealthDamageEvent>();
        }

        internal override List<PhaseData> GetPhases(ParsedEvtcLog log, bool requirePhases)
        {
            List<PhaseData> phases = GetInitialPhase(log);
            NPC mainTarget = Targets.FirstOrDefault(x => x.ID == (int)ArcDPSEnums.TargetID.Sabir);
            if (mainTarget == null)
            {
                throw new MissingKeyActorsException("Sabir not found");
            }
            phases[0].AddTarget(mainTarget);
            if (!requirePhases)
            {
                return phases;
            }
            IReadOnlyList<AbstractCastEvent> cls = mainTarget.GetCastEvents(log, 0, log.FightData.FightEnd);
            var wallopingWinds = cls.Where(x => x.SkillId == 56094).ToList();
            long start = 0, end = 0;
            for (int i = 0; i < wallopingWinds.Count; i++)
            {
                AbstractCastEvent wallopinbWind = wallopingWinds[i];
                end = wallopinbWind.Time;
                var phase = new PhaseData(start, end, "Phase " + (i + 1));
                phase.AddTarget(mainTarget);
                phases.Add(phase);
                AbstractCastEvent nextAttack = cls.FirstOrDefault(x => x.Time >= wallopinbWind.EndTime && (x.SkillId == 56620 || x.SkillId == 56629 || x.SkillId == 56307));
                if (nextAttack == null)
                {
                    break;
                }
                start = nextAttack.Time;
                if (i == wallopingWinds.Count - 1)
                {
                    phase = new PhaseData(start, log.FightData.FightEnd, "Phase " + (i + 2));
                    phase.AddTarget(mainTarget);
                    phases.Add(phase);
                }
            }

            return phases;
        }

        protected override CombatReplayMap GetCombatMapInternal(ParsedEvtcLog log)
        {
            return new CombatReplayMap("https://i.imgur.com/wesgoc6.png",
                            (1000, 910),
                            (-14122, 142, -9199, 4640)/*,
                            (-21504, -21504, 24576, 24576),
                            (33530, 34050, 35450, 35970)*/);
        }

        internal override void ComputeNPCCombatReplayActors(NPC target, ParsedEvtcLog log, CombatReplay replay)
        {
            int start = (int)replay.TimeOffsets.start;
            int end = (int)replay.TimeOffsets.end;
            switch (target.ID)
            {
                case (int)ArcDPSEnums.TrashID.BigKillerTornado:
                    replay.Decorations.Add(new CircleDecoration(true, 0, 420, (start, end), "rgba(255, 150, 0, 0.4)", new AgentConnector(target)));
                    break;
                case (int)ArcDPSEnums.TrashID.SmallKillerTornado:
                    replay.Decorations.Add(new CircleDecoration(true, 0, 120, (start, end), "rgba(255, 150, 0, 0.4)", new AgentConnector(target)));
                    break;
                case (int)ArcDPSEnums.TrashID.SmallJumpyTornado:
                case (int)ArcDPSEnums.TrashID.ParalyzingWisp:
                case (int)ArcDPSEnums.TrashID.VoltaicWisp:
                    break;
                default:
                    break;

            }
        }
        internal override long GetFightOffset(FightData fightData, AgentData agentData, List<CombatItem> combatData)
        {
            // Find target
            AgentItem target = agentData.GetNPCsByID((int)ArcDPSEnums.TargetID.Sabir).FirstOrDefault();
            if (target == null)
            {
                throw new MissingKeyActorsException("Sabir not found");
            }
            CombatItem enterCombat = combatData.FirstOrDefault(x => x.IsStateChange == ArcDPSEnums.StateChange.EnterCombat && x.SrcAgent == target.Agent);
            return enterCombat != null ? enterCombat.Time : fightData.LogStart;
        }

        internal override FightData.CMStatus IsCM(CombatData combatData, AgentData agentData, FightData fightData)
        {
            NPC target = Targets.FirstOrDefault(x => x.ID == (int)ArcDPSEnums.TargetID.Sabir);
            if (target == null)
            {
                throw new MissingKeyActorsException("Sabir not found");
            }
            return (target.GetHealth(combatData) > 32e6) ? FightData.CMStatus.CM : FightData.CMStatus.NoCM;
        }
    }
}
