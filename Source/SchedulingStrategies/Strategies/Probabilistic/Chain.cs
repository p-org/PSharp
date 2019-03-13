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
        internal int readTill;
        internal List<ChainEvent> events;

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

        internal ChainEvent Head { get { return (readTill < events.Count) ? events[readTill] : null; }  }

        internal void markCompletion(ChainEvent causingChainEvent)
        {
            if (!causingChainEvent.EqualsChainEvent(events[readTill]))
            {
                throw new System.Exception("This is not right");
            }
            readTill++;
        }

        public void PrintChain(ILogger logger)
        {
            char chainSeparator = '-';
            logger.Write("<PctcpChain>: ");
            int ci = 0;
            foreach (ChainEvent ce in events)
            {
                if (readTill == ci)
                {
                    chainSeparator = '=';
                }
                ci++;
                logger.Write($"{chainSeparator}[{ce.machineId},{ce.opType},{ce.otherId}]{chainSeparator}");
            }
            logger.WriteLine("###");
        }
    }

    internal class ChainEvent
    {
        internal OperationType opType;
        internal ulong machineId;
        internal ulong otherId; // We might need just one

        internal Chain chain;
        //internal bool complete; 

        internal static ChainEvent FromISchedulable(ISchedulable sch)
        {
            ulong otherId = 0;
            switch (sch.NextOperationType)
            {
                case OperationType.Receive:
                    otherId = sch.NextOperationMatchingSendIndex;
                    break;
                case OperationType.Send:
                case OperationType.Create:
                case OperationType.Start:
                case OperationType.Stop:
                    otherId = sch.NextTargetId;
                    break;
                default:
                    throw new Exception("OperationType not yet supported by PCTCP: " + sch.NextOperationType);
            }
            return new ChainEvent(sch.NextOperationType, sch.Id, otherId);
        }

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
            if (sch.NextOperationType == opType && sch.Id == machineId)
            {
                if(opType == OperationType.Receive)
                {
                    return otherId == sch.NextOperationMatchingSendIndex;
                }
                else
                {
                    if(otherId != sch.NextTargetId)
                    {
                        throw new Exception("This isnt a necessary check");
                    }
                    return otherId == sch.NextTargetId;
                }
            }
            else
            {
                return false;
            }
        }

        internal void MarkComplete()
        {
            chain.markCompletion(this);
        }
    }

}