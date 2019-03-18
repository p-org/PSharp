using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class TreeTraceEditor
    {


        private EventTree BestGuideTree;

        // Use as read only. Let SchedulingStrategy do updates
        // TODO: Move to TraceIteration
        internal ProgramModel activeProgramModel;

        //private int currentIterationIndex;

        internal ulong actualStepsExecuted; // Count of how many steps we've actually executed ( is this even needed here? Let the strategy do it? )

        int currentWithHoldCandidate; // 1 indexed :p
        HashSet<int> withHeldCandidates;

        internal bool ranOutOfEvents;
        int eventSeenThisTime;
        TraceIteration currentRun; // Make private





        // Deletion Tracking
        internal class TraceIteration {

            EventTree guideTree;
            internal int totalOrderingIndex = 0;

            internal EventTreeNode activeNode;
            private int activeNode_boolIndex;
            private int activeNode_intIndex;
            HashSet<int> deletedIndices;
            internal int HAX_deletedCount;

            public TraceIteration(EventTree GuideTree)
            {
                guideTree = GuideTree;
                totalOrderingIndex = 0;
                deletedIndices = new HashSet<int>();
                setActiveNode(null);
                HAX_deletedCount = 0;
            }

            public EventTreeNode peek()
            {
                return guideTree.totalOrdering[totalOrderingIndex];
            }

            public void next()
            {
                totalOrderingIndex++;
            }

            public void addDeletion(int index)
            {
                deletedIndices.Add(index);
            }
            public bool checkDeleted(EventTreeNode etn)
            {
                if (deletedIndices.Contains(etn.totalOrderingIndex))
                {
                    if (etn.directChild != null) { deletedIndices.Add(etn.directChild.totalOrderingIndex); }
                    if (etn.createdChild != null) { deletedIndices.Add(etn.createdChild.totalOrderingIndex); }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void setActiveNode(EventTreeNode match)
            {
                activeNode = match;
                activeNode_boolIndex = 0;
                activeNode_intIndex = 0;
            }
            public bool getNondetBooleanChoice(out bool nextBool)
            {

                if (activeNode != null && activeNode.getBoolean(activeNode_boolIndex, out nextBool))
                {
                    activeNode_boolIndex++;
                    return true;
                }
                else
                {
                    nextBool = false;
                    return false;
                }
            }

            public bool getNondetIntegerChoice(out int nextInt)
            {

                if (activeNode != null && activeNode.getInteger(activeNode_intIndex, out nextInt))
                {
                    activeNode_intIndex++;
                    return true;
                }
                else
                {
                    nextInt = 0;
                    return false;
                }
            }

            internal bool coarseMatch(EventTreeNode etn)
            {
                // TODO: DO a coarse match
                return true;
            }

            internal bool reachedEnd()
            {
                if(guideTree?.totalOrdering != null)
                {
                    return totalOrderingIndex >= guideTree.totalOrdering.Count;
                }
                else
                {
                    return false;
                }
                
            }
        }


        public TreeTraceEditor(EventTree guideTree)
        {
            BestGuideTree = guideTree;
            currentWithHoldCandidate = 0;
            ranOutOfEvents = false;
            withHeldCandidates = new HashSet<int>();
            reset();
        }

        public bool prepareForNextIteration(bool bugFound)
        {
            if (bugFound)
            {
                BestGuideTree = activeProgramModel.getTree();
                // We need to increment because the Send event will appear regardless. We have to actively withhold it each time.
                if (currentWithHoldCandidate != 0) // Why pollute?
                {
                    withHeldCandidates.Add(currentWithHoldCandidate);
                }


            }
            currentWithHoldCandidate++;

            ranOutOfEvents = (eventSeenThisTime < currentWithHoldCandidate);
            reset();
            return ranOutOfEvents;
        }


        public void reset()
        {
            eventSeenThisTime = 0;
            currentRun = new TraceIteration(BestGuideTree);
            activeProgramModel = new ProgramModel();
        }


        internal EventTree getGuideTree()
        {
            return BestGuideTree;
        }

        internal bool ShouldDeliverEvent(Event e)
        {
            //TODO: Properly
            if (currentRun.reachedEnd()){
                return true;
            }
            System.Console.WriteLine(e.GetType().FullName);
            //if (e.GetType().FullName == "ReplicatingStorage.RepairTimer+Timeout")
            if ( eventIsCandidateToBeDropped(e) )
            {
                eventSeenThisTime++;
                if (eventSeenThisTime == currentWithHoldCandidate || withHeldCandidates.Contains(eventSeenThisTime) )
                {
                    if (currentRun.activeNode != null //&& currentRun.activeNode.createdChild!=null
                        ) {
                        currentRun.addDeletion(currentRun.activeNode.createdChild.totalOrderingIndex);
                    }
                    else
                    {   // TODO: deal with this better
                        throw new ArgumentException("How did we come up with an external event that isn't in the trace?");
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        internal bool reachedEnd()
        {
            return currentRun.reachedEnd();
        }

        private bool eventIsCandidateToBeDropped(Event e)
        {
            return (
                e.GetType().FullName == "ReplicatingStorage.SyncTimer+Timeout" 
                );
        }



        // Queries
        #region program model queries
        internal bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            Dictionary<ulong, ISchedulable> enabledChoices = choices.Where(x => x.IsEnabled).ToDictionary(x => x.Id);
            // Skip everything deleted ?
            // TODO: Can we be smarter? Check if any of the deleted ones could actually be one worth delivering
            //      i.e., matches the enabledOne, whereas the next non-deleted one does not match (??) idk.
            while (!currentRun.reachedEnd() && currentRun.checkDeleted(currentRun.peek()))
            {
                currentRun.HAX_deletedCount++;
                currentRun.next();
            }

            // Now we need to see if we can schedule this event. If it doesn't exist, what do we do? Ignore?
            next = null;

            bool matched = false;
            while (!currentRun.reachedEnd() && !matched)
            {
                EventTreeNode currentNode = currentRun.peek();
                if (enabledChoices.ContainsKey(currentNode.srcMachineId))
                {

                    ISchedulable candidate = enabledChoices[currentNode.srcMachineId];
                    EventTreeNode potentialMatch;
                    if (activeProgramModel.getTreeNodeFromISchedulable(candidate, out potentialMatch))
                    {
                        if (currentRun.coarseMatch(potentialMatch))
                        {
                            // It's likely a match. 
                            currentRun.setActiveNode(currentNode);
                            currentRun.next();
                            // TODO: Should we backtrack?
                        }
                        else
                        {   // It's likely not the one we were looking for. 
                            // We could always assume it is and backtrack, but better keep coarse matching coarse.
                            // For now, just deliver this message assuming it's an added, unexpected extra.
                            // TODO: What if it's the next one? And the one we're expecting never appears because 
                            //  it's a hidden causal relation which got deleted :(
                            currentRun.setActiveNode(null); // TODO: Should this be currentNode anyway? Don't think so
                        }
                    }

                    next = candidate;
                    matched = true;

                }
                else
                {   // The machine we expected is not here. Assume it got deleted and skip to next one ?
                    currentRun.next();
                }

            }

            return matched;

        }

        internal bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            return currentRun.getNondetBooleanChoice(out next);
        }
        internal bool GetNextIntegerChoice(int maxValue, out int next)
        {
            bool result = currentRun.getNondetIntegerChoice(out next);
            if (result)
            {
                if (next > maxValue)
                {
                    throw new ArgumentException("Val is less than maxval");
                }
                return true;
            }
            else
            {
                return false;
            }

        }
        #endregion

        #region program model updates

        internal void recordSchedulingChoiceStart(ISchedulable next, ulong scheduledSteps)
        {
            activeProgramModel.recordSchedulingChoiceStart(next, scheduledSteps);
        }

        internal void recordSchedulingChoiceResult(ISchedulable current, Dictionary<ulong, ISchedulable> enabledChoices, ulong scheduledSteps)
        {
            // We need to delete the withheld events in the trace as well.
            bool wasWithHeld = (currentRun.activeNode!=null && currentRun.activeNode.createdChild!=null) ? currentRun.checkDeleted(currentRun.activeNode.createdChild) : false;
            activeProgramModel.recordSchedulingChoiceResult(current, enabledChoices, scheduledSteps, wasWithHeld);
        }

        internal void RecordIntegerChoice(int next)
        {
            activeProgramModel.RecordIntegerChoice(next);
        }

        internal void RecordBooleanChoice(bool next)
        {
            activeProgramModel.RecordBooleanChoice(next);
        }

        #endregion
    }
}