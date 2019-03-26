using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    internal class CriticalTransitionSearchControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.CriticalTransitionSearch; } }
        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }

        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }


        internal CriticalTransitionSearchControlUnit(EventTree guideTree)
        {

            if (guideTree.reproducesBug())
            {
                BestTree = guideTree;
                Left = 0;
                Right = guideTree.totalOrdering.Count - 1;
                Valid = true;
                Completed = false;
            }
            else
            {
                Valid = false;
                Completed = true;
                throw new ArgumentException("Cannot run critical transition search on non-buggy schedule");
            }

        }


        public bool PrepareForNextIteration(EventTree resultTree)
        {
            if ( Left == Right )
            {
                if (resultTree.reproducesBug())
                {
                    BestTree = resultTree;
                }
                //else {Valid = false;} // This is not the critical transition
                
                Completed = true;
            }
            else {

                int mid = (Left + Right) / 2;
                if (resultTree.reproducesBug())
                {
                    Right = mid;
                    BestTree = resultTree;
                }
                else
                {
                    Left = mid + 1;
                }
            }
            Valid = (BestTree!=null);

            return Completed && Valid;
        }
    }

}
