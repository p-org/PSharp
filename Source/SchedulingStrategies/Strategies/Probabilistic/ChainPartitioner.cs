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
        //private ulong machinesCreatedSoFar;
        private ulong highestKnownId;
        Dictionary<ChainEvent, Chain> chainTailCache;
        Dictionary<ulong, ChainEvent> runningHandlers;


        IRandomNumberGenerator HAX_RandomNumberGenerator;
        ILogger HAX_logger;
        private bool initialized;

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
            //machinesCreatedSoFar = 0;
            highestKnownId = 0;
            // TestHarness machine creation is done on first call of recordChoiceEffct
            initialized = false;
        }

        internal void recordChoiceEffect(ISchedulable currentChoice, Dictionary<ulong, ISchedulable> nextEnabledChoices, ulong scheduledSteps)
        {
            ChainEvent currentChainEvent = null;
            if (!initialized)
            {
                
                currentChainEvent = new ChainEvent(OperationType.Start, 0, 0);
                Chain ch = new Chain();

                InsertIntoChain(currentChainEvent, ch);
                //Insert
                prioritizedList.Add(ch);

                runningHandlers.Add(0, currentChainEvent);
                //machinesCreatedSoFar++;
                highestKnownId = 1;
                initialized = true;
            }
            else
            {
                currentChainEvent = runningHandlers[currentChoice.Id];
            }

            currentChainEvent.MarkComplete();

            ChainEvent createdEvent = null;
            // Record the effect of our choice.
            if (currentChainEvent.opType == OperationType.Create)
            {
                // A start operation has to be added to the chains
                //ulong expectedMachineId = machinesCreatedSoFar++;
                ulong expectedMachineId = nextEnabledChoices.Where(x => x.Key > highestKnownId ).Select( x=>x.Key).Max();
                createdEvent = new ChainEvent(OperationType.Start, expectedMachineId, expectedMachineId);
                highestKnownId = expectedMachineId;
            }
            else if (currentChainEvent.opType == OperationType.Send)
            {
                createdEvent = new ChainEvent(OperationType.Receive, currentChainEvent.otherId, scheduledSteps);
            }

            
            ISchedulable nextStepOfCurrentSchedulable = null;
            if (nextEnabledChoices.TryGetValue(currentChainEvent.machineId, out nextStepOfCurrentSchedulable)) {
                if (!IsContinuation(currentChainEvent, nextStepOfCurrentSchedulable))
                {
                    nextStepOfCurrentSchedulable = null;
                }
            }

            // Mark EventHandler as complete if the next step is not a continuation.
            ChainEvent nextStepChainEvent = null;
            if (nextStepOfCurrentSchedulable != null)
            {
                nextStepChainEvent = ChainEvent.FromISchedulable(nextStepOfCurrentSchedulable);
                    //  new ChainEvent(nextStepOfCurrentSchedulable.NextOperationType, nextStepOfCurrentSchedulable.NextTargetId, 0); // Can't be anything but 0
            }

           
            if (HAX_RandomNumberGenerator.Next(2)==0)
            {
                if (nextStepChainEvent != null) { InsertChainEvent(nextStepChainEvent, new List<ChainEvent> { currentChainEvent }); }
                if (createdEvent != null) { InsertChainEvent(createdEvent, new List<ChainEvent> { currentChainEvent }); }
            }
            else
            {
                if (createdEvent != null) { InsertChainEvent(createdEvent, new List<ChainEvent> { currentChainEvent }); }
                if (nextStepChainEvent != null) { InsertChainEvent(nextStepChainEvent, new List<ChainEvent> { currentChainEvent }); }
            }

        }
        
        private void InsertChainEvent(ChainEvent chainEvent, List<ChainEvent> predecessors)
        {
            // TODO: Implement this stuff properly.

            // Till then, I'm gonna do some random hax which will work
            // TODO: I use non-deterministic random here :|
            List<ChainEvent> possibleInsertionPoints = predecessors.Where( x => chainTailCache.ContainsKey(x) ).ToList(); 

            if ( possibleInsertionPoints.Count > 0 ) // This might cause trouble.
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
        
        internal bool GetNext(out ISchedulable next, out ChainEvent nextChainEvent, Dictionary<ulong, ISchedulable> enabledMachineToSChedulable, ISchedulable current)
        {

            nextChainEvent = null;
            next = null;
            foreach ( Chain chain in prioritizedList)
            {
                ChainEvent head = chain.getHead();
                if (head != null && enabledMachineToSChedulable.ContainsKey(head.machineId))
                {
                    ISchedulable sch = enabledMachineToSChedulable[head.machineId];
                    if (head.MatchSchedulable(sch))
                    {
                        nextChainEvent = head;
                        next = sch;
                        break;
                    }
                }
            }

            // Could do with a single variable 'ChainEvent runningChainEvent;'
            runningHandlers[next.Id] = nextChainEvent;
            

            if (next == null)
            {
                throw new Exception("This should not have happened, ideally");
            }
            else
            {
                return true;
            }
            //return false;
        }

        #region Functions to know if continuation
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
                        return true;
                    default:
                        return false;

                }
            }
        }
        #endregion
    }
}