using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartialOrderSlicingClient
{
    public class PartialOrderSliceController : AbstractStrategyController
    {
        private enum ControllerState
        {
            Failed,

            Initial,
            ReplayingTrace,
            ReplayingPartialOrder,
            ReplayingSliced,

            Success
        }

        private ControllerState CurrentState;

        private string[] ScheduleDump;
        private bool ScheduleIsFair;
        private IProgramStep OriginalReplayPartialOrder;
        private IProgramStep PartialOrderRoot;
        private IProgramStep BugTriggeringStep;

        public PartialOrderSliceController(Configuration configuration, string replayTraceFile)
            : base(configuration)
        {
            this.CurrentState = ControllerState.Initial;
            TestingClientUtils.ReadScheduleFileForReplay(replayTraceFile, out this.ScheduleDump, configuration, out this.ScheduleIsFair);
        }

        public override void Initialize(out ISchedulingStrategy strategy)
        {
            this.StrategyPrepareForNextIteration(out strategy, out int maxStepsIgnored);
        }

        public override string GetReport()
        {
            return "Ended up in state: " + this.CurrentState;
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            switch (this.CurrentState)
            {
                case ControllerState.ReplayingTrace:
                    if (bugFound)
                    {
                        this.OriginalReplayPartialOrder = (this.ActiveStrategy as AbstractBaseProgramModelStrategy).GetRootStep();
                        IProgramStep bugTriggeringStep = (this.ActiveStrategy as AbstractBaseProgramModelStrategy).GetBugTriggeringStep();

                        this.PartialOrderRoot = PartialOrderManipulationUtils.ClonePartialOrder(this.OriginalReplayPartialOrder,
                            new List<IProgramStep> { bugTriggeringStep },
                            out Dictionary<IProgramStep, IProgramStep> stepMap);
                        this.BugTriggeringStep = stepMap[bugTriggeringStep];

                        this.CurrentState = ControllerState.ReplayingPartialOrder;
                    }
                    else
                    {
                        Microsoft.PSharp.IO.Error.Report("Could not reproduce bug from trace");
                        this.CurrentState = ControllerState.Failed;
                    }
                    break;

                case ControllerState.ReplayingPartialOrder:
                    if (bugFound)
                    {
                        this.CurrentState = ControllerState.ReplayingSliced;
                    }
                    else
                    {
                        Microsoft.PSharp.IO.Error.Report("Could not reproduce bug from partial order");
                        this.CurrentState = ControllerState.Failed;
                    }
                    break;

                case ControllerState.ReplayingSliced:
                    if (bugFound)
                    {
                        this.CurrentState = ControllerState.Success;
                    }
                    else
                    {
                        Microsoft.PSharp.IO.Error.Report("Could not reproduce bug from partial order");
                        this.CurrentState = ControllerState.Failed;
                    }
                    break;

                default:
                    Microsoft.PSharp.IO.Error.Report("Controller reached unexpected state");
                    this.CurrentState = ControllerState.Failed;
                    break;
            }
        }

        // public void SetControlUnitStrategy(ControlUnitStrategy controlUnitStrategy)
        // {
        //    this.ControlStrategy = controlUnitStrategy;
        // }

        public override bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, out int maxSteps)
        {
            maxSteps = this.Configuration.MaxFairSchedulingSteps;
            nextStrategy = null;

            switch (this.CurrentState)
            {
                case ControllerState.Initial:
                case ControllerState.ReplayingTrace:
                    this.CurrentState = ControllerState.ReplayingTrace;
                    nextStrategy = TestingClientUtils.CreateBasicProgramModelBasedStrategy(TestingClientUtils.CreateReplayStrategy(this.Configuration, this.ScheduleIsFair, this.ScheduleDump));
                    break;

                case ControllerState.ReplayingPartialOrder:
                    nextStrategy = new ProgramGraphReplayStrategy(this.PartialOrderRoot, this.ScheduleIsFair);
                    break;

                case ControllerState.ReplayingSliced:
                    IProgramStep SlicedPartialOrderRoot = SlicePartialOrder(this.PartialOrderRoot, this.BugTriggeringStep);
                    nextStrategy = new ProgramGraphReplayStrategy(SlicedPartialOrderRoot, this.ScheduleIsFair);
                    break;

                case ControllerState.Failed:
                case ControllerState.Success:
                default:
                    break;

            }
            maxSteps = this.Configuration.MaxFairSchedulingSteps;

            if (this.CurrentState == ControllerState.Failed || this.CurrentState == ControllerState.Success)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static IProgramStep SlicePartialOrder(IProgramStep partialOrderRoot, IProgramStep bugTriggeringStep)
        {
            // TODO: The slicing
            IProgramStep newRoot = PartialOrderManipulationUtils.ClonePartialOrder(partialOrderRoot, new List<IProgramStep> { bugTriggeringStep }, out Dictionary<IProgramStep, IProgramStep> stepsMap);
            IProgramStep newBugTriggeringStep = stepsMap[bugTriggeringStep];

            List<IProgramStep> slicableSteps = FindNonSourceSteps(newRoot, newBugTriggeringStep);

            foreach (IProgramStep slicableStep in slicableSteps)
            {
                PartialOrderManipulationUtils.SliceStep(slicableStep);
                // PartialOrderManipulationUtils.SliceSubtree(slicableStep);
            }

            return newRoot;
        }

        public override void StrategyReset()
        {
            this.ActiveStrategy.Reset();
        }

        private static List<IProgramStep> FindNonSourceSteps(IProgramStep at, IProgramStep sink)
        {
            List<IProgramStep> slicableSteps = new List<IProgramStep>();
            FindNonSourceStepsImpl(at, sink, slicableSteps, new Dictionary<IProgramStep, bool>());
            return slicableSteps;
        }

        // Returns true if reachable
        private static bool FindNonSourceStepsImpl(IProgramStep at, IProgramStep sink, /*out*/ List<IProgramStep> slicableSteps, Dictionary<IProgramStep, bool> cache)
        {
            // base case
            if (at == sink)
            {
                return true;
            }

            if (cache.ContainsKey(at))
            {
                return cache[at];
            }

            HashSet<IProgramStep> nonReaching = new HashSet<IProgramStep>();
            HashSet<IProgramStep> children = new HashSet<IProgramStep> { at.NextMachineStep, at.CreatedStep };
            if (at.NextMonitorSteps != null)
            {
                at.NextMonitorSteps.Select(s => children.Add(s.Value));
            }

            if (at.NextMachineStep == null)
            {
                children.Add(GetNextHandlerStart(at));
            }

            children.Remove(null);

            foreach (IProgramStep child in children)
            {
                if (!FindNonSourceStepsImpl(child, sink, slicableSteps, cache)){
                    nonReaching.Add(child);
                }
            }


            bool anythingReaches = children.Count > nonReaching.Count;
            if (anythingReaches && nonReaching.Count > 0)
            {
                // Everything in non-reaching are the highest points which can be sliced.
                slicableSteps.AddRange(nonReaching);
            }

            cache.Add(at, anythingReaches);
            return anythingReaches;
        }

        private static IProgramStep GetNextHandlerStart(IProgramStep step)
        {
            while (step.PrevMachineStep != null)
            {
                step = step.PrevMachineStep;
            }

            // Avoid those nasty SpecialProgramStep pit-falls
            if (step.ProgramStepType == ProgramStepType.SchedulableStep)
            {
                return step.CreatorParent.NextEnqueuedStep?.CreatedStep ?? null;
            }
            else
            {
                return null;
            }
        }
    }
}
