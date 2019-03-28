using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    internal class CriticalTransitionSearchControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.PrefixReplay; } }
        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }

        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }

        public int ReplayLength { get { return (Left + Right) / 2; } }

        // Can't be true, because then we won't find the actual critical transition if it's before the bugTriggeringStep
        public bool strictBugEquivalenceChecking { get { return false; } } 

        private bool initialized;
        private int nReplaysRequired;
        private ITraceEditorControlUnit minTraceReplay;

        internal CriticalTransitionSearchControlUnit(int nReplaysToVerify)
        {
            initialized = false;
            Completed = false;
            Valid = true;
            nReplaysRequired = nReplaysToVerify;
            minTraceReplay = new MinimalTraceReplayControlUnit(0, true); // Dummy to fix the control flow
        }

        private void Initialize(EventTree resultTree)
        {
            initialized = true;
            if (resultTree.reproducesBug())
            {
                BestTree = resultTree;
                Left = 0;
                Right = resultTree.totalOrdering.Count - 1;
                Valid = true;
                Completed = false;
            }
            else
            {
                Valid = false;
                Completed = true;
            }

        }

        private void refineBounds(EventTree resultTree)
        {
            if (Left == Right)
            {
                if (resultTree.reproducesBug())
                {
                    // Let's not change the best tree. Let's just set the critical transition.
                    //BestTree = resultTree;
                    BestTree.setCriticalTransitionStep(Left);
                }
                Completed = true;
                Valid = resultTree.reproducesBug();
            }
            else
            {

                int mid = (Left + Right) / 2;
                if (resultTree.reproducesBug())
                {
                    Right = mid;
                    // Let's not change the best tree. Let's just set the critical transition.
                    //BestTree = resultTree;
                    //BestTree.setCriticalTransitionStep(mid);
                }
                else
                {
                    Left = mid + 1;
                }
            }
        }

        public void PrepareForNextIteration(EventTree resultTree)
        {
            if (!initialized)
            {
                Initialize(resultTree);
            }
            else if (nReplaysRequired > 0) // Check if we require replays
            {
                minTraceReplay.PrepareForNextIteration(resultTree);
                if (minTraceReplay.Completed)
                {
                    // TODO: Do we assume that the replay had no failures if resultTree.reproducesBug()
                    refineBounds(resultTree);
                    minTraceReplay = new MinimalTraceReplayControlUnit(nReplaysRequired, false);
                }
                
            }
            else{
                refineBounds(resultTree);
            }
        }
    }

}
