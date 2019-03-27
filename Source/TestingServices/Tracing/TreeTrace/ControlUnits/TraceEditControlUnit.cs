using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    class TraceEditControlUnit : ITraceEditorControlUnit
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


        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.TraceEdit; } }

        public EventTree BestTree { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }

        public int ReplayLength { get { return BestTree.totalOrdering.Count;  } }
        public bool strictBugEquivalenceChecking { get { return true; } }


        //internal Stack< Tuple<int, int> > bSearchBounds;
        internal Stack<int> bSearchRightBounds;
        private bool isInited;

        public TraceEditControlUnit() {
            Completed = false;
            Valid = true;
            bSearchRightBounds = new Stack<int>();
        }
        private void Initialize(EventTree guideTree)
        {
            isInited = true;
            if (guideTree.reproducesBug())
            {
                Valid = true;
                Completed = false;
                BestTree = guideTree;
                Left = 0;
                Right = guideTree.totalOrdering.Count - 1;
            }
            else
            {
                Completed = true;
                Valid = false;
                throw new ArgumentException("Can't do trace edit if guideTree doesn't reproduce bug");
            }

        }

        public void PrepareForNextIteration(EventTree resultTree)
        {
            if (!isInited)
            {
                Initialize(resultTree);
            }
            else if (resultTree.reproducesBug())
            {
                // We can do this since Left and Right is applied on the trace being constructed.
                Left = Right + 1;
                if (Left >= resultTree.totalOrdering.Count)
                {
                    Completed = true;
                }
                else
                {   // Reasonably, We've covered everything to the left of Right. Now we need to set our Left

                    while (Right < Left && bSearchRightBounds.Count > 0)
                    {
                        Right = bSearchRightBounds.Pop();
                    }
                    if (Right < Left)
                    {   // The array may have grown (!!!) since. Not great for minimization, but this should work.
                        Right = resultTree.totalOrdering.Count - 1;
                    }
                }
                BestTree = resultTree; // TODO:? Move this up and rename resultTree usages to BestTree?
            }
            else
            {   // No bug 
                if (BestTree == null)
                {
                    Valid = false;
                }
                else if ( Left == Right )
                {   // Can't recurse
                    Left = Right + 1;
                    if (Left >= BestTree.totalOrdering.Count)    // Use BestTree because it is our guide
                    {   // We are done
                        Completed = true;
                    }
                    else
                    {
                        while (Right < Left && bSearchRightBounds.Count > 0)
                        {
                            Right = bSearchRightBounds.Pop();
                        }

                        if (Right < Left)
                        {   // Stack is empty
                            Right = resultTree.totalOrdering.Count-1;
                        }

                    }
                }
                else
                {   // recurse
                    bSearchRightBounds.Push(Right); // Store next step
                    int mid = (Left+Right)/ 2;      
                    Right = mid;                    // Divide
                }
            }
            
        }

    }
}
