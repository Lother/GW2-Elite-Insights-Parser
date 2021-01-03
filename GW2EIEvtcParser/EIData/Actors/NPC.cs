﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIEvtcParser.EIData
{
    public class NPC : AbstractSingleActor
    {
        private List<Dictionary<long, FinalBuffs>> _buffs;
        private List<Segment> _breakbarPercentUpdates { get; set; }
        // Constructors
        internal NPC(AgentItem agent) : base(agent)
        {
        }

        private int _health = -1;

        public int GetHealth(CombatData combatData)
        {
            if (_health == -1)
            {
                IReadOnlyList<MaxHealthUpdateEvent> maxHpUpdates = combatData.GetMaxHealthUpdateEvents(AgentItem);
                _health = maxHpUpdates.Count > 0 ? maxHpUpdates.Max(x => x.MaxHealth) : 1;
            }
            return _health;
        }

        public IReadOnlyList<Segment> GetBreakbarPercentUpdates(ParsedEvtcLog log)
        {
            if (_breakbarPercentUpdates == null)
            {
                _breakbarPercentUpdates = Segment.FromStates(log.CombatData.GetBreakbarPercentEvents(AgentItem).Select(x => x.ToState()).ToList(), 0, log.FightData.FightEnd);
            }
            return _breakbarPercentUpdates;
        }

        internal void OverrideName(string name)
        {
            Character = name;
        }

        public override string GetIcon()
        {
            return AgentItem.Type == AgentItem.AgentType.EnemyPlayer ? ParserHelper.GetHighResolutionProfIcon(Prof) : ParserHelper.GetNPCIcon(ID);
        }

        internal void SetManualHealth(int health)
        {
            _health = health;
        }

        /*public void AddCustomCastLog(long time, long skillID, int expDur, ParseEnum.Activation startActivation, int actDur, ParseEnum.Activation endActivation, ParsedEvtcLog log)
        {
            if (CastLogs.Count == 0)
            {
                GetCastLogs(log, 0, log.FightData.FightEnd);
            }
            CastLogs.Add(new CastLog(time, skillID, expDur, startActivation, actDur, endActivation, Agent, InstID));
        }*/

        public Dictionary<long, FinalBuffs> GetBuffs(ParsedEvtcLog log, int phaseIndex)
        {
            if (_buffs == null)
            {
                SetBuffs(log);
            }
            return _buffs[phaseIndex];
        }

        public List<Dictionary<long, FinalBuffs>> GetBuffs(ParsedEvtcLog log)
        {
            if (_buffs == null)
            {
                SetBuffs(log);
            }
            return _buffs;
        }

        private void SetBuffs(ParsedEvtcLog log)
        {
            _buffs = new List<Dictionary<long, FinalBuffs>>();
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
            {
                BuffDistribution buffDistribution = GetBuffDistribution(log, phaseIndex);
                var rates = new Dictionary<long, FinalBuffs>();
                _buffs.Add(rates);
                Dictionary<long, long> buffPresence = GetBuffPresence(log, phaseIndex);

                PhaseData phase = phases[phaseIndex];
                long phaseDuration = phase.DurationInMS;

                foreach (Buff buff in GetTrackedBuffs(log))
                {
                    if (buffDistribution.HasBuffID(buff.ID))
                    {
                        rates[buff.ID] = new FinalBuffs(buff, buffDistribution, buffPresence, phaseDuration);
                    }
                }
            }
        }

        protected override void InitAdditionalCombatReplayData(ParsedEvtcLog log)
        {
            log.FightData.Logic.ComputeNPCCombatReplayActors(this, log, CombatReplay);
            if (CombatReplay.Rotations.Any() && log.FightData.Logic.Targets.Contains(this))
            {
                CombatReplay.Decorations.Add(new FacingDecoration(((int)CombatReplay.TimeOffsets.start, (int)CombatReplay.TimeOffsets.end), new AgentConnector(this), CombatReplay.PolledRotations));
            }
        }


        //

        public override AbstractSingleActorSerializable GetCombatReplayJSON(CombatReplayMap map, ParsedEvtcLog log)
        {
            if (CombatReplay == null)
            {
                InitCombatReplay(log);
            }
            return new NPCSerializable(this, log, map, CombatReplay);
        }

        protected override bool InitCombatReplay(ParsedEvtcLog log)
        {
            if (base.InitCombatReplay(log))
            {
                // Trim
                DespawnEvent despawnCheck = log.CombatData.GetDespawnEvents(AgentItem).LastOrDefault();
                SpawnEvent spawnCheck = log.CombatData.GetSpawnEvents(AgentItem).LastOrDefault();
                DeadEvent deathCheck = log.CombatData.GetDeadEvents(AgentItem).LastOrDefault();
                AliveEvent aliveCheck = log.CombatData.GetAliveEvents(AgentItem).LastOrDefault();
                if (AgentItem.Type != AgentItem.AgentType.EnemyPlayer && deathCheck != null && (aliveCheck == null || aliveCheck.Time < deathCheck.Time))
                {
                    CombatReplay.Trim(AgentItem.FirstAware, deathCheck.Time);
                }
                else if (despawnCheck != null && (spawnCheck == null || spawnCheck.Time < despawnCheck.Time))
                {
                    CombatReplay.Trim(AgentItem.FirstAware, despawnCheck.Time);
                }
                else
                {
                    CombatReplay.Trim(AgentItem.FirstAware, AgentItem.LastAware);
                }
                return true;
            }
            return false;
        }
    }
}
