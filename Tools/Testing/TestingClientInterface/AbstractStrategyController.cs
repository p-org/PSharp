// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.ClientInterface;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingClientInterface
{
    public abstract class AbstractStrategyController : IStrategyController
    {
        protected internal ISchedulingStrategy ActiveStrategy;
        protected internal readonly Configuration Configuration;

        public AbstractStrategyController(Configuration config)
        {
            this.Configuration = config;
            this.ActiveStrategy = null;

            this.UpdateConfiguration();
            this.ValidateConfiguration();
        }

        public abstract void Initialize(out ISchedulingStrategy strategy);

        public virtual void Initialize(Configuration configuration, out ISchedulingStrategy nextStrategy)
        {
            // Ignore the configuraiton
            this.Initialize(out nextStrategy);
            this.ActiveStrategy = nextStrategy;
        }

        public abstract string GetReport();

        public abstract void NotifySchedulingEnded(bool bugFound);

        public abstract void StrategyReset();

        public bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, Configuration configurationForNextIter)
        {
            // Noooope. Let's not give a Configuration object to the programmer.
            if (this.PrepareStrategyAndConfigurationForNextIteration(out nextStrategy, configurationForNextIter))
            {
                this.ActiveStrategy = nextStrategy;
                return true;
            }
            else
            {
                this.ActiveStrategy = null;
                return false;
            }
        }

        public abstract bool PrepareStrategyAndConfigurationForNextIteration(out ISchedulingStrategy nextStrategy, Configuration configuration);

        // Configuration manipulation

        internal void UpdateConfiguration()
        {
            if (!this.Configuration.UserExplicitlySetMaxFairSchedulingSteps)
            {
                this.Configuration.MaxFairSchedulingSteps = this.Configuration.MaxUnfairSchedulingSteps * 10;
            }

            if (this.Configuration.LivenessTemperatureThreshold == 0)
            {
                if (this.Configuration.EnableCycleDetection)
                {
                    this.Configuration.LivenessTemperatureThreshold = 100;
                }
                else if (this.Configuration.MaxFairSchedulingSteps > 0)
                {
                    this.Configuration.LivenessTemperatureThreshold =
                        this.Configuration.MaxFairSchedulingSteps / 2;
                }
            }

            if (this.Configuration.RandomSchedulingSeed is null)
            {
                this.Configuration.RandomSchedulingSeed = DateTime.Now.Millisecond;
            }
        }

        internal void ValidateConfiguration()
        {
            if (this.Configuration.SchedulingStrategy != SchedulingStrategy.ControlUnit)
            {
                Error.ReportAndExit("Invalid Configuration: SchedulingStrategy has to be ControlUnit");
            }

            if (this.Configuration.MaxFairSchedulingSteps < this.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("Invalid Configuration:  MaxFairSchedulingSteps < MaxUnfairSchedulingSteps");
            }
        }

        public static Configuration CreateDefaultConfiguration()
        {
            Configuration config = Configuration.Create();
            config.SchedulingStrategy = SchedulingStrategy.ControlUnit;
            return config;
        }

        public virtual bool ShouldEnqueueEvent(MachineId senderId, MachineId targetId, Event evt)
        {
            return true;
        }
    }
}
