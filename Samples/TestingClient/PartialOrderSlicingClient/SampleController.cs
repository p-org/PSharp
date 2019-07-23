using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.ClientInterface;
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
            ReplayingSliced,
            Success
        }

        private ControllerState CurrentState;

        private string[] ScheduleDump;
        private bool ScheduleIsFair;

        private IProgramStep PartialOrderRoot;

        public PartialOrderSliceController(Configuration configuration, string replayTraceFile)
            : base(configuration)
        {
            this.CurrentState = ControllerState.Initial;
            TestingClientUtils.ReadScheduleFileForReplay(replayTraceFile, out this.ScheduleDump, configuration, out this.ScheduleIsFair);
        }

        public override string GetReport()
        {
            return "Ended up in state: " + this.CurrentState;
        }

        private static AbstractBaseProgramModelStrategy HAX_OLDSTRATEGY;
        public override void NotifySchedulingEnded(bool bugFound)
        {
            switch (this.CurrentState)
            {
                case ControllerState.ReplayingTrace:
                    if (bugFound)
                    {
                        HAX_OLDSTRATEGY = (AbstractBaseProgramModelStrategy)this.ActiveStrategy;
                        
                        this.PartialOrderRoot = (this.ActiveStrategy as AbstractBaseProgramModelStrategy).GetRootStep(); // PartialOrderManipulationUtils.ClonePartialOrderFromProgramModelBasedStrategy(this.ActiveStrategy);
                        Console.WriteLine("PO SIZE: " + PartialOrderManipulationUtils.CountTreeSize(this.PartialOrderRoot));
                        this.CurrentState = ControllerState.ReplayingSliced;
                    }
                    else
                    {
                        Microsoft.PSharp.IO.Error.Report("Could not reproduce bug from trace");
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
                        Microsoft.PSharp.IO.Error.Report("Could not reproduce bug from trace");
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

                case ControllerState.ReplayingSliced:
                    nextStrategy = new ProgramGraphReplayStrategy(this.PartialOrderRoot, this.ScheduleIsFair);
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

        public override void StrategyReset()
        {
            this.ActiveStrategy.Reset();
        }
    }
}
