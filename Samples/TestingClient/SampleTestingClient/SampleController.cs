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

        public override void Initialize(out ISchedulingStrategy strategy)
        {
            strategy = new RandomStrategy(this.Configuration.MaxFairSchedulingSteps);
        }

        public override bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, out int maxSteps)
        {
            this.ActiveStrategy.PrepareForNextIteration();

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
