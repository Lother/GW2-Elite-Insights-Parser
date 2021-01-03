﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIEvtcParser.EIData
{
    internal abstract class BuffSimulatorID : AbstractBuffSimulator
    {
        protected class BuffStackItemID : BuffStackItem
        {

            public long StackID { get; protected set; } = 0;

            public bool Active { get; set; } = false;

            public BuffStackItemID(long start, long boonDuration, AgentItem src, bool active, long stackID) : base(start, boonDuration, src)
            {
                Active = active;
                StackID = stackID;
            }

            public void Activate()
            {
                Active = true;
            }

            public void Disable()
            {
                Active = false;
            }

            public override void Shift(long startShift, long durationShift)
            {
                // Only time shift if not active or duration is 0
                if (!Active || Duration == 0)
                {
                    base.Shift(startShift, 0);
                    return;
                }
                base.Shift(startShift, durationShift);
            }
        }

        protected List<BuffStackItemID> BuffStack { get; set; } = new List<BuffStackItemID>();
        //protected List<(long duration, AgentItem src)> OverrideCandidates { get; } = new List<(long duration, AgentItem src)>();

        // Constructor
        protected BuffSimulatorID(ParsedEvtcLog log, Buff buff) : base(log, buff)
        {
        }

        protected override void Clear()
        {
            BuffStack.Clear();
        }

        public override void Extend(long extension, long oldValue, AgentItem src, long time, uint stackID)
        {
            BuffStackItem toExtend = BuffStack.FirstOrDefault(x => x.StackID == stackID);
            if (toExtend == null)
            {
                throw new EIBuffSimulatorIDException("Extend has failed");
            }
            toExtend.Extend(extension, src);
            //ExtendedSimulationResult.Add(new BuffCreationItem(src, extension, time, toExtend.ID));
        }

        public override void Remove(AgentItem by, long removedDuration, int removedStacks, long time, ArcDPSEnums.BuffRemove removeType, uint stackID)
        {
            BuffStackItemID toRemove;
            switch (removeType)
            {
                case ArcDPSEnums.BuffRemove.All:
                    // remove all due to despawn event
                    if (removedStacks == BuffRemoveAllEvent.FullRemoval)
                    {
                        BuffStack.Clear();
                        return;
                    }
                    if (BuffStack.Count != 1)
                    {
                        if (BuffStack.Count < removedStacks)
                        {
                            //removedStacks = BuffStack.Count;
                            throw new EIBuffSimulatorIDException("Remove all failed");
                        }
                        // buff cleanse all
                        for (int i = 0; i < removedStacks; i++)
                        {
                            BuffStackItem stackItem = BuffStack[i];
                            WasteSimulationResult.Add(new BuffSimulationItemWasted(stackItem.Src, stackItem.Duration, time));
                            if (stackItem.Extensions.Any())
                            {
                                foreach ((AgentItem src, long value) in stackItem.Extensions)
                                {
                                    WasteSimulationResult.Add(new BuffSimulationItemWasted(src, value, time));
                                }
                            }
                        }
                        BuffStack = BuffStack.GetRange(removedStacks, BuffStack.Count - removedStacks);
                        return;
                    }
                    toRemove = BuffStack[0];
                    break;
                case ArcDPSEnums.BuffRemove.Single:
                    toRemove = BuffStack.FirstOrDefault(x => x.StackID == stackID);
                    break;
                default:
                    throw new InvalidDataException("Unknown remove type");
            }
            if (toRemove == null)
            {
                //return;
                throw new EIBuffSimulatorIDException("Remove has failed");
            }
            BuffStack.Remove(toRemove);
            if (removedDuration > ParserHelper.BuffSimulatorDelayConstant)
            {
                // safe checking, this can happen when an inactive stack is being removed but it was actually active
                if (Math.Abs(removedDuration - toRemove.TotalDuration) > ParserHelper.BuffSimulatorDelayConstant && !toRemove.Active)
                {
                    toRemove.Activate();
                    toRemove.Shift(0, Math.Abs(removedDuration - toRemove.TotalDuration));
                }
                // Removed due to override
                //(long duration, AgentItem src)? candidate = OverrideCandidates.FirstOrDefault(x => Math.Abs(x.duration - removedDuration) < ParserHelper.BuffSimulatorDelayConstant);
                if (by == ParserHelper._unknownAgent)
                {
                    //(long duration, AgentItem candSrc) = candidate.Value;
                    //OverrideCandidates.Remove(candidate.Value);
                    WasteSimulationResult.Add(new BuffSimulationItemWasted(toRemove.Src, toRemove.Duration, time));
                    if (toRemove.Extensions.Any())
                    {
                        foreach ((AgentItem src, long value) in toRemove.Extensions)
                        {
                            WasteSimulationResult.Add(new BuffSimulationItemWasted(src, value, time));
                        }
                    }
                }
                // Removed due to a cleanse
                else
                {
                    WasteSimulationResult.Add(new BuffSimulationItemWasted(toRemove.Src, toRemove.Duration, time));
                    if (toRemove.Extensions.Any())
                    {
                        foreach ((AgentItem src, long value) in toRemove.Extensions)
                        {
                            WasteSimulationResult.Add(new BuffSimulationItemWasted(src, value, time));
                        }
                    }
                }
            }
        }

        public override void Reset(uint stackID, long toDuration)
        {
            BuffStackItemID toDisable = BuffStack.FirstOrDefault(x => x.StackID == stackID);
            if (toDisable == null)
            {
                throw new EIBuffSimulatorIDException("Reset has failed");
            }
            toDisable.Disable();
        }

    }
}

