using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class TreeTraceEditor
    {


        // Use as read only. Let SchedulingStrategy do updates
        // TODO: Move to TraceIteration
        internal ProgramModel activeProgramModel;
        private bool isFirstStep; 
        private bool bugIsRecorded; // turns true once we set the bug.

        TraceIteration currentRun; // Make private

        internal int currentWithHoldRangeStart;
        internal int currentWithHoldRangeEnd;

        internal TraceEditorMode currentMode { get { return currentRun?.iterationMode ?? TraceEditorMode.Initial ; } }
        internal enum TraceEditorMode
        {
            Initial,

            ScheduleTraceReplay,
            //MinimizedTraceReplay,
            PrefixReplay,
            TraceEdit,
        }


        #region deprecated fields
        //private int currentIterationIndex;
        //internal bool ranOutOfEvents;
        //int eventSeenThisTime;
        #endregion

        // Deletion Tracking
        internal class TraceIteration {

            internal EventTree guideTree;
            internal int totalOrderingIndex = 0;

            internal EventTreeNode activeNode;
            private int activeNode_boolIndex;
            private int activeNode_intIndex;
            internal HashSet<int> deletedIndices;
            internal HashSet<int> deletedLoopReceiveIndices;
            //Dictionary<int,int> deletedLoopReceiveIndices; // Maps receive Event to sendEvent
            //internal Dictionary<string,int> delayedLoopEvents; // Maps EventSig to the send Event - Needed to fix matchingSendId

            internal int HAX_deletedCount;

            internal TraceEditorMode iterationMode;
            private int replayLength;
            internal bool strictBugEquivalenceChecking;
            

            public Dictionary<Monitor, Tuple<int,int>> HotMonitors { get; internal set; } // Maps some monitor identifier to the totalOrderingIndex

            public TraceIteration(EventTree GuideTree, TraceEditorMode currentMode)
            {
                guideTree = GuideTree;
                totalOrderingIndex = 0;
                iterationMode = currentMode;
                deletedIndices = new HashSet<int>();
                //deletedLoopReceiveIndices = new Dictionary<int, int>();
                deletedLoopReceiveIndices = new HashSet<int>();
                //delayedLoopEvents = new Dictionary<string, int>();
                HotMonitors = new Dictionary<Monitor, Tuple<int, int>>();
                setActiveNode(null);
                HAX_deletedCount = 0;

                replayLength = guideTree?.totalOrdering.Count ?? -1 ;
                strictBugEquivalenceChecking = true;
            }

            public EventTreeNode peek()
            {
                return guideTree.totalOrdering[totalOrderingIndex];
            }

            public void next()
            {
                totalOrderingIndex++;
            }

            public void addDeletion(int index, bool isLoopEvent)
            {
                deletedIndices.Add(index);
                if (isLoopEvent)
                {
                    deletedLoopReceiveIndices.Add(index);
                }

            }
            public bool checkDeleted(EventTreeNode etn)
            {
                if (deletedIndices.Contains(etn.totalOrderingIndex))
                {
                    if (etn.createdChild != null) { deletedIndices.Add(etn.createdChild.totalOrderingIndex); }
                    if (etn.directChild  != null) { deletedIndices.Add(etn.directChild.totalOrderingIndex); }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            internal bool isLoopSendEvent(EventTreeNode eventNode, out int correspondingSendId)
            {
                correspondingSendId = -1;
                if (eventNode.opType != OperationType.Send || eventNode.srcMachineId != eventNode.targetMachineId) // TODO: Improve with explicit notion of LoopSend, LoopReceive
                {
                    return false;
                }
                
                EventTreeNode at = eventNode;
                while (at!=null)
                {
                    if(at.opType == OperationType.Receive)
                    {
                        if (deletedLoopReceiveIndices.Contains(at.totalOrderingIndex)/*deletedLoopReceiveIndices.ContainsKey(at.totalOrderingIndex)*/)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    at = at.parent;
                }

                return false;
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
                // TODO: DO a coarse match. Remember to include loopEvent correction
                return true;
            }

            internal bool reachedEnd()
            {
                if (guideTree?.totalOrdering != null)
                {
                    return ( replayLength >= 0 && totalOrderingIndex > Math.Min(guideTree.totalOrdering.Count-1, replayLength) );
                    //switch (iterationMode)
                    //{
                    //    case TraceEditorMode.PrefixReplay:
                    //        return totalOrderingIndex > replayLength;
                    //    default:
                    //        return (totalOrderingIndex >= guideTree.totalOrdering.Count );
                    //}
                }
                else
                {
                    return false;
                }

            }

            internal bool checkWithHeld(EventTreeNode etn)
            {
                return guideTree?.checkWithHeld(etn) ?? false;
            }

            internal void setReplayLength(int replayLength, bool strictBugEquivalenceChecking)
            {
                this.replayLength = replayLength;
                this.strictBugEquivalenceChecking = strictBugEquivalenceChecking;
            }

            internal bool checkLivenessViolation(out int bugIndexOfGuideTree, out int BugIndexOfCurrentTree)
            {
                bool result = false;
                bugIndexOfGuideTree = 0;
                BugIndexOfCurrentTree = 0;


                Monitor hotMonitor = null;
                foreach (Monitor m in HotMonitors.Keys)
                {
                    if (m.HAX_IsLivenessTemperatureAboveTreshold())
                    {
                        hotMonitor = m;
                        bugIndexOfGuideTree = HotMonitors[hotMonitor].Item1;
                        BugIndexOfCurrentTree = HotMonitors[hotMonitor].Item2;
                        result = true;
                        break;
                    }
                }

                return result;    
            }
        }


        public TreeTraceEditor()
        {
            bugIsRecorded = false;
        }


        public void reset()
        {
            //eventSeenThisTime = 0;
            currentWithHoldRangeStart = 0;
            currentWithHoldRangeEnd = 0;
            activeProgramModel = new ProgramModel();
            isFirstStep = true;
        }

        #region old prepare for next iteration
        //private bool prepareForPruneSuffixIteration(EventTree guideTree)
        //{
        //    currentRun = new TraceIteration(guideTree, TraceEditorMode.PruneSuffix);
        //    reset();

        //    //currentMode = TraceEditorMode.MinimizedTraceReplay;
        //    return true;
        //}

        //public bool prepareForMinimalTraceReplay(EventTree guideTree)
        //{

        //    currentRun = new TraceIteration(guideTree, TraceEditorMode.MinimizedTraceReplay);
        //    reset();

        //    //currentMode = TraceEditorMode.MinimizedTraceReplay;
        //    return true;
        //}

        //internal void prepareForMinimalTraceReplay()
        //{
        //    prepareForMinimalTraceReplay(activeProgramModel.constructTree);
        //}

        //public bool prepareForCriticalTransitionSearchIteration(EventTree guideTree, int left, int right)
        //{
        //    currentRun = new TraceIteration(guideTree, TraceEditorMode.CriticalTransitionSearch);
        //    reset();

        //    int criticalTransitionIndex = (left + right) / 2;
        //    currentRun.setReplayLength(criticalTransitionIndex);

        //   //currentMode = TraceEditorMode.CriticalTransitionSearch;
        //    return true;
        //}
        //public bool prepareForCriticalTransitionSearchIteration(EventTree guideTree, int replayLength)
        //{

        //    currentRun = new TraceIteration(guideTree, TraceEditorMode.PrefixReplay);
        //    reset();
        //    currentRun.setReplayLength(replayLength);

        //    return true;
        //}


        #endregion

        #region prepare for next iteration

        public bool prepareForNextIteration(ControlUnits.ITraceEditorControlUnit controlUnit)
        {
            switch (controlUnit.RequiredTraceEditorMode)
            {
                case TraceEditorMode.ScheduleTraceReplay:
                    return prepareForScheduleTraceReplay(controlUnit.strictBugEquivalenceChecking);
                case TraceEditorMode.PrefixReplay:
                    return prepareForPrefixReplay(controlUnit.BestTree, controlUnit.ReplayLength, controlUnit.strictBugEquivalenceChecking);
                case TraceEditorMode.TraceEdit:
                    return prepareForTraceEditIteration(controlUnit.BestTree, controlUnit.Left, controlUnit.Right);
                default:
                    throw new ArgumentException("TraceEditor cannot do nextIteration in mode " + controlUnit.RequiredTraceEditorMode);
            }
        }

        private void copyMetaBetweenTrees(EventTree from, EventTree to)
        {
            to.criticalTransitionStep = from.criticalTransitionStep; // TODO:: Is this even right? It could change because of the edits.
        }

        public bool prepareForScheduleTraceReplay(bool strictBugEquivalenceChecking)
        {
            currentRun = new TraceIteration(null, TraceEditorMode.ScheduleTraceReplay);

            reset();
            
            return true;
        }
        public bool prepareForPrefixReplay(EventTree guideTree, int replayLength, bool strictBugEquivalenceChecking)
        {

            currentRun = new TraceIteration(guideTree, TraceEditorMode.PrefixReplay);
            reset();
            currentRun.setReplayLength(replayLength, strictBugEquivalenceChecking);

            copyMetaBetweenTrees(guideTree, activeProgramModel.constructTree);

            return true;
        }

        public bool prepareForTraceEditIteration(EventTree guideTree, int left, int right)
        {
            currentRun = new TraceIteration(guideTree, TraceEditorMode.TraceEdit);
            reset();

            copyMetaBetweenTrees(guideTree, activeProgramModel.constructTree);
            currentWithHoldRangeStart = left;
            currentWithHoldRangeEnd = right;
            
            return true;
        }
        #endregion
        
        
        internal bool ShouldDeliverEvent(Event e)
        {
            //TODO: Properly
            if (currentRun.reachedEnd()){
                return true;
            }
            bool isLoopEvent = false;
            bool deliver = true;
            System.Console.WriteLine(e.GetType().FullName);
            if ( eventIsCandidateToBeDropped(e, out isLoopEvent) )
            {
                int stepIndex = activeProgramModel.constructTree.totalOrdering.Count-1;
                if (currentRun.checkWithHeld(currentRun.activeNode))
                {
                    deliver = false;
                    // Don't do optimization hacks right now. It'll get confusing. +  we're doing bsearch anyway
                    //if (eventSeenThisTime == currentWithHoldCandidate)
                    //{
                    //    // Don't waste this deletion run. It's already 
                    //    currentWithHoldCandidate++;
                    //}

                }
                else if ( stepIsInPruneRange(stepIndex) )
                {
                    deliver = false;
                }
            }

            if (deliver)
            {
                return true;
            }
            else
            { 

                if (currentRun.activeNode != null && currentRun.activeNode.createdChild != null)
                {
                    currentRun.addDeletion(currentRun.activeNode.createdChild.totalOrderingIndex, isLoopEvent);
                }
                // We should not have to care about this.
                // string eventSig = getEventSignature(e);
                if (isLoopEvent)
                {
                    // Let it be delivered so we traverse iteration
                    // A loop event which traverses isn't actually withheld
                    return true;
                }
                else
                {
                    recordEventWithHeld(); 
                    return false;
                }
            }
        }

        private bool stepIsInPruneRange(int stepIndex)
        {
            return (stepIndex >= currentWithHoldRangeStart  && stepIndex <= currentWithHoldRangeEnd);
        }

        private string getEventSignature(Event e)
        {
            return e.ToString() + "::" + currentRun.activeNode.srcMachineId + "::" + currentRun.activeNode.targetMachineId; 
        }

        internal bool reachedEnd()
        {
            return currentRun.reachedEnd();
        }

        private bool eventIsCandidateToBeDropped(Event e, out bool isLoopEvent)
        {
            if(
                //e.GetType().FullName == "Microsoft.PSharp.Timers.TimerElapsedEvent" || 
                e.GetType().FullName == "ReplicatingStorage.SyncTimer+Timeout" ||
                e.GetType().FullName == "Raft.PeriodicTimer+Timeout" || e.GetType().FullName == "Raft.ElectionTimer+Timeout" ||
                e.GetType().FullName == "ChainReplication.Client+Update" || e.GetType().FullName == "ChainReplication.Client+Query"
                )
            {
                isLoopEvent = false;
                return true;
            }
            else if( e.GetType().FullName == "Microsoft.PSharp.Timers.RepeatTimeout"  )
            {
                isLoopEvent = true;
                return true;
            }
            else
            {
                isLoopEvent = false;
                return false;
            }
        }



        // Queries
        #region program model queries
        internal bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            Dictionary<ulong, ISchedulable> enabledChoices = choices.Where(x => x.IsEnabled).ToDictionary(x => x.Id);
            // Skip everything deleted ?
            // TODO: Can we be smarter? Check if any of the deleted ones could actually be one worth delivering
            //      i.e., matches the enabledOne, whereas the next non-deleted one does not match (??) idk.
            while (!currentRun.reachedEnd() && ( currentRun.checkDeleted(currentRun.peek()) || currentRun.peek().opType==OperationType.HAX_Dummy) )
            {
                EventTreeNode currentEvent = currentRun.peek();
                int correspondingSendId = -1;
                if ( currentRun.isLoopSendEvent(currentEvent, out correspondingSendId) )
                {
                    int stepIndex = activeProgramModel.constructTree.totalOrdering.Count - 1;
                    if ( stepIsInPruneRange(stepIndex+1) ) 
                    {
                        // This receive should also be skipped. Mark it as skipped
                        currentRun.deletedLoopReceiveIndices.Add(currentEvent.createdChild.totalOrderingIndex);

                        // Shall we insert a dummy, withheld step for the heck of it? 
                        // Might prevent this from degrading to a linear search when the loop is the limiting factor.
                        EventTreeNode insertNode = new EventTreeNode(OperationType.HAX_Dummy, currentEvent.srcMachineId, currentEvent.targetMachineId, currentEvent.otherId );
                        activeProgramModel.forceInsertEventNode(insertNode);
                        // currentRun.addDeletion(insertNode.totalOrderingIndex, false); // Does this do anything? 
                    }
                    else
                    {
                        currentRun.deletedIndices.Remove(currentEvent.createdChild.totalOrderingIndex);
                        // TOOD: Fix the receivedId here or in the coarse match?   
                    }

                }
                if (currentEvent.opType == OperationType.HAX_Dummy)
                {
                    EventTreeNode insertNode = new EventTreeNode(OperationType.HAX_Dummy, currentEvent.srcMachineId, currentEvent.targetMachineId, currentEvent.otherId);
                    activeProgramModel.forceInsertEventNode(insertNode);
                }

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
                    else
                    {
                        // Hitting this would mean our program model is wrong,
                        // We can't match an enabled node in our program model to the scheduler
                        // The guidetree has nothing to do with it. That only comes into play during coarseMatch
                        throw new ArgumentException("This really shouldn't happen. ");
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

        internal void recordEventWithHeld()
        {
            activeProgramModel.recordEventWithHeld();
        }
        internal void recordSchedulingChoiceStart(ISchedulable next, ulong scheduledSteps)
        {
            activeProgramModel.recordSchedulingChoiceStart(next, scheduledSteps);
        }

        internal void recordSchedulingChoiceResult(ISchedulable current, Dictionary<ulong, ISchedulable> enabledChoices, ulong scheduledSteps)
        {
            if (isFirstStep)
            {   // Needs some help here.
                EventTreeNode root = activeProgramModel.initializeWithTestHarnessMachine(current.Id); // Is this correct?
                currentRun?.setActiveNode(root);
                isFirstStep = false;
            }
            activeProgramModel.recordSchedulingChoiceResult(current, enabledChoices, scheduledSteps);
            if (currentRun!=null && currentRun.activeNode != null)
            {
                currentRun.next();
            }
        }

        internal void recordResult(bool bugFound, ScheduleTrace scheduleTrace)
        {
            int guideTreeIndex = -1, activeProgramModelIndex = -1;
            bool bugActuallyFound = false;
            if (bugFound)
            {
                // TODO: Verify bug equivalence
                if( !currentRun.checkLivenessViolation(out guideTreeIndex, out activeProgramModelIndex))
                {
                    guideTreeIndex = currentRun.totalOrderingIndex;
                    activeProgramModelIndex = activeProgramModel.constructTree.totalOrdering.Count - 1;
                }
                if (bugIsRecorded) {
                    bugActuallyFound = verifyBugEquivalence(guideTreeIndex);
                }
                else
                {   // We don't actually know what the bug is.
                    bugActuallyFound = true;
                    bugIsRecorded = true;
                }
            }

            activeProgramModel.getTree().setResult(bugActuallyFound, scheduleTrace, activeProgramModelIndex);
            
        }

        private bool verifyBugEquivalence(int guideTreeIndex)
        {
            if( currentRun.strictBugEquivalenceChecking)
            {
                return currentRun.guideTree.bugTriggeringStep == guideTreeIndex; 
            }
            else
            {
                return true;
            }
            //switch (currentRun.iterationMode)
            //{
            //    case TraceEditorMode.PrefixReplay:
            //        // Return true - Allow the reply ( if any ) to verify. Else nothing we can do.
            //        return true;
            //    default:
            //        return currentRun.guideTree.bugTriggeringStep == guideTreeIndex;
            //}
        }

        internal void RecordIntegerChoice(int next)
        {
            activeProgramModel.RecordIntegerChoice(next);
        }

        internal void RecordBooleanChoice(bool next)
        {
            activeProgramModel.RecordBooleanChoice(next);
        }


        internal void recordEnterHotstate(Monitor monitor)
        {
            currentRun.HotMonitors.Add(monitor, new Tuple<int, int>(currentRun.totalOrderingIndex, activeProgramModel.constructTree.totalOrdering.Count - 1));
        }

        internal void recordExitHotState(Monitor monitor)
        {
            currentRun.HotMonitors.Remove(monitor);
        }


        #endregion
    }
}