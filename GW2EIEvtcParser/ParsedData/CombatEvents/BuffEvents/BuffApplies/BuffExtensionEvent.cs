﻿using GW2EIEvtcParser.EIData;

namespace GW2EIEvtcParser.ParsedData
{
    public class BuffExtensionEvent : AbstractBuffApplyEvent
    {
        private readonly long _oldValue;
        private readonly long _durationChange;
        private bool _sourceFinderRan = false;

        internal BuffExtensionEvent(CombatItem evtcItem, AgentData agentData, SkillData skillData) : base(evtcItem, agentData, skillData)
        {
            _oldValue = evtcItem.OverstackValue - evtcItem.Value;
            _durationChange = evtcItem.Value;
        }

        internal override void TryFindSrc(ParsedEvtcLog log)
        {
            if (!_sourceFinderRan && By == ParserHelper._unknownAgent)
            {
                _sourceFinderRan = true;
                By = log.Buffs.TryFindSrc(To, Time, _durationChange, log, BuffID);
            }
        }

        internal override void UpdateSimulator(AbstractBuffSimulator simulator)
        {
            simulator.Extend(_durationChange, _oldValue, CreditedBy, Time, BuffInstance);
        }

        internal override int CompareTo(AbstractBuffEvent abe)
        {
            if (abe is BuffExtensionEvent)
            {
                return 0;
            }
            if (abe is BuffApplyEvent)
            {
                return 1;
            }
            return -1;
        }
    }
}
