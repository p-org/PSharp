using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    class TraceEditAndVerifyControlUnit : ITraceEditorControlUnit
    {

        /*
         * We can't do a normal binary search here because the array we're searching on changes 
         *  Further, There's merit ( and hardly any demerits ) to searching left first, 
         *  and considering the result when searching the right ( No issue of the conquer step not reproducing the bug ).
         * So:
         *  Left : The index of the last event in BestTree which was withhold
         *  Right: The index of the last event that we may try to withhold in this iteration.
         *  bSearchBounds: Holds the (Left, Right) pairs which still need to be considered.
         *      Since the array may change, these left and right may refer to different events in the updated minTrace.
         *      Assuming 
         *      Left should be max(Left, HighestIndexOfWithheldEventsInPreviousRun)
         *      If we are at the last element in the stack, Right should be atleast minTrace.Count ( Or INF ).
         *      
         *  HAX: Apply any ordering on the trace being constructed - 
         *       Everything to the left of Left will match ( since we're not editing ),
         *       We still cover every event
         */

        /*
         * New question: 
         * 1) How do you verify that we hit the same bug? 
         * 2) The bug is not hit because of mindless deletion?
         *  Idea: Given critical-transition step index Sc and bug Triggering Step index Sb, We can determine the following
         *  1. A replay run Rr hits the same bug as the guide run Rg iff Rr.Sb corresponds to Rg.Sb
         *  2. The deletion of an event at step Rg.Se in the guide run desirable if
         *      a. By replaying the Rg till max(Rg.Sb, Rg.Sc) reproduces the same bug.
         *      b.  Rg.Se < Rg.Sc and doing random walks from Rg.Se does not reproduce the same bug.
         *      
         *      Is this true: A deletion is desirable if Rg.Se > Rg.Sc    ( It shouldn't affect the outcome )
         */


        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return activeControlUnit.RequiredTraceEditorMode; } }

        // TODO: This isn't actually the best tree.
        public EventTree BestTree { get { return activeControlUnit.BestTree; } }
        public int Left { get { return activeControlUnit.Left; } }
        public int Right { get { return activeControlUnit.Right; } }

        public bool Valid { get { return traceEditControl.Valid;  }  }

        public bool Completed { get { return traceEditControl.Completed; } }

        public int ReplayLength { get { return activeControlUnit.ReplayLength; } }
        public bool strictBugEquivalenceChecking { get { return activeControlUnit.strictBugEquivalenceChecking; }  }

        
        private EventTree candidateTree;
        private ITraceEditorControlUnit activeControlUnit;


        internal TraceEditControlUnit traceEditControl;
        private int currentStage;
        private bool isInited;
        //private int bugReproductionReplaysLeft;

        public TraceEditAndVerifyControlUnit()
        {
            traceEditControl = new TraceEditControlUnit();// This we have to re-use
            currentStage = -1;
            activeControlUnit = null;
            isInited = false;

            //bugReproductionReplaysLeft = 0;
        }

        private void GoToNextTraceEditIteration(EventTree resultTree)
        {
            currentStage = 0;
            activeControlUnit = traceEditControl;
            activeControlUnit.PrepareForNextIteration(resultTree);
        }

        public void PrepareForNextIteration(EventTree resultTree)
        {
            if (!isInited)
            {
                Initialize(resultTree);
                //activeControlUnit.PrepareForNextIteration(resultTree);
            }
            else if (currentStage == 0)
            {   // We just did a traceEdit
                //if(resultTree.withHeldSendIndices.Count > 0) // ;// If we withHeld anything 
                // TODO: Optimize. It may be that we didn't hit anything in the window
                if (resultTree.reproducesBug() && resultTree.withHeldSendIndices.Count > 0)
                {
                    currentStage = 1;
                    candidateTree = resultTree;
                    activeControlUnit = new MinimalTraceReplayControlUnit(MinimizationStrategy.NREPLAYSREQUIRED, true);
                    activeControlUnit.PrepareForNextIteration(candidateTree);
                }
                else
                {
                    GoToNextTraceEditIteration(resultTree);
                }
            }
            else if(currentStage == 1)
            {
                activeControlUnit.PrepareForNextIteration(resultTree);
                if (resultTree.reproducesBug()) {
                    if (activeControlUnit.Completed)
                    {
                        GoToNextTraceEditIteration(resultTree);
                        //currentStage = 2;
                        //int replayTill = getReplayLengthForIndependenceCheck(candidateTree);
                        //if (replayTill > 0)
                        //{
                        //    bugReproductionReplaysLeft = MinimizationStrategy.NREPLAYSREQUIRED;
                        //    activeControlUnit = new PrefixReplayControlUnit(1, replayTill, false);
                        //    activeControlUnit.PrepareForNextIteration(candidateTree);
                        //}
                        //else
                        //{
                        //    GoToNextTraceEditIteration(resultTree);
                        //}
                    }
                }
                else
                {   // No bug -> Go back to traceEditor
                    GoToNextTraceEditIteration(resultTree);
                }
            }
            else if (currentStage==2)
            {
                throw new NotImplementedException("This part has to be implemented");
                //// This is the difficult one - we want atleast one iteration where the bug is not reproduced.
                //activeControlUnit.PrepareForNextIteration(candidateTree);
                //bugReproductionReplaysLeft--;
                //if (resultTree.reproducesBug())
                //{
                //    if(bugReproductionReplaysLeft > 0)
                //    {   // Good, more tries to 
                //        int replayTill = candidateTree.withHeldSendIndices[candidateTree.withHeldSendIndices.Count - 1];
                //        activeControlUnit = new PrefixReplayControlUnit(1, replayTill, false);
                //        activeControlUnit.PrepareForNextIteration(candidateTree);
                //    }
                //    else
                //    {
                //        // Bad. Set the candidate tree to not reproduce the bug ( so that TraceEdit gets the right message )
                //        candidateTree.setResult(false, candidateTree.getActualTrace(), -1); // candidateTree.bugTriggeringStep); // Does this matter?

                //        GoToNextTraceEditIteration(candidateTree);
                //    }
                //}
                //else
                //{   // GREAT SUCCESS! We can go back to traceEdit with success
                //    BestTree = candidateTree;

                //    GoToNextTraceEditIteration(candidateTree);
                //}
            }
            else
            {
                throw new ArgumentException("TraceEditAndVerifyControlUnit is in invalid stage: " + currentStage);
            }

        }

        private int getReplayLengthForIndependenceCheck(EventTree guideTree)
        {
            int whichIndex = guideTree.withHeldSendIndices.Count - 1;
            // Return atleast 0
            while (whichIndex >= 0 && guideTree.withHeldSendIndices[whichIndex] >= guideTree.criticalTransitionStep)
            {
                whichIndex--;
            }
            return (whichIndex > 0) ? guideTree.withHeldSendIndices[whichIndex] : -1; 
            
        }

        private void Initialize(EventTree guideTree)
        {
            
            if (guideTree.reproducesBug())
            {
                GoToNextTraceEditIteration(guideTree);
                //currentStage = 0;
                //activeControlUnit = traceEditControl;
                
            }
            else
            {
                throw new ArgumentException("Can't do trace edit if guideTree doesn't reproduce bug");
            }

            candidateTree = null; // Do not set candidateTree. This is the BestTree. Not a candidate
            isInited = true;
        }
    }
}
