using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    internal class PrefixReplayControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.PrefixReplay; } }
        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }

        public int ReplayLength { get; }

        public bool strictBugEquivalenceChecking { get; }

        private int nReplays;
        public PrefixReplayControlUnit(int nReplaysRequired, int replayLength, bool strictBugEquivalenceChecking)
        {
            nReplays = nReplaysRequired;
            Completed = false;
            Valid = true;

            this.ReplayLength = replayLength;
            this.strictBugEquivalenceChecking = strictBugEquivalenceChecking;
        }



        public void PrepareForNextIteration(EventTree resultTree)
        {
            if (resultTree.reproducesBug())
            {
                BestTree = resultTree;
                nReplays--;
                Completed = (nReplays < 0);
            }
            else
            {
                Completed = true;
                Valid = false;
            }
        }
    }
}
