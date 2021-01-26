﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIEvtcParser.EIData
{
    public class BuffDamageModifier : DamageModifier
    {

        internal BuffsTracker Tracker { get; }

        internal BuffDamageModifier(long id, string name, string tooltip, DamageSource damageSource, double gainPerStack, DamageType srctype, DamageType compareType, ParserHelper.Source src, GainComputer gainComputer, string icon, DamageModifierMode mode, DamageLogChecker dlChecker = null) : base(name, tooltip, damageSource, gainPerStack, srctype, compareType, src, icon, gainComputer, dlChecker, ulong.MinValue, ulong.MaxValue, mode)
        {
            Tracker = new BuffsTrackerSingle(id);
        }

        internal BuffDamageModifier(long id, string name, string tooltip, DamageSource damageSource, double gainPerStack, DamageType srctype, DamageType compareType, ParserHelper.Source src, GainComputer gainComputer, string icon, ulong minBuild, ulong maxBuild, DamageModifierMode mode, DamageLogChecker dlChecker = null) : base(name, tooltip, damageSource, gainPerStack, srctype, compareType, src, icon, gainComputer, dlChecker, minBuild, maxBuild, mode)
        {
            Tracker = new BuffsTrackerSingle(id);
        }

        internal BuffDamageModifier(long[] ids, string name, string tooltip, DamageSource damageSource, double gainPerStack, DamageType srctype, DamageType compareType, ParserHelper.Source src, GainComputer gainComputer, string icon, DamageModifierMode mode, DamageLogChecker dlChecker = null) : base(name, tooltip, damageSource, gainPerStack, srctype, compareType, src, icon, gainComputer, dlChecker, ulong.MinValue, ulong.MaxValue, mode)
        {
            Tracker = new BuffsTrackerMulti(new List<long>(ids));
        }

        internal BuffDamageModifier(long[] ids, string name, string tooltip, DamageSource damageSource, double gainPerStack, DamageType srctype, DamageType compareType, ParserHelper.Source src, GainComputer gainComputer, string icon, ulong minBuild, ulong maxBuild, DamageModifierMode mode, DamageLogChecker dlChecker = null) : base(name, tooltip, damageSource, gainPerStack, srctype, compareType, src, icon, gainComputer, dlChecker, minBuild, maxBuild, mode)
        {
            Tracker = new BuffsTrackerMulti(new List<long>(ids));
        }

        protected double ComputeGain(int stack, AbstractHealthDamageEvent dl)
        {
            if (DLChecker != null && !DLChecker(dl))
            {
                return -1.0;
            }
            double gain = GainComputer.ComputeGain(GainPerStack, stack);
            return gain > 0.0 ? gain * dl.HealthDamage : -1.0;
        }

        internal override void ComputeDamageModifier(Dictionary<string, List<DamageModifierStat>> data, Dictionary<NPC, Dictionary<string, List<DamageModifierStat>>> dataTarget, Player p, ParsedEvtcLog log)
        {
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            Dictionary<long, BuffsGraphModel> bgms = p.GetBuffGraphs(log);
            if (!Tracker.Has(bgms) && GainComputer != ByAbsence)
            {
                return;
            }
            foreach (NPC target in log.FightData.Logic.Targets)
            {
                if (!dataTarget.TryGetValue(target, out Dictionary<string, List<DamageModifierStat>> extra))
                {
                    dataTarget[target] = new Dictionary<string, List<DamageModifierStat>>();
                }
                Dictionary<string, List<DamageModifierStat>> dict = dataTarget[target];
                if (!dict.TryGetValue(Name, out List<DamageModifierStat> list))
                {
                    var extraDataList = new List<DamageModifierStat>();
                    foreach (PhaseData phase in phases)
                    {
                        int totalDamage = GetTotalDamage(p, log, target, phase.Start, phase.End);
                        IReadOnlyList<AbstractHealthDamageEvent> typeHits = GetHitDamageEvents(p, log, target, phase.Start, phase.End);
                        var damages = typeHits.Select(x => ComputeGain(Tracker.GetStack(bgms, x.Time), x)).Where(x => x != -1.0).ToList();
                        extraDataList.Add(new DamageModifierStat(damages.Count, typeHits.Count, damages.Sum(), totalDamage));
                    }
                    dict[Name] = extraDataList;
                }
            }
            data[Name] = new List<DamageModifierStat>();
            foreach (PhaseData phase in phases)
            {
                int totalDamage = GetTotalDamage(p, log, null, phase.Start, phase.End);
                IReadOnlyList<AbstractHealthDamageEvent> typeHits = GetHitDamageEvents(p, log, null, phase.Start, phase.End);
                var damages = typeHits.Select(x => ComputeGain(Tracker.GetStack(bgms, x.Time), x)).Where(x => x != -1.0).ToList();
                data[Name].Add(new DamageModifierStat(damages.Count, typeHits.Count, damages.Sum(), totalDamage));
            }
        }
    }
}
