using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    class PruneInternalControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode  { get { return TreeTraceEditor.TraceEditorMode.PruneInternal; } }

        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }

        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }

        public int ReplayLength { get { return BestTree.totalOrdering.Count; } }

        public bool strictBugEquivalenceChecking { get { return true; } }


        HashSet<int> requiredIndices;
        HashSet<int> pruneCandidates;

        public PruneInternalControlUnit()
        {
            requiredIndices = new HashSet<int>();
            pruneCandidates = new HashSet<int>();
        }

        public void PrepareForNextIteration(EventTree resultTree)
        {
            identifyPruneCandidates();
            
        }


        #region Prune inconsequentials
        internal void identifyPruneCandidates()
        {
            // Start from the back, From each leaf
            markRequired(BestTree.bugTriggeringStep); // Should I go further? Not sure how



        }

        private void markRequired(int startStep)
        {
            EventTreeNode at = BestTree.totalOrdering[startStep];
            HashSet<ulong> machinesWhichHaveReceived = new HashSet<ulong>();

            while (at != null )
            {
                requiredIndices.Add(at.totalOrderingIndex);
                if ( at.opType == SchedulingStrategies.OperationType.Receive && !machinesWhichHaveReceived.Contains(at.srcMachineId) )
                {
                    machinesWhichHaveReceived.Add(at.srcMachineId);
                }
                else if (at.opType == SchedulingStrategies.OperationType.Send && machinesWhichHaveReceived.Contains(at.srcMachineId))
                {
                    // To respect 'causality' ( A-e1->B ... A-e2->B) -> (e1<e2). This event must be received and processed
                    // This might span the whole tree. Probably need some re-scheduling to make it useful at all.

                }

                at = at.parent;
            }
        }

        #endregion
    }
}
