﻿namespace GW2EIEvtcParser.ParsedData
{
    public abstract class AbstractDamageEvent : AbstractTimeCombatEvent
    {
        public AgentItem From { get; }
        public AgentItem CreditedFrom => From.GetFinalMaster();
        public AgentItem To { get; }

        public SkillItem Skill { get; }
        public long SkillId => Skill.ID;
        public ArcDPSEnums.IFF IFF { get; }

        //private int _damage;
        public bool IsOverNinety { get; }
        public bool AgainstUnderFifty { get; }
        public bool IsMoving { get; }
        public bool IsFlanking { get; }

        internal AbstractDamageEvent(CombatItem evtcItem, AgentData agentData, SkillData skillData) : base(evtcItem.Time)
        {
            From = agentData.GetAgent(evtcItem.SrcAgent);
            To = agentData.GetAgent(evtcItem.DstAgent);
            Skill = skillData.Get(evtcItem.SkillID);
            IsOverNinety = evtcItem.IsNinety > 0;
            AgainstUnderFifty = evtcItem.IsFifty > 0;
            IsMoving = evtcItem.IsMoving > 0;
            IsFlanking = evtcItem.IsFlanking > 0;
            IFF = evtcItem.IFF;
        }

        public abstract bool ConditionDamageBased(ParsedEvtcLog log);
    }
}
