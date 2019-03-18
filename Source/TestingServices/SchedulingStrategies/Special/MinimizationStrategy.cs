
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a replaying scheduling strategy.
    /// </summary>
    internal sealed class MinimizationStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        private ScheduleTrace originalScheduleTrace;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private ISchedulingStrategy SuffixStrategy;
        
        /// <summary>
        /// Is the scheduler that produced the
        /// schedule trace fair?
        /// </summary>
        private bool IsSchedulerFair;

        /// <summary>
        /// Is the scheduler replaying the trace?
        /// </summary>
        private bool IsReplaying;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Text describing a replay error.
        /// </summary>
        internal string ErrorText { get; private set; }



        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="trace">ScheduleTrace</param>
        /// <param name="minimizationGuide"></param>
        /// <param name="isFair">Is scheduler fair</param>
        public MinimizationStrategy(Configuration configuration, ScheduleTrace trace, IMinimizationGuide minimizationGuide, bool isFair)
            : this(configuration, trace, minimizationGuide, isFair, null)
        { }
        /* /// <param name="externalEventTypes">Specifies a list of "external" events which are to be pruned first</param> */
        

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="trace">ScheduleTrace</param>
        /// <param name="minimizationGuide"></param>
        /// <param name="isFair">Is scheduler fair</param>
        /// <param name="suffixStrategy">The suffix strategy.</param>
        public MinimizationStrategy(Configuration configuration, ScheduleTrace trace, IMinimizationGuide minimizationGuide, bool isFair, ISchedulingStrategy suffixStrategy)
        {
            Configuration = configuration;
            originalScheduleTrace = trace;
            ScheduledSteps = 0;
            IsSchedulerFair = isFair;
            IsReplaying = true;
            SuffixStrategy = suffixStrategy;
            ErrorText = string.Empty;

            MinimizationGuide = minimizationGuide;

            replayStrategy = new ReplayStrategy(configuration, trace, isFair, SuffixStrategy);
            enterReplayMode(trace);
            traceEditor = new TreeTraceEditor(null);

            //isFirstStep = true;
        }

        //private bool isFirstStep;

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            if (IsReplaying)
            {
                return GetNextReplayMode(out next, choices, current);
            }
            else
            {
                return GetNextEditMode(out next, choices, current);
                
            }
        }

        private bool GetNextReplayMode(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            if (ConstructTree)
            {
                traceEditor.recordSchedulingChoiceResult(current, choices.ToDictionary(x => x.Id), (ulong)GetScheduledSteps());
            }

            if (replayStrategy.GetNext(out next, choices, current)){

                if (ConstructTree)
                {
                    traceEditor.recordSchedulingChoiceStart(next, (ulong)GetScheduledSteps());
                }
                return true;
            }else{
                return false;
            }
        }

        public bool GetNextEditMode(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            if (ConstructTree)
            {
                traceEditor.recordSchedulingChoiceResult(current, choices.ToDictionary(x => x.Id), (ulong)GetScheduledSteps());
            }

            bool result = traceEditor.GetNext(out next, choices, current);
            if (result)
            {
                ScheduledSteps++;
                if (ConstructTree)
                {
                    traceEditor.recordSchedulingChoiceStart(next, (ulong)GetScheduledSteps());
                }
            }
            else if(!result && traceEditor.reachedEnd() && SuffixStrategy!=null)
            {
                // Stop logging past this point
                ConstructTree = false;
                result = SuffixStrategy.GetNext(out next, choices, current);
            }
            
            return result;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (IsReplaying)
            {
                return GetNextBooleanChoiceReplayMode(maxValue, out next);
            }
            else
            {
                return GetNextBooleanChoiceEditMode(maxValue, out next);
            }
        }

        public bool GetNextBooleanChoiceReplayMode(int maxValue, out bool next) {
            if (replayStrategy.GetNextBooleanChoice(maxValue, out next))
            {
                if (ConstructTree)
                {
                    traceEditor.RecordBooleanChoice(next);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetNextBooleanChoiceEditMode(int maxValue, out bool next)
        {
            bool result = traceEditor.GetNextBooleanChoice(maxValue, out next);
            if (result)
            {
                ScheduledSteps++;
                if (ConstructTree)
                {
                    traceEditor.RecordBooleanChoice(next);
                }
            }
            else if (!result && traceEditor.reachedEnd() && SuffixStrategy != null)
            {
                // Stop logging past this point
                ConstructTree = false;
                result = SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
            }
            return result;
        }

        internal EventTree getBestTree()
        {
            return traceEditor.getGuideTree();
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (IsReplaying)
            {
                return GetNextIntegerChoiceReplayMode(maxValue, out next);
            }
            else
            {
                return GetNextIntegerChoiceEditMode(maxValue, out next);
            }
        }

        private bool GetNextIntegerChoiceReplayMode(int maxValue, out int next)
        {
            if(replayStrategy.GetNextIntegerChoice(maxValue, out next)){
                if (ConstructTree)
                {
                    traceEditor.RecordIntegerChoice(next);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetNextIntegerChoiceEditMode(int maxValue, out int next) {

            bool result = traceEditor.GetNextIntegerChoice(maxValue, out next);
            if (result)
            {
                ScheduledSteps++;
                if (ConstructTree)
                {
                    traceEditor.RecordIntegerChoice(next);
                }
            }
            else if (!result && traceEditor.reachedEnd() && SuffixStrategy != null)
            {
                // Stop logging past this point
                ConstructTree = false;
                result = SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
            }
            return result;

        }

        /// <summary>
        /// Forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            replayStrategy.PrepareForNextIteration();
            ScheduledSteps = 0;
            bool result = !traceEditor.ranOutOfEvents;
            if (SuffixStrategy != null)
            {
                result = result && SuffixStrategy.PrepareForNextIteration();
            }

            // TODO:
            return result;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ScheduledSteps = 0;
            replayStrategy.Reset();
            SuffixStrategy?.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            if (IsReplaying) // is replaying
            {
                return replayStrategy.GetScheduledSteps();
            }else
            {
                return ScheduledSteps + (SuffixStrategy?.GetScheduledSteps()??0); // I'm not even sure what this is
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return GetScheduledSteps() > originalScheduleTrace.Count;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            // TODO: This.
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.IsFair();
            }
            else
            {
                return false; // Can't guarantee the edited trace is fair
            }
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            if (SuffixStrategy != null)
            {
                return "Replay(" + SuffixStrategy.GetDescription() + ")";
            }
            else
            {
                return "Replay";
            }
        }

        #endregion


        #region Minimization Stuff



        // Minimization stuff
        // TODO: Binary search Delta Debugging. Till then -> linear search.

        // Edit mode stuff
        private bool WasAbandoned;
        private bool ConstructTree;
        // Deletion
        private IMinimizationGuide MinimizationGuide;

        // Replay mode
        ReplayStrategy replayStrategy;
        private const int replaysRequired = 1; // How many before we conclude we hit the bug?
        private int replaysRemaining; // 

        // Exchange between the two modes:

        //private 
        internal TreeTraceEditor traceEditor;

        private void enterEditMode(bool bugReproduced)
        {
            IsReplaying = false;
            traceEditor.prepareForNextIteration(bugReproduced);
            WasAbandoned = false;
            ConstructTree = true;
        }

        private void enterReplayMode(ScheduleTrace trace)
        {
            IsReplaying = true;
            //traceToReplay = trace; // Doesn't work. Make new trace
            replayStrategy = new ReplayStrategy(Configuration, trace, IsSchedulerFair);
            replaysRemaining = replaysRequired;

            ConstructTree = true;
        }



        public void recordResult(bool bugFound, ScheduleTrace scheduleTrace)
        {
            traceEditor.activeProgramModel.getTree().setActualTrace(scheduleTrace);
            if(WasAbandoned)
            {   // The edit went catastrophically wrong. Re-enter edit mode
                enterEditMode(false);
            }
            else if (IsReplaying)
            {   //  If we didn't get a bug, This can't be deleted.
                if (!bugFound)
                {
                    enterEditMode(false);
                }
                else
                {
                    replaysRemaining--;
                    // We were constructing. Now we've constructed. Keep it.
                    ConstructTree = false;
                    if (replaysRemaining == 0)
                    {
                        enterEditMode(true);
                    }
                }
            }
            else
            {   // We were editing. If we did hit the bug, Just replay to confirm. Else try the next index
                if (!bugFound)
                {
                    enterEditMode(false);
                }
                else
                {
                    enterEditMode(true);
                    //enterReplayMode(scheduleTrace);// Let's actually enter edit mode 
                    
                }
            }
        }

        internal bool ShouldDeliverEvent(BaseMachine sender, Event e, Machine receiver)
        {
            return traceEditor?.ShouldDeliverEvent(e) ?? true ;
        }

        #endregion
    }
}
