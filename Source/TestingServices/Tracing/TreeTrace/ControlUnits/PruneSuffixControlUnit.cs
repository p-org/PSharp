using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    class PruneSuffixControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.PruneSuffix; } }
        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }


        private int nReplays;
        public PruneSuffixControlUnit()
        {
            BestTree = null;
            Completed = false;
            Valid = true;
            nReplays = 1;
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
