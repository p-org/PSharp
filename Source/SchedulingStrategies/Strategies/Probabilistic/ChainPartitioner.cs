// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;


namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    internal class ChainPartitioner
    {

        // Chains in prioritized order
        List<Chain> prioritizedList;
        private ulong machinesCreatedSoFar;
        Dictionary<ChainEvent, Chain> chainTailCache;
        Dictionary<ulong, ChainEvent> runningHandlers;


        IRandomNumberGenerator HAX_RandomNumberGenerator;
        ILogger HAX_logger;
        internal ChainPartitioner(ILogger logger, IRandomNumberGenerator randnumGen)
        {
            prioritizedList = new List<Chain>();
            runningHandlers = new Dictionary<ulong, ChainEvent>();
            chainTailCache = new Dictionary<ChainEvent, Chain>();
            Reset();

            HAX_RandomNumberGenerator = randnumGen;
            HAX_logger = logger;
        }


        internal void Reset()
        {
            prioritizedList.Clear();
            runningHandlers.Clear();
            chainTailCache.Clear();

            // TestHarness machine creation is automatic (?)
            machinesCreatedSoFar = 1;
            Chain ch = new Chain();
            ChainEvent chEvent = new ChainEvent(OperationType.Start, 0, 0);
            InsertIntoChain(chEvent, ch);
            prioritizedList.Add(ch);
            runningHandlers.Add(0, chEvent);
            
        }

        internal void recordSendEvent(ISchedulable causingSchedulable, ChainEvent causingChainEvent, ulong sendStepIndex)
        {
            ChainEvent chainEvent = new ChainEvent(OperationType.Receive, causingSchedulable.NextTargetId, sendStepIndex);
            InsertChainEvent(chainEvent, new List<ChainEvent> { causingChainEvent });

            HAX_logger.WriteLine($"recordSendEvent {chainEvent} caused by {causingChainEvent} at sendStepIndex {sendStepIndex}");
        }

        internal void recordCreateEvent(ISchedulable causingSchedulable, ChainEvent causingChainEvent)
        {
            
            ulong expectedMachineId = machinesCreatedSoFar++;
            ChainEvent chainEvent = new ChainEvent(OperationType.Start, expectedMachineId, 0);
            InsertChainEvent( chainEvent, new List<ChainEvent>{ causingChainEvent } );

            HAX_logger.WriteLine($"recordCreateEvent {chainEvent} caused by {causingChainEvent} ");
        }


        internal void recordCompleteEvent(ISchedulable causingSchedulable, ChainEvent causingChainEvent)
        {
            // TODO: I don't see how we can tell.
            causingChainEvent.chain.markCompletion(causingChainEvent);

            HAX_logger.WriteLine($"recorCompleteEvent {causingChainEvent}");
        }
        private void InsertChainEvent(ChainEvent chainEvent, List<ChainEvent> predecessors)
        {
            // TODO: Implement this stuff properly.

            // Till then, I'm gonna do some random hax which will work
            // TODO: I use non-deterministic random here :|
            List<ChainEvent> possibleInsertionPoints = predecessors.Where( x => chainTailCache.ContainsKey(x) ).ToList(); 

            if ( possibleInsertionPoints.Count > 0 && false ) // This might cause trouble.
            {
                Chain chain = chainTailCache[possibleInsertionPoints[HAX_RandomNumberGenerator.Next(possibleInsertionPoints.Count)]];
                InsertIntoChain(chainEvent, chain);
            }
            else
            {
                Chain chain = new Chain();
                InsertIntoChain(chainEvent, chain);
                prioritizedList.Insert(HAX_RandomNumberGenerator.Next(prioritizedList.Count), chain );
            }

            
        }

        private void InsertIntoChain(ChainEvent chainEvent, Chain chain)
        {
            if (chain.Tail != null && chainTailCache.ContainsKey(chain.Tail)) {
                chainTailCache.Remove(chain.Tail);
            }
            chain.Add(chainEvent);
            chainEvent.chain = chain;
            chainTailCache.Add(chain.Tail, chain);
        }

        internal bool GetNext(out ISchedulable next, out ChainEvent nextChainEvent, List<ISchedulable> enabledChoices, ISchedulable current)
        {

            Dictionary<ulong, ISchedulable> enabledMachineToSChedulable = new Dictionary<ulong, ISchedulable>();
            
            foreach (ISchedulable sc in enabledChoices)
            {
                // TODO: Should this be Id and not NextTargetId ?
                switch (sc.NextOperationType)
                {
                    case OperationType.Create:
                    case OperationType.Send:
                    case OperationType.Stop:
                        enabledMachineToSChedulable.Add(sc.Id, sc);
                        break;
                    case OperationType.Receive:
                    case OperationType.Start:
                        enabledMachineToSChedulable.Add(sc.Id, sc);
                        break;
                    default:
                        throw new Exception("OperationType is not yet supported by PCTCP: " + sc.NextOperationType);
                }
            }

            foreach ( Chain chain in prioritizedList)
            {
                ChainEvent head = chain.getHead();
                if (head != null && enabledMachineToSChedulable.ContainsKey(head.machineId))
                {
                    ISchedulable sch = enabledMachineToSChedulable[head.machineId];
                    if ( IsContinuation(head, sch) ) {
                        nextChainEvent = head;
                        next = sch;
                        recordHandlerContinue(head);
                        return true;
                    }else if (head.MatchSchedulable(sch))
                    {
                        nextChainEvent = head;
                        next = sch;
                        recordHandlerComplete(head);
                        recordHandlerStart(head);
                        return true;
                    }
                }
            }

            nextChainEvent = null;
            next = null;
            throw new Exception("This should not have happened, ideally");
            //return false;
        }


        #region Functions to know if continuation
        private void recordHandlerStart(ChainEvent sch)
        {
            runningHandlers[sch.machineId] = sch;
        }

        private void recordHandlerContinue(ChainEvent sch)
        {
            // Do nothing
        }
        private void recordHandlerComplete(ChainEvent sch)
        {
            if(runningHandlers.ContainsKey(sch.machineId))
            {
                runningHandlers[sch.machineId].MarkComplete();
            }
            runningHandlers.Remove(sch.machineId);
        }

        private bool IsContinuation(ChainEvent head, ISchedulable sch)
        {
            if (head.machineId != sch.Id)
            {
                return false;
            }
            else
            {
                switch (sch.NextOperationType)
                {
                    case OperationType.Create:
                    case OperationType.Send:
                    case OperationType.Stop:
                        return (
                            runningHandlers.ContainsKey(head.machineId) 
                            && head.otherId == runningHandlers[head.machineId].otherId);
                    default:
                        return false;

                }
            }
            
        }
        #endregion
    }
}