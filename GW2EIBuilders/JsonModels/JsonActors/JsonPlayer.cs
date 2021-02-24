﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using Newtonsoft.Json;
using static GW2EIBuilders.JsonModels.JsonBuffsUptime;
using static GW2EIBuilders.JsonModels.JsonPlayerBuffsGeneration;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIBuilders.JsonModels
{
    /// <summary>
    /// Class representing a player
    /// </summary>
    public class JsonPlayer : JsonActor
    {
        [JsonProperty]
        /// <summary>
        /// Account name of the player
        /// </summary>
        public string Account { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Group of the player
        /// </summary>
        public int Group { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Indicates if a player has a commander tag
        /// </summary>
        public bool HasCommanderTag { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Profession of the player
        /// </summary>
        public string Profession { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Weapons of the player \n
        /// 0-1 are the first land set, 1-2 are the second land set \n
        /// 3-4 are the first aquatic set, 5-6 are the second aquatic set \n
        /// When unknown, 'Unknown' value will appear \n
        /// If 2 handed weapon even indices will have "2Hand" as value
        /// </summary>
        public IReadOnlyList<string> Weapons { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of Total DPS stats \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <seealso cref="JsonStatistics.JsonDPS"/>
        public IReadOnlyList<IReadOnlyList<JsonStatistics.JsonDPS>> DpsTargets { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int representing 1S damage points \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>> TargetDamage1S { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int representing 1S power damage points \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>> TargetPowerDamage1S { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of int representing 1S condition damage points \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<int>>> TargetConditionDamage1S { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Array of double representing 1S breakbar damage points \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <remarks>
        /// If the duration of the phase in seconds is non integer, the last point of this array will correspond to the last point  \n
        /// ex: duration === 15250ms, the array will have 17 elements [0, 1000,...,15000,15250]
        /// </remarks>
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> TargetBreakbarDamage1S { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Per Target Damage distribution array \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <seealso cref="JsonDamageDist"/>
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<JsonDamageDist>>> TargetDamageDist { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Stats against targets  \n
        /// Length == # of targets and the length of each sub array is equal to # of phases
        /// </summary>
        /// <seealso cref="JsonStatistics.JsonGameplayStats"/>
        public IReadOnlyList<IReadOnlyList<JsonStatistics.JsonGameplayStats>> StatsTargets { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Support stats \n
        /// Length == # of phases
        /// </summary>
        /// <seealso cref="JsonStatistics.JsonPlayerSupport"/>
        public IReadOnlyList<JsonStatistics.JsonPlayerSupport> Support { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Damage modifiers against all
        /// </summary>
        /// <seealso cref="JsonDamageModifierData"/>
        public IReadOnlyList<JsonDamageModifierData> DamageModifiers { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// Damage modifiers against targets \n
        /// Length == # of targets
        /// </summary>
        /// <seealso cref="JsonDamageModifierData"/>
        public IReadOnlyList<IReadOnlyList<JsonDamageModifierData>> DamageModifiersTarget { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status
        /// </summary>
        /// <seealso cref="JsonBuffsUptime"/>
        public IReadOnlyList<JsonBuffsUptime> BuffUptimes { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on self generation  \n
        /// Key is "'b' + id"
        /// </summary>
        /// <seealso cref="JsonBuffsUptime"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> SelfBuffs { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on group generation
        /// </summary>
        /// <seealso cref="JsonPlayerBuffsGeneration"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> GroupBuffs { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on off group generation
        /// </summary>
        /// <seealso cref="JsonPlayerBuffsGeneration"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> OffGroupBuffs { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on squad generation
        /// </summary>
        /// <seealso cref="JsonPlayerBuffsGeneration"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> SquadBuffs { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on active time
        /// </summary>
        /// <seealso cref="JsonBuffsUptime"/>
        public IReadOnlyList<JsonBuffsUptime> BuffUptimesActive { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on self generation on active time
        /// </summary>
        /// <seealso cref="JsonBuffsUptime"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> SelfBuffsActive { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on group generation on active time
        /// <seealso cref="JsonPlayerBuffsGeneration"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> GroupBuffsActive { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on off group generation on active time
        /// <seealso cref="JsonPlayerBuffsGeneration"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> OffGroupBuffsActive { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of buff status on squad generation on active time
        /// </summary>
        /// <seealso cref="JsonPlayerBuffsGeneration"/>
        public IReadOnlyList<JsonPlayerBuffsGeneration> SquadBuffsActive { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of death recaps \n
        /// Length == number of death
        /// </summary>
        /// <seealso cref="JsonDeathRecap"/>
        public IReadOnlyList<JsonDeathRecap> DeathRecap { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of used consumables
        /// </summary>
        /// <seealso cref="JsonConsumable"/>
        public IReadOnlyList<JsonConsumable> Consumables { get; internal set; }
        [JsonProperty]
        /// <summary>
        /// List of time during which the player was active (not dead and not dc) \n
        /// Length == number of phases
        /// </summary>
        public IReadOnlyList<long> ActiveTimes { get; internal set; }

        [JsonConstructor]
        internal JsonPlayer()
        {

        }

        internal JsonPlayer(Player player, ParsedEvtcLog log, RawFormatSettings settings, Dictionary<string, JsonLog.SkillDesc> skillDesc, Dictionary<string, JsonLog.BuffDesc> buffDesc, Dictionary<string, JsonLog.DamageModDesc> damageModDesc, Dictionary<string, HashSet<long>> personalBuffs) : base(player, log, settings, skillDesc, buffDesc)
        {
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            //
            Account = player.Account;
            Weapons = player.GetWeaponsArray(log).Select(w => w ?? "Unknown").ToArray();
            Group = player.Group;
            Profession = player.Prof;
            ActiveTimes = phases.Select(x => player.GetActiveDuration(log, x.Start, x.End)).ToList();
            HasCommanderTag = player.HasCommanderTag;
            //
            Support = phases.Select(phase => new JsonStatistics.JsonPlayerSupport(player.GetPlayerSupportStats(log, phase.Start, phase.End))).ToArray();           
            var targetDamage1S = new IReadOnlyList<int>[log.FightData.Logic.Targets.Count][];
            var targetPowerDamage1S = new IReadOnlyList<int>[log.FightData.Logic.Targets.Count][];
            var targetConditionDamage1S = new IReadOnlyList<int>[log.FightData.Logic.Targets.Count][];
            var targetBreakbarDamage1S = new IReadOnlyList<double>[log.FightData.Logic.Targets.Count][];
            var dpsTargets = new JsonStatistics.JsonDPS[log.FightData.Logic.Targets.Count][];
            var statsTargets = new JsonStatistics.JsonGameplayStats[log.FightData.Logic.Targets.Count][];
            var targetDamageDist = new IReadOnlyList<JsonDamageDist>[log.FightData.Logic.Targets.Count][];
            for (int j = 0; j < log.FightData.Logic.Targets.Count; j++)
            {
                NPC target = log.FightData.Logic.Targets[j];
                var graph1SDamageList = new IReadOnlyList<int>[phases.Count];
                var graph1SPowerDamageList = new IReadOnlyList<int>[phases.Count];
                var graph1SConditionDamageList = new IReadOnlyList<int>[phases.Count];
                var graph1SBreakbarDamageList = new IReadOnlyList<double>[phases.Count];
                var targetDamageDistList = new IReadOnlyList<JsonDamageDist>[phases.Count];
                for (int i = 0; i < phases.Count; i++)
                {
                    PhaseData phase = phases[i];
                    if (settings.RawFormatTimelineArrays)
                    {
                        graph1SDamageList[i] = player.Get1SDamageList(log, phase.Start, phase.End, target, DamageType.All);
                        graph1SPowerDamageList[i] = player.Get1SDamageList(log, phase.Start, phase.End, target, DamageType.Power);
                        graph1SConditionDamageList[i] = player.Get1SDamageList(log, phase.Start, phase.End, target, DamageType.Condition);
                        graph1SBreakbarDamageList[i] = player.Get1SBreakbarDamageList(log, phase.Start, phase.End, target);
                    }
                    targetDamageDistList[i] = JsonDamageDist.BuildJsonDamageDistList(player.GetDamageEvents(target, log, phase.Start, phase.End).GroupBy(x => x.SkillId).ToDictionary(x => x.Key, x => x.ToList()), log, skillDesc, buffDesc);
                }
                if (settings.RawFormatTimelineArrays)
                {
                    targetDamage1S[j] = graph1SDamageList;
                    targetPowerDamage1S[j] = graph1SPowerDamageList;
                    targetConditionDamage1S[j] = graph1SConditionDamageList;
                    targetBreakbarDamage1S[j] = graph1SBreakbarDamageList;
                }
                targetDamageDist[j] = targetDamageDistList;
                dpsTargets[j] = phases.Select(phase => new JsonStatistics.JsonDPS(player.GetDPSStats(target, log, phase.Start, phase.End))).ToArray();
                statsTargets[j] = phases.Select(phase => new JsonStatistics.JsonGameplayStats(player.GetGameplayStats(target, log, phase.Start, phase.End))).ToArray();
            }
            if (settings.RawFormatTimelineArrays)
            {
                TargetDamage1S = targetDamage1S;
                TargetPowerDamage1S = targetPowerDamage1S;
                TargetConditionDamage1S = targetConditionDamage1S;
                TargetBreakbarDamage1S = targetBreakbarDamage1S;
            }
            TargetDamageDist = targetDamageDist;
            DpsTargets = dpsTargets;
            StatsTargets = statsTargets;
            if (!log.CombatData.HasBreakbarDamageData)
            {
                TargetBreakbarDamage1S = null;
            }
            //
            BuffUptimes = GetPlayerJsonBuffsUptime(player, phases.Select(phase => player.GetBuffs(BuffEnum.Self, log, phase.Start, phase.End)).ToList(), log, settings, buffDesc, personalBuffs);
            SelfBuffs = GetPlayerBuffGenerations(phases.Select(phase => player.GetBuffs(BuffEnum.Self, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            GroupBuffs = GetPlayerBuffGenerations(phases.Select(phase => player.GetBuffs(BuffEnum.Group, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            OffGroupBuffs = GetPlayerBuffGenerations(phases.Select(phase => player.GetBuffs(BuffEnum.OffGroup, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            SquadBuffs = GetPlayerBuffGenerations(phases.Select(phase => player.GetBuffs(BuffEnum.Squad, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            //
            BuffUptimesActive = GetPlayerJsonBuffsUptime(player, phases.Select(phase => player.GetActiveBuffs(BuffEnum.Self, log, phase.Start, phase.End)).ToList(), log, settings, buffDesc, personalBuffs);
            SelfBuffsActive = GetPlayerBuffGenerations(phases.Select(phase => player.GetActiveBuffs(BuffEnum.Self, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            GroupBuffsActive = GetPlayerBuffGenerations(phases.Select(phase => player.GetActiveBuffs(BuffEnum.Group, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            OffGroupBuffsActive = GetPlayerBuffGenerations(phases.Select(phase => player.GetActiveBuffs(BuffEnum.OffGroup, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            SquadBuffsActive = GetPlayerBuffGenerations(phases.Select(phase => player.GetActiveBuffs(BuffEnum.Squad, log, phase.Start, phase.End)).ToList(), log, buffDesc);
            //
            IReadOnlyList<Consumable> consumables = player.GetConsumablesList(log, 0, log.FightData.FightEnd);
            if (consumables.Any())
            {
                var consumablesJSON = new List<JsonConsumable>();
                foreach (Consumable food in consumables)
                {
                    if (!buffDesc.ContainsKey("b" + food.Buff.ID))
                    {
                        buffDesc["b" + food.Buff.ID] = new JsonLog.BuffDesc(food.Buff, log);
                    }
                    consumablesJSON.Add(new JsonConsumable(food));
                }
                Consumables = consumablesJSON;
            }
            //
            IReadOnlyList<DeathRecap> deathRecaps = player.GetDeathRecaps(log);
            if (deathRecaps.Any())
            {
                DeathRecap = deathRecaps.Select(x => new JsonDeathRecap(x)).ToList();
            }
            // 
            DamageModifiers = JsonDamageModifierData.GetDamageModifiers(player.GetDamageModifierStats(log, null), log, damageModDesc);
            DamageModifiersTarget = JsonDamageModifierData.GetDamageModifiersTarget(player, log, damageModDesc);
        }

        private static List<JsonPlayerBuffsGeneration> GetPlayerBuffGenerations(List<IReadOnlyDictionary<long, FinalPlayerBuffs>> buffs, ParsedEvtcLog log, Dictionary<string, JsonLog.BuffDesc> buffDesc)
        {
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            var uptimes = new List<JsonPlayerBuffsGeneration>();
            foreach (KeyValuePair<long, FinalPlayerBuffs> pair in buffs[0])
            {
                Buff buff = log.Buffs.BuffsByIds[pair.Key];
                if (!buffDesc.ContainsKey("b" + pair.Key))
                {
                    buffDesc["b" + pair.Key] = new JsonLog.BuffDesc(buff, log);
                }
                var data = new List<JsonBuffsGenerationData>();
                for (int i = 0; i < phases.Count; i++)
                {
                    if (buffs[i].TryGetValue(pair.Key, out FinalPlayerBuffs val))
                    {
                        var value = new JsonBuffsGenerationData(val);
                        data.Add(value);
                    }
                    else
                    {
                        var value = new JsonBuffsGenerationData();
                        data.Add(value);
                    }
                }
                var jsonBuffs = new JsonPlayerBuffsGeneration()
                {
                    BuffData = data,
                    Id = pair.Key
                };
                uptimes.Add(jsonBuffs);
            }

            if (!uptimes.Any())
            {
                return null;
            }

            return uptimes;
        }

        private static List<JsonBuffsUptime> GetPlayerJsonBuffsUptime(Player player, List<IReadOnlyDictionary<long, FinalPlayerBuffs>> buffs, ParsedEvtcLog log, RawFormatSettings settings, Dictionary<string, JsonLog.BuffDesc> buffDesc, Dictionary<string, HashSet<long>> personalBuffs)
        {
            var res = new List<JsonBuffsUptime>();
            var profEnums = new HashSet<ParserHelper.Source>(ProfToEnum(player.Prof));
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            foreach (KeyValuePair<long, FinalPlayerBuffs> pair in buffs[0])
            {
                Buff buff = log.Buffs.BuffsByIds[pair.Key];
                var data = new List<JsonBuffsUptimeData>();
                for (int i = 0; i < phases.Count; i++)
                {
                    PhaseData phase = phases[i];
                    Dictionary<long, FinalBuffsDictionary> buffsDictionary = player.GetBuffsDictionary(log, phase.Start, phase.End);
                    if (buffs[i].TryGetValue(pair.Key, out FinalPlayerBuffs val))
                    {
                        var value = new JsonBuffsUptimeData(val, buffsDictionary[pair.Key]);
                        data.Add(value);
                    }
                    else
                    {
                        var value = new JsonBuffsUptimeData();
                        data.Add(value);
                    }
                }
                if (buff.Nature == Buff.BuffNature.GraphOnlyBuff && profEnums.Contains(buff.Source))
                {
                    if (player.GetBuffDistribution(log, phases[0].Start, phases[0].End).GetUptime(pair.Key) > 0)
                    {
                        if (personalBuffs.TryGetValue(player.Prof, out HashSet<long> list) && !list.Contains(pair.Key))
                        {
                            list.Add(pair.Key);
                        }
                        else
                        {
                            personalBuffs[player.Prof] = new HashSet<long>()
                                {
                                    pair.Key
                                };
                        }
                    }
                }
                res.Add(new JsonBuffsUptime(player, pair.Key, log, settings, data, buffDesc));
            }
            return res;
        }

    }
}
