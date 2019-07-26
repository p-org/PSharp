// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.ClientInterface;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Sometimes ( Specifically Trace minimization ), we want to do some sequence of very different things.
    /// In that case, It's useful to have a wrapper strategy which coordinates.
    /// This strategy is meant to do just that.
    /// </summary>
    internal class ControlUnitStrategy : IProgramAwareSchedulingStrategy
    {
        private IStrategyController Controller;
        private ISchedulingStrategy CurrentStrategy;
        private readonly Configuration Configuration;

        private static readonly Dictionary<ITestingEngine, ControlUnitStrategy> EngineToControlStrategyMap = new Dictionary<ITestingEngine, ControlUnitStrategy>();

        /// <summary>
        /// AbstractTestingEngine.Strategy is protected. So I can't set the controller through such an instance.
        /// Get around this using this static Initialization method which must be called before we run the engine.
        /// </summary>
        /// <param name="testingEngine">The testing engine which will create an instance of ControlUnitStrategy</param>
        /// <param name="controller">The controller the engine should use</param>
        public static void InitializeInstanceThroughEngine(ITestingEngine testingEngine, IStrategyController controller)
        {
            lock (EngineToControlStrategyMap)
            {
                EngineToControlStrategyMap[testingEngine].Initialize(controller);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlUnitStrategy"/> class.
        /// </summary>
        public ControlUnitStrategy(ITestingEngine creatorEngine, Configuration configuration)
        {
            lock (EngineToControlStrategyMap)
            {
                EngineToControlStrategyMap.Add(creatorEngine, this);
            }

            this.Configuration = configuration;
        }

        /// <summary>
        /// Sets the Controller to be used when the is eventually createds
        /// </summary>
        /// <param name="controller">The controller to be used for the next Instance spawned</param>
        internal void Initialize(IStrategyController controller)
        {
            this.Controller = controller;
            this.Controller.Initialize(this.Configuration, out this.CurrentStrategy);
        }

        /// <inheritdoc/>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current) => this.CurrentStrategy.ForceNext(next, ops, current);

        /// <inheritdoc/>
        public void ForceNextBooleanChoice(int maxValue, bool next) => this.CurrentStrategy.ForceNextBooleanChoice(maxValue, next);

        /// <inheritdoc/>
        public void ForceNextIntegerChoice(int maxValue, int next) => this.CurrentStrategy.ForceNextIntegerChoice(maxValue, next);

        /// <inheritdoc/>
        public string GetDescription()
        {
            return "Wrapper strategy which asks a IStrategyController for strategy to use";
        }

        /// <inheritdoc/>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current) => this.CurrentStrategy.GetNext(out next, ops, current);

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(int maxValue, out bool next) => this.CurrentStrategy.GetNextBooleanChoice(maxValue, out next);

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(int maxValue, out int next) => this.CurrentStrategy.GetNextIntegerChoice(maxValue, out next);

        /// <inheritdoc/>
        public int GetScheduledSteps() => this.CurrentStrategy.GetScheduledSteps();

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps() => this.CurrentStrategy.HasReachedMaxSchedulingSteps();

        /// <inheritdoc/>
        public bool IsFair() => this.CurrentStrategy.IsFair();

        /// <inheritdoc/>
        public bool PrepareForNextIteration()
        {
            // Let Controller call strategy.PrepareForNextIteration if it must
            return this.Controller.StrategyPrepareForNextIteration(out this.CurrentStrategy, this.Configuration);
        }

        /// <inheritdoc/>
        public void Reset()
        {
            this.Controller.StrategyReset();
        }

        // Program aware bits.

        public void RecordCreateMachine(Machine createdMachine, Machine creatorMachine) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordCreateMachine(createdMachine, creatorMachine);

        public void RecordStartMachine(Machine machine, Event initialEvent) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordStartMachine(machine, initialEvent);

        public void RecordReceiveEvent(Machine machine, Event evt) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordReceiveEvent(machine, evt);

        public void RecordSendEvent(AsyncMachine sender, MachineId targetMachineId, Event e) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordSendEvent(sender, targetMachineId, e);

        public void RecordMonitorEvent(Type monitorType, AsyncMachine sender, Event e) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordMonitorEvent(monitorType, sender, e);

        public void RecordNonDetBooleanChoice(bool boolChoice) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordNonDetBooleanChoice(boolChoice);

        public void RecordNonDetIntegerChoice(int intChoice) => (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.RecordNonDetIntegerChoice(intChoice);

        public void NotifySchedulingEnded(bool bugFound)
        {
            (this.CurrentStrategy as IProgramAwareSchedulingStrategy)?.NotifySchedulingEnded(bugFound);
            this.Controller.NotifySchedulingEnded(bugFound);
        }
    }
}
