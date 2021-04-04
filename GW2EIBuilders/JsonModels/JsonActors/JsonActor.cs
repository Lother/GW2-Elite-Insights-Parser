﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using Newtonsoft.Json;

namespace GW2EIBuilders.JsonModels
{
    /// <summary>
    /// Base class for Players and NPCs
    /// </summary>
    /// <seealso cref="JsonPlayer"/> 
    /// <seealso cref="JsonNPC"/>
    public abstract class JsonActor
    {

        [JsonProperty]
        /// <summary>
        /// Name of the actor
        /// </summary>
        public string Name { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Total health of the actor. -1 if information is missing (ex: players)
        /// </summary>
        public int TotalHealth { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Condition damage score
        /// </summary>
        public uint Condition { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Concentration score
        /// </summary>
        public uint Concentration { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Healing Power score
        /// </summary>
        public uint Healing { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Toughness score
        /// </summary>
        public uint Toughness { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Height of the hitbox
        /// </summary>
        public uint HitboxHeight { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Width of the hitbox
        /// </summary>
        public uint HitboxWidth { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// ID of the actor in the instance
        /// </summary>
        public ushort InstanceID { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of minions
        /// </summary>
        /// <seealso cref="JsonMinions"/>
        public IReadOnlyList<JsonMinions> Minions { get; internal set; }
        [JsonProperty]

        /// <summary>
        /// Array of Total DPS stats \n
        /// Length == # of phases
        /// </summary>
        /// <seealso cref="JsonDPS"/>
        public IReadOnlyList<JsonStatistics.JsonDPS> DpsAll { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Stats against all  \n
        /// Length == # of phases
        /// </summary>
        /// <seealso cref="JsonGameplayStatsAll"/>
        public IReadOnlyList<JsonStatistics.JsonGameplayStatsAll> StatsAll { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Defensive stats \n
        /// Length == # of phases
        /// </summary>
        /// <seealso cref="JsonDefensesAll"/>
        public IReadOnlyList<JsonStatistics.JsonDefensesAll> Defenses { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Total Damage distribution array \n
        /// Length == # of phases
        /// </summary>
        /// <seealso cref="JsonDamageDist"/>
        public IReadOnlyList<IReadOnlyList<JsonDamageDist>> TotalDamageDist { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Damage taken array
        /// Length == # of phases
        /// </summary>
        /// <seealso cref="JsonDamageDist"/>
        public IReadOnlyList<IReadOnlyList<JsonDamageDist>> TotalDamageTaken { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Rotation data
        /// </summary>
        /// <seealso cref="JsonRotation"/>
        public IReadOnlyList<JsonRotation> Rotation { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int representing 1S damage points \n
        /// Length == # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<int>> Damage1S { get; internal set; }
        /// <summary>
        /// Array of int representing 1S power damage points \n
        /// Length == # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<int>> PowerDamage1S { get; internal set; }
        /// <summary>
        /// Array of int representing 1S condition damage points \n
        /// Length == # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<int>> ConditionDamage1S { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of double representing 1S breakbar damage points \n
        /// Length == # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<double>> BreakbarDamage1S { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int[2] that represents the number of conditions \n
        /// Array[i][0] will be the time, Array[i][1] will be the number of conditions present from Array[i][0] to Array[i+1][0] \n
        /// If i corresponds to the last element that means the status did not change for the remainder of the fight \n
        /// </summary>
        public IReadOnlyList<IReadOnlyList<int>> ConditionsStates { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int[2] that represents the number of boons \n
        /// Array[i][0] will be the time, Array[i][1] will be the number of boons present from Array[i][0] to Array[i+1][0] \n
        /// If i corresponds to the last element that means the status did not change for the remainder of the fight
        /// </summary>
        public IReadOnlyList<IReadOnlyList<int>> BoonsStates { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int[2] that represents the number of active combat minions \n
        /// Array[i][0] will be the time, Array[i][1] will be the number of active combat minions present from Array[i][0] to Array[i+1][0] \n
        /// If i corresponds to the last element that means the status did not change for the remainder of the fight
        /// </summary>
        public IReadOnlyList<IReadOnlyList<int>> ActiveCombatMinions { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of double[2] that represents the health status of the actor \n
        /// Array[i][0] will be the time, Array[i][1] will be health % \n
        /// If i corresponds to the last element that means the health did not change for the remainder of the fight \n
        /// </summary>
        public IReadOnlyList<IReadOnlyList<double>> HealthPercents { get; internal set; }
        /// <summary>
        /// Array of double[2] that represents the barrier status of the actor \n
        /// Array[i][0] will be the time, Array[i][1] will be barrier % \n
        /// If i corresponds to the last element that means the health did not change for the remainder of the fight \n
        /// </summary>
        public IReadOnlyList<IReadOnlyList<double>> BarrierPercents { get; internal set; }


        [JsonConstructor]
        internal JsonActor()
        {

        }

        protected JsonActor(AbstractSingleActor actor, ParsedEvtcLog log, RawFormatSettings settings, Dictionary<string, JsonLog.SkillDesc> skillDesc, Dictionary<string, JsonLog.BuffDesc> buffDesc)
        {
            IReadOnlyList<PhaseData> phases = log.FightData.GetNonDummyPhases(log);
            //
            Name = actor.Character;
            TotalHealth = actor.GetHealth(log.CombatData);
            Toughness = actor.Toughness;
            Healing = actor.Healing;
            Concentration = actor.Concentration;
            Condition = actor.Condition;
            HitboxHeight = actor.HitboxHeight;
            HitboxWidth = actor.HitboxWidth;
            InstanceID = actor.InstID;
            //
            DpsAll = phases.Select(phase => new JsonStatistics.JsonDPS(actor.GetDPSStats(log, phase.Start, phase.End))).ToArray();
            StatsAll = phases.Select(phase => new JsonStatistics.JsonGameplayStatsAll(actor.GetGameplayStats(log, phase.Start, phase.End))).ToArray();
            Defenses = phases.Select(phase => new JsonStatistics.JsonDefensesAll(actor.GetDefenseStats(log, phase.Start, phase.End))).ToArray();
            //
            IReadOnlyDictionary<long, Minions> minionsList = actor.GetMinions(log);
            if (minionsList.Values.Any())
            {
                Minions = minionsList.Values.Select(x => new JsonMinions(x, log, skillDesc, buffDesc)).ToList();
            }
            //
            var skillByID = actor.GetIntersectingCastEvents(log, 0, log.FightData.FightEnd).GroupBy(x => x.SkillId).ToDictionary(x => x.Key, x => x.ToList());
            if (skillByID.Any())
            {
                Rotation = JsonRotation.BuildJsonRotationList(log, skillByID, skillDesc);
            }
            //
            if (settings.RawFormatTimelineArrays)
            {
                var damage1S = new IReadOnlyList<int>[phases.Count];
                var powerDamage1S = new IReadOnlyList<int>[phases.Count];
                var conditionDamage1S = new IReadOnlyList<int>[phases.Count];
                var breakbarDamage1S = new IReadOnlyList<double>[phases.Count];
                for (int i = 0; i < phases.Count; i++)
                {
                    PhaseData phase = phases[i];
                    damage1S[i] = actor.Get1SDamageList(log, phase.Start, phase.End, null, ParserHelper.DamageType.All);
                    powerDamage1S[i] = actor.Get1SDamageList(log, phase.Start, phase.End, null, ParserHelper.DamageType.Power);
                    conditionDamage1S[i] = actor.Get1SDamageList(log, phase.Start, phase.End, null, ParserHelper.DamageType.Condition);
                    breakbarDamage1S[i] = actor.Get1SBreakbarDamageList(log, phase.Start, phase.End, null);
                }
                Damage1S = damage1S;
                PowerDamage1S = powerDamage1S;
                ConditionDamage1S = conditionDamage1S;
                BreakbarDamage1S = breakbarDamage1S;
            }
            if (!log.CombatData.HasBreakbarDamageData)
            {
                BreakbarDamage1S = null;
            }
            //
            TotalDamageDist = BuildDamageDistData(actor, phases, log, skillDesc, buffDesc);
            TotalDamageTaken = BuildDamageTakenDistData(actor, phases, log, skillDesc, buffDesc);
            //
            if (settings.RawFormatTimelineArrays)
            {
                Dictionary<long, BuffsGraphModel> buffGraphs = actor.GetBuffGraphs(log);
                BoonsStates = JsonBuffsUptime.GetBuffStates(buffGraphs[Buff.NumberOfBoonsID]);
                ConditionsStates = JsonBuffsUptime.GetBuffStates(buffGraphs[Buff.NumberOfConditionsID]);
                if (buffGraphs.TryGetValue(Buff.NumberOfActiveCombatMinions, out BuffsGraphModel states))
                {
                    ActiveCombatMinions = JsonBuffsUptime.GetBuffStates(states);
                }
                // Health
                HealthPercents = actor.GetHealthUpdates(log).Select(x => new double[2] { x.Start, x.Value }).ToList();
                BarrierPercents = actor.GetBarrierUpdates(log).Select(x => new double[2] { x.Start, x.Value }).ToList();
            }
        }

        protected static List<JsonDamageDist>[] BuildDamageDistData(AbstractSingleActor actor, IReadOnlyList<PhaseData> phases, ParsedEvtcLog log, Dictionary<string, JsonLog.SkillDesc> skillDesc, Dictionary<string, JsonLog.BuffDesc> buffDesc)
        {
            var res = new List<JsonDamageDist>[phases.Count];
            for (int i = 0; i < phases.Count; i++)
            {
                PhaseData phase = phases[i];
                res[i] = JsonDamageDist.BuildJsonDamageDistList(actor.GetDamageEvents(null, log, phase.Start, phase.End).GroupBy(x => x.SkillId).ToDictionary(x => x.Key, x => x.ToList()), log, skillDesc, buffDesc);
            }
            return res;
        }

        protected static List<JsonDamageDist>[] BuildDamageTakenDistData(AbstractSingleActor actor, IReadOnlyList<PhaseData> phases, ParsedEvtcLog log, Dictionary<string, JsonLog.SkillDesc> skillDesc, Dictionary<string, JsonLog.BuffDesc> buffDesc)
        {
            var res = new List<JsonDamageDist>[phases.Count];
            for (int i = 0; i < phases.Count; i++)
            {
                PhaseData phase = phases[i];
                res[i] = JsonDamageDist.BuildJsonDamageDistList(actor.GetDamageTakenEvents(null, log, phase.Start, phase.End).GroupBy(x => x.SkillId).ToDictionary(x => x.Key, x => x.ToList()), log, skillDesc, buffDesc);
            }
            return res;
        }
    }
}
