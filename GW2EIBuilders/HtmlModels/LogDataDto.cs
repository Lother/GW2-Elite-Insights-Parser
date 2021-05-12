﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.EncounterLogic;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIBuilders.HtmlModels
{
    internal class LogDataDto
    {
        public List<TargetDto> Targets { get; set; } = new List<TargetDto>();
        public List<PlayerDto> Players { get; } = new List<PlayerDto>();
        public List<EnemyDto> Enemies { get; } = new List<EnemyDto>();
        public List<PhaseDto> Phases { get; } = new List<PhaseDto>();
        public List<long> Boons { get; } = new List<long>();
        public List<long> OffBuffs { get; } = new List<long>();
        public List<long> SupBuffs { get; } = new List<long>();
        public List<long> DefBuffs { get; } = new List<long>();
        public List<long> GearBuffs { get; } = new List<long>();
        public List<long> FractalInstabilities { get; } = new List<long>();
        public List<long> DmgModifiersItem { get; } = new List<long>();
        public List<long> DmgModifiersCommon { get; } = new List<long>();
        public Dictionary<string, List<long>> DmgModifiersPers { get; } = new Dictionary<string, List<long>>();
        public Dictionary<string, List<long>> PersBuffs { get; } = new Dictionary<string, List<long>>();
        public List<long> Conditions { get; } = new List<long>();
        public Dictionary<string, SkillDto> SkillMap { get; } = new Dictionary<string, SkillDto>();
        public Dictionary<string, BuffDto> BuffMap { get; } = new Dictionary<string, BuffDto>();
        public Dictionary<string, DamageModDto> DamageModMap { get; } = new Dictionary<string, DamageModDto>();
        public List<MechanicDto> MechanicMap { get; set; } = new List<MechanicDto>();
        public CombatReplayDto CrData { get; set; } = null;
        public string EncounterDuration { get; set; }
        public bool Success { get; set; }
        public bool Wvw { get; set; }
        public bool HasCommander { get; set; }
        public bool Targetless { get; set; }
        public string FightName { get; set; }
        public string FightIcon { get; set; }
        public bool LightTheme { get; set; }
        public bool NoMechanics { get; set; }
        public bool SingleGroup { get; set; }
        public bool HasBreakbarDamage { get; set; }
        public List<string> LogErrors { get; set; }

        public string EncounterStart { get; set; }
        public string EncounterEnd { get; set; }
        public string ArcVersion { get; set; }
        public ulong Gw2Build { get; set; }
        public long FightID { get; set; }
        public string Parser { get; set; }
        public string RecordedBy { get; set; }
        public List<string> UploadLinks { get; set; }


        private static Dictionary<string, List<Buff>> BuildPersonalBoonData(ParsedEvtcLog log, Dictionary<string, List<long>> dict, Dictionary<long, Buff> usedBuffs)
        {
            var boonsBySpec = new Dictionary<string, List<Buff>>();
            // Collect all personal buffs by spec
            foreach (KeyValuePair<string, List<Player>> pair in log.PlayerListBySpec)
            {
                List<Player> players = pair.Value;
                var specBoonIds = new HashSet<long>(log.Buffs.GetPersonalBuffsList(pair.Key).Select(x => x.ID));
                var boonToUse = new HashSet<Buff>();
                foreach (Player player in players)
                {
                    foreach (PhaseData phase in log.FightData.GetPhases(log))
                    {
                        IReadOnlyDictionary<long, FinalPlayerBuffs> boons = player.GetBuffs(BuffEnum.Self, log, phase.Start, phase.End);
                        foreach (Buff boon in log.StatisticsHelper.GetPresentRemainingBuffsOnPlayer(player))
                        {
                            if (boons.TryGetValue(boon.ID, out FinalPlayerBuffs uptime))
                            {
                                if (uptime.Uptime > 0 && specBoonIds.Contains(boon.ID))
                                {
                                    boonToUse.Add(boon);
                                }
                            }
                        }
                    }
                }
                boonsBySpec[pair.Key] = boonToUse.ToList();
            }
            foreach (KeyValuePair<string, List<Buff>> pair in boonsBySpec)
            {
                dict[pair.Key] = new List<long>();
                foreach (Buff boon in pair.Value)
                {
                    dict[pair.Key].Add(boon.ID);
                    usedBuffs[boon.ID] = boon;
                }
            }
            return boonsBySpec;
        }

        private static Dictionary<string, List<DamageModifier>> BuildPersonalDamageModData(ParsedEvtcLog log, Dictionary<string, List<long>> dict, HashSet<DamageModifier> usedDamageMods)
        {
            var damageModBySpecs = new Dictionary<string, List<DamageModifier>>();
            // Collect all personal damage mods by spec
            foreach (KeyValuePair<string, List<Player>> pair in log.PlayerListBySpec)
            {
                var specDamageModsName = new HashSet<string>(log.DamageModifiers.GetModifiersPerProf(pair.Key).Select(x => x.Name));
                var damageModsToUse = new HashSet<DamageModifier>();
                foreach (Player player in pair.Value)
                {
                    var presentDamageMods = new HashSet<string>(player.GetPresentDamageModifier(log).Intersect(specDamageModsName));
                    foreach (string name in presentDamageMods)
                    {
                        damageModsToUse.Add(log.DamageModifiers.DamageModifiersByName[name]);
                    }
                }
                damageModBySpecs[pair.Key] = damageModsToUse.ToList();
            }
            foreach (KeyValuePair<string, List<DamageModifier>> pair in damageModBySpecs)
            {
                dict[pair.Key] = new List<long>();
                foreach (DamageModifier mod in pair.Value)
                {
                    dict[pair.Key].Add(mod.ID);
                    usedDamageMods.Add(mod);
                }
            }
            return damageModBySpecs;
        }

        private static bool HasBoons(ParsedEvtcLog log, PhaseData phase, NPC target)
        {
            IReadOnlyDictionary<long, FinalBuffs> conditions = target.GetBuffs(log, phase.Start, phase.End);
            foreach (Buff boon in log.StatisticsHelper.PresentBoons)
            {
                if (conditions.TryGetValue(boon.ID, out FinalBuffs uptime))
                {
                    if (uptime.Uptime > 0.0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static LogDataDto BuildLogData(ParsedEvtcLog log, Dictionary<long, SkillItem> usedSkills, Dictionary<long, Buff> usedBuffs, HashSet<DamageModifier> usedDamageMods, bool cr, bool light, Version parserVersion, string[] uploadLinks)
        {
            StatisticsHelper statistics = log.StatisticsHelper;
            log.UpdateProgressWithCancellationCheck("HTML: building Log Data");
            var logData = new LogDataDto
            {
                EncounterStart = log.LogData.LogStartStd,
                EncounterEnd = log.LogData.LogEndStd,
                ArcVersion = log.LogData.ArcVersion,
                Gw2Build = log.LogData.GW2Build,
                FightID = log.FightData.TriggerID,
                Parser = "Elite Insights " + parserVersion.ToString(),
                RecordedBy = log.LogData.PoVName,
                UploadLinks = uploadLinks.ToList()
            };
            if (cr)
            {
                logData.CrData = new CombatReplayDto(log);
            }
            log.UpdateProgressWithCancellationCheck("HTML: building Players");
            foreach (Player player in log.PlayerList)
            {
                logData.HasCommander = logData.HasCommander || player.HasCommanderTag;
                logData.Players.Add(new PlayerDto(player, log, ActorDetailsDto.BuildPlayerData(log, player, usedSkills, usedBuffs)));
            }

            log.UpdateProgressWithCancellationCheck("HTML: building Enemies");
            foreach (AbstractSingleActor enemy in log.MechanicData.GetEnemyList(log, log.FightData.FightStart, log.FightData.FightEnd))
            {
                logData.Enemies.Add(new EnemyDto() { Name = enemy.Character });
            }

            log.UpdateProgressWithCancellationCheck("HTML: building Targets");
            foreach (NPC target in log.FightData.Logic.Targets)
            {
                var targetDto = new TargetDto(target, log, ActorDetailsDto.BuildTargetData(log, target, usedSkills, usedBuffs, cr));
                logData.Targets.Add(targetDto);
            }
            //
            log.UpdateProgressWithCancellationCheck("HTML: building Skill/Buff dictionaries");
            Dictionary<string, List<Buff>> persBuffDict = BuildPersonalBoonData(log, logData.PersBuffs, usedBuffs);
            Dictionary<string, List<DamageModifier>> persDamageModDict = BuildPersonalDamageModData(log, logData.DmgModifiersPers, usedDamageMods);
            var allDamageMods = new HashSet<string>();
            foreach (Player p in log.PlayerList)
            {
                allDamageMods.UnionWith(p.GetPresentDamageModifier(log));
            }
            var commonDamageModifiers = new List<DamageModifier>();
            if (log.DamageModifiers.DamageModifiersPerSource.TryGetValue(Source.Common, out IReadOnlyList<DamageModifier> list))
            {
                foreach (DamageModifier dMod in list)
                {
                    if (allDamageMods.Contains(dMod.Name))
                    {
                        commonDamageModifiers.Add(dMod);
                        logData.DmgModifiersCommon.Add(dMod.ID);
                        usedDamageMods.Add(dMod);
                    }
                }
            }
            if (log.DamageModifiers.DamageModifiersPerSource.TryGetValue(Source.FightSpecific, out list))
            {
                foreach (DamageModifier dMod in list)
                {
                    if (allDamageMods.Contains(dMod.Name))
                    {
                        commonDamageModifiers.Add(dMod);
                        logData.DmgModifiersCommon.Add(dMod.ID);
                        usedDamageMods.Add(dMod);
                    }
                }
            }
            var itemDamageModifiers = new List<DamageModifier>();
            if (log.DamageModifiers.DamageModifiersPerSource.TryGetValue(Source.Item, out list))
            {
                foreach (DamageModifier dMod in list)
                {
                    if (allDamageMods.Contains(dMod.Name))
                    {
                        itemDamageModifiers.Add(dMod);
                        logData.DmgModifiersItem.Add(dMod.ID);
                        usedDamageMods.Add(dMod);
                    }
                }
            }
            if (log.DamageModifiers.DamageModifiersPerSource.TryGetValue(Source.Gear, out list))
            {
                foreach (DamageModifier dMod in list)
                {
                    if (allDamageMods.Contains(dMod.Name))
                    {
                        itemDamageModifiers.Add(dMod);
                        logData.DmgModifiersItem.Add(dMod.ID);
                        usedDamageMods.Add(dMod);
                    }
                }
            }
            foreach (Buff boon in statistics.PresentBoons)
            {
                logData.Boons.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            foreach (Buff boon in statistics.PresentConditions)
            {
                logData.Conditions.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            foreach (Buff boon in statistics.PresentOffbuffs)
            {
                logData.OffBuffs.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            foreach (Buff boon in statistics.PresentSupbuffs)
            {
                logData.SupBuffs.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            foreach (Buff boon in statistics.PresentDefbuffs)
            {
                logData.DefBuffs.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            foreach (Buff boon in statistics.PresentGearbuffs)
            {
                logData.GearBuffs.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            foreach (Buff boon in statistics.PresentFractalInstabilities)
            {
                logData.FractalInstabilities.Add(boon.ID);
                usedBuffs[boon.ID] = boon;
            }
            //
            log.UpdateProgressWithCancellationCheck("HTML: building Phases");
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            for (int i = 0; i < phases.Count; i++)
            {
                PhaseData phase = phases[i];
                var phaseDto = new PhaseDto(phase, phases, log)
                {
                    DpsStats = PhaseDto.BuildDPSData(log, phase),
                    DpsStatsTargets = PhaseDto.BuildDPSTargetsData(log, phase),
                    DmgStatsTargets = PhaseDto.BuildDMGStatsTargetsData(log, phase),
                    DmgStats = PhaseDto.BuildDMGStatsData(log, phase),
                    DefStats = PhaseDto.BuildDefenseData(log, phase),
                    SupportStats = PhaseDto.BuildSupportData(log, phase),
                    //
                    BoonStats = BuffData.BuildBuffUptimeData(log, statistics.PresentBoons, phase),
                    OffBuffStats = BuffData.BuildBuffUptimeData(log, statistics.PresentOffbuffs, phase),
                    SupBuffStats = BuffData.BuildBuffUptimeData(log, statistics.PresentSupbuffs, phase),
                    DefBuffStats = BuffData.BuildBuffUptimeData(log, statistics.PresentDefbuffs, phase),
                    PersBuffStats = BuffData.BuildPersonalBuffUptimeData(log, persBuffDict, phase),
                    GearBuffStats = BuffData.BuildBuffUptimeData(log, statistics.PresentGearbuffs, phase),
                    ConditionsStats = BuffData.BuildBuffUptimeData(log, statistics.PresentConditions, phase),
                    BoonGenSelfStats = BuffData.BuildBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.Self),
                    BoonGenGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.Group),
                    BoonGenOGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.OffGroup),
                    BoonGenSquadStats = BuffData.BuildBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.Squad),
                    OffBuffGenSelfStats = BuffData.BuildBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.Self),
                    OffBuffGenGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.Group),
                    OffBuffGenOGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.OffGroup),
                    OffBuffGenSquadStats = BuffData.BuildBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.Squad),
                    SupBuffGenSelfStats = BuffData.BuildBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.Self),
                    SupBuffGenGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.Group),
                    SupBuffGenOGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.OffGroup),
                    SupBuffGenSquadStats = BuffData.BuildBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.Squad),
                    DefBuffGenSelfStats = BuffData.BuildBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.Self),
                    DefBuffGenGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.Group),
                    DefBuffGenOGroupStats = BuffData.BuildBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.OffGroup),
                    DefBuffGenSquadStats = BuffData.BuildBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.Squad),
                    //
                    BoonActiveStats = BuffData.BuildActiveBuffUptimeData(log, statistics.PresentBoons, phase),
                    OffBuffActiveStats = BuffData.BuildActiveBuffUptimeData(log, statistics.PresentOffbuffs, phase),
                    SupBuffActiveStats = BuffData.BuildActiveBuffUptimeData(log, statistics.PresentSupbuffs, phase),
                    DefBuffActiveStats = BuffData.BuildActiveBuffUptimeData(log, statistics.PresentDefbuffs, phase),
                    PersBuffActiveStats = BuffData.BuildActivePersonalBuffUptimeData(log, persBuffDict, phase),
                    GearBuffActiveStats = BuffData.BuildActiveBuffUptimeData(log, statistics.PresentGearbuffs, phase),
                    ConditionsActiveStats = BuffData.BuildActiveBuffUptimeData(log, statistics.PresentConditions, phase),
                    BoonGenActiveSelfStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.Self),
                    BoonGenActiveGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.Group),
                    BoonGenActiveOGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.OffGroup),
                    BoonGenActiveSquadStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentBoons, phase, BuffEnum.Squad),
                    OffBuffGenActiveSelfStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.Self),
                    OffBuffGenActiveGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.Group),
                    OffBuffGenActiveOGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.OffGroup),
                    OffBuffGenActiveSquadStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentOffbuffs, phase, BuffEnum.Squad),
                    SupBuffGenActiveSelfStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.Self),
                    SupBuffGenActiveGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.Group),
                    SupBuffGenActiveOGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.OffGroup),
                    SupBuffGenActiveSquadStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentSupbuffs, phase, BuffEnum.Squad),
                    DefBuffGenActiveSelfStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.Self),
                    DefBuffGenActiveGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.Group),
                    DefBuffGenActiveOGroupStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.OffGroup),
                    DefBuffGenActiveSquadStats = BuffData.BuildActiveBuffGenerationData(log, statistics.PresentDefbuffs, phase, BuffEnum.Squad),
                    //
                    DmgModifiersCommon = DamageModData.BuildDmgModifiersData(log, i, commonDamageModifiers),
                    DmgModifiersItem = DamageModData.BuildDmgModifiersData(log, i, itemDamageModifiers),
                    DmgModifiersPers = DamageModData.BuildPersonalDmgModifiersData(log, i, persDamageModDict),
                    TargetsCondiStats = new List<List<BuffData>>(),
                    TargetsCondiTotals = new List<BuffData>(),
                    TargetsBoonTotals = new List<BuffData>(),
                    MechanicStats = MechanicDto.BuildPlayerMechanicData(log, phase),
                    EnemyMechanicStats = MechanicDto.BuildEnemyMechanicData(log, phase)
                };
                foreach (NPC target in phase.Targets)
                {
                    phaseDto.TargetsCondiStats.Add(BuffData.BuildTargetCondiData(log, phase.Start, phase.End, target));
                    phaseDto.TargetsCondiTotals.Add(BuffData.BuildTargetCondiUptimeData(log, phase, target));
                    phaseDto.TargetsBoonTotals.Add(HasBoons(log, phase, target) ? BuffData.BuildTargetBoonData(log, phase, target) : null);
                }
                logData.Phases.Add(phaseDto);
            }
            //
            log.UpdateProgressWithCancellationCheck("HTML: building Meta Data");
            logData.EncounterDuration = log.FightData.DurationString;
            logData.Success = log.FightData.Success;
            logData.Wvw = log.FightData.Logic.Mode == FightLogic.ParseMode.WvW;
            logData.Targetless = log.FightData.Logic.Targetless;
            logData.FightName = log.FightData.GetFightName(log);
            logData.FightIcon = log.FightData.Logic.Icon;
            logData.LightTheme = light;
            logData.SingleGroup = log.PlayerList.Where(x => !x.IsFakeActor).Select(x => x.Group).Distinct().Count() == 1;
            logData.HasBreakbarDamage = log.CombatData.HasBreakbarDamageData;
            logData.NoMechanics = log.FightData.Logic.HasNoFightSpecificMechanics;
            if (log.LogData.LogErrors.Count > 0)
            {
                logData.LogErrors = new List<string>(log.LogData.LogErrors);
            }
            //
            SkillDto.AssembleSkills(usedSkills.Values, logData.SkillMap, log.SkillData);
            DamageModDto.AssembleDamageModifiers(usedDamageMods, logData.DamageModMap);
            BuffDto.AssembleBoons(usedBuffs.Values, logData.BuffMap, log);
            MechanicDto.BuildMechanics(log.MechanicData.GetPresentMechanics(log, log.FightData.FightStart, log.FightData.FightEnd), logData.MechanicMap);
            return logData;
        }

    }
}
