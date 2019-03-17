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


        private EventTree GuideTree;

        // Use as read only. Let SchedulingStrategy do updates
        // TODO: Move to TraceIteration
        private ProgramModel activeProgramModel;

        //private int currentIterationIndex;

        internal ulong actualStepsExecuted; // Count of how many steps we've actually executed ( is this even needed here? Let the strategy do it? )

        HashSet<int> WithHeldEventIndices;
        int currentWithHoldCandidate;

        bool ranOutOfEvents;
        int eventSeenThisTime;
        TraceIteration currentRun;

        

        // Deletion Tracking
        internal class TraceIteration {

            EventTree guideTree;
            internal int totalOrderingIndex = 0;

            internal EventTreeNode activeMatch;
            private int activeMatch_boolIndex;
            private int activeMatch_intIndex;
            HashSet<int> deletedIndices;

            public TraceIteration(EventTree GuideTree)
            {
                guideTree = GuideTree;
                totalOrderingIndex = 0;
                deletedIndices = new HashSet<int>();
                setActiveMatch(null);
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
                if (etn.parent!=null && deletedIndices.Contains(etn.parent.totalOrderingIndex))
                {
                    deletedIndices.Add(etn.totalOrderingIndex);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void setActiveMatch(EventTreeNode match)
            {
                activeMatch = match;
                activeMatch_boolIndex = 0;
                activeMatch_intIndex = 0;
            }
            public bool getNondetBooleanChoice(out bool nextBool)
            {

                if (activeMatch != null && activeMatch.getBoolean(activeMatch_boolIndex, out nextBool))
                {
                    activeMatch_boolIndex++;
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

                if (activeMatch != null && activeMatch.getInteger(activeMatch_intIndex, out nextInt))
                {
                    activeMatch_intIndex++;
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
                return totalOrderingIndex >= guideTree.totalOrdering.Count;
            }
        }


        public TreeTraceEditor(EventTree guideTree, ProgramModel programModel)
        {
            GuideTree = guideTree;
            activeProgramModel = programModel;
            WithHeldEventIndices = new HashSet<int>();
            currentWithHoldCandidate = -1;
            ranOutOfEvents = false;

            reset();
        }

        public bool prepareForNextIteration(bool bugFound)
        {
            if (bugFound)
            {
                WithHeldEventIndices.Add(currentWithHoldCandidate);
            }
            currentWithHoldCandidate++;
            ranOutOfEvents = ranOutOfEvents && (eventSeenThisTime > currentWithHoldCandidate);
            reset();
            return ranOutOfEvents;
        }

        public void reset()
        {
            eventSeenThisTime = 0;
            currentRun = new TraceIteration(GuideTree);
        }


        // Queries
        internal bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            Dictionary<ulong, ISchedulable> enabledChoices = choices.Where(x=>x.IsEnabled).ToDictionary(x => x.Id);
            // Skip everything deleted ?
            // TODO: Can we be smarter? Check if any of the deleted ones could actually be one worth delivering
            //      i.e., matches the enabledOne, whereas the next non-deleted one does not match (??) idk.
            while(!currentRun.reachedEnd() && currentRun.checkDeleted(currentRun.peek()))
            {
                currentRun.next();
            }

            // Now we need to see if we can schedule this event. If it doesn't exist, what do we do? Ignore?
            next = null;

            bool matched = false;
            while( !matched )
            {
                EventTreeNode currentNode =  currentRun.peek();
                if (enabledChoices.ContainsKey(currentNode.srcMachineId)) {

                    ISchedulable candidate = enabledChoices[currentNode.srcMachineId];
                    EventTreeNode potentialMatch;
                    if (activeProgramModel.getTreeNodeFromISchedulable(candidate, out potentialMatch)) {
                        if (currentRun.coarseMatch(potentialMatch))
                        {
                            // It's likely a match. 
                            currentRun.setActiveMatch(currentNode);
                            currentRun.next();
                            // TODO: Should we backtrack?
                        }
                        else
                        {   // It's likely not the one we were looking for. 
                            // We could always assume it is and backtrack, but better keep coarse matching coarse.
                            // For now, just deliver this message assuming it's an added, unexpected extra.
                            // TODO: What if it's the next one? And the one we're expecting never appears because 
                            //  it's a hidden causal relation which got deleted :(
                            currentRun.setActiveMatch(null); // TODO: Should this be currentNode anyway? Don't think so
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



        internal bool ShouldDeliverEvent(Event e)
        {
            //TODO: Properly
            System.Console.WriteLine(e.GetType().FullName);
            if (e.GetType().FullName == "Environment.TickEvent")
            {
                if (eventSeenThisTime == currentWithHoldCandidate || WithHeldEventIndices.Contains(eventSeenThisTime))
                {
                    currentRun.addDeletion(currentRun.totalOrderingIndex);
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


    }
}