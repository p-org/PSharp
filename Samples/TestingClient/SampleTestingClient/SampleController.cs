using Microsoft.PSharp;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingServices.Scheduling.ClientInterface;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleTestingClient
{
    public class SampleController : AbstractStrategyController
    {
        internal ISchedulingStrategy ActiveStrategy;
        private int BugCount;
        private int nBugstoStopAt;
        public SampleController(Configuration configuration, int nBugsToStopAt)
            : base(configuration)
        {
            this.ActiveStrategy = null;
            this.BugCount = 0;

            this.nBugstoStopAt = nBugsToStopAt;
        }

        public override string GetReport()
        {
            return "SampleController found " + this.BugCount + " bugs";
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            if (bugFound)
            {
                this.BugCount++;
            }

            // Does nothing, but you should.
        }

        // public void SetControlUnitStrategy(ControlUnitStrategy controlUnitStrategy)
        // {
        //    this.ControlStrategy = controlUnitStrategy;
        // }

        public override bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, out int maxSteps)
        {
            if (this.ActiveStrategy == null)
            {
                this.ActiveStrategy = new RandomStrategy(this.Configuration.MaxFairSchedulingSteps);
            }
            else
            {
                this.ActiveStrategy.PrepareForNextIteration();
            }

            maxSteps = this.Configuration.MaxFairSchedulingSteps;

            nextStrategy = this.ActiveStrategy;
            return this.BugCount < this.nBugstoStopAt;
        }

        public override void StrategyReset()
        {
            this.ActiveStrategy.Reset();
        }
    }
}
