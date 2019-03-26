using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    internal class MinimalTraceReplayControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.MinimizedTraceReplay; } }
        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }


        private int nReplays;
        public MinimalTraceReplayControlUnit(int nReplaysRequired)
        {
            BestTree = null;
            nReplays = nReplaysRequired;
            Completed = false;
        }

        

        public bool PrepareForNextIteration(EventTree resultTree)
        {
            if (resultTree.reproducesBug())
            {
                BestTree = resultTree;
            }

            if (resultTree.reproducesBug())
            {
                nReplays--;
                Completed = (nReplays < 0);
            }
            else
            {
                Completed = true;
            }
            Valid = (BestTree != null);
            return Completed && Valid;
        }
    }
}
