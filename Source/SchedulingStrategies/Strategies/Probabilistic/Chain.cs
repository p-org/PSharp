// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    internal class Chain
    {
        int readTill;
        List<ChainEvent> events;

        internal ChainEvent Tail
        {
            get { return events.Count>0 ? events[events.Count-1]: null; } 
        }

        internal Chain()
        {
            events = new List<ChainEvent>();
            readTill = 0;
        }

        internal void Add(ChainEvent chainEvent)
        {
            events.Add(chainEvent);
        }

        internal ChainEvent getHead()
        {
            return ( readTill < events.Count)? events[readTill] : null ;
        }

        internal void markCompletion(ChainEvent causingChainEvent)
        {
            if (!causingChainEvent.EqualsChainEvent(events[readTill]))
            {
                throw new System.Exception("This is not right");
            }
            readTill++;
        }

    }

    internal class ChainEvent
    {
        internal OperationType opType;
        internal ulong machineId;
        internal ulong otherId; // We might need just one

        internal Chain chain;
        internal bool complete; 

        internal ChainEvent(OperationType _opType, ulong _machineId, ulong _otherId)
        {
            opType = _opType;
            machineId = _machineId;
            otherId = _otherId;
        }


        public override string ToString()
        {
            return $"({opType} {machineId} {otherId})";
        }

        internal bool EqualsChainEvent(ChainEvent e)
        {
            return (opType == e.opType && machineId == e.machineId && otherId == e.otherId);
        }


        internal bool MatchSchedulable(ISchedulable sch)
        {
            if (sch.NextOperationType == opType && sch.NextTargetId == machineId)
            {
                return (opType == OperationType.Start
                    || (opType == OperationType.Receive && otherId == sch.NextOperationMatchingSendIndex));  // TODO: If we have a later event that has higher priority, is it blocked or does it promote all dependencies?
            }
            else
            {
                return false;
            }
        }

        internal void MarkComplete()
        {
            complete = true;
            chain.markCompletion(this);
        }
    }

}