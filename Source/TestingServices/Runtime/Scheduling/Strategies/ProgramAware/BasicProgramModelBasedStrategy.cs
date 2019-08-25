// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware
{
    /// <summary>
    /// Counts d-hitting tuples in the execution
    /// </summary>
    public class BasicProgramModelBasedStrategy : AbstractBaseProgramModelStrategy
    {
        private readonly ISchedulingStrategy ChildStrategy;

        /// <inheritdoc/>
        protected override bool HashEvents => this.ShouldHashEvents;

        /// <inheritdoc/>
        protected override bool HashMachines => this.ShouldHashMachines;

        private readonly bool ShouldHashEvents;
        private readonly bool ShouldHashMachines;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicProgramModelBasedStrategy"/> class.
        /// </summary>
        /// <param name="childStrategy">The actual scheduling strategy</param>
        /// <param name="shouldHashEvents">True if the eventHash must be calculated</param>
        /// <param name="shouldHashMachines">True if machineHash must be recorded for each step</param>
        public BasicProgramModelBasedStrategy(ISchedulingStrategy childStrategy, bool shouldHashEvents = false, bool shouldHashMachines = false)
            : base()
        {
            this.ChildStrategy = childStrategy;
            this.ShouldHashEvents = shouldHashEvents;
            this.ShouldHashMachines = shouldHashMachines;
        }

        /// <inheritdoc/>
        public override string GetDescription()
        {
            return "Wrapper strategy which constructs a model of the program while it runs according to the specified child-strategy:" + this.ChildStrategy.GetType().Name;
        }

        /// <inheritdoc/>
        public override bool IsFair()
        {
            return this.ChildStrategy.IsFair();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            this.ChildStrategy.Reset();
        }

        /// <inheritdoc/>
        public override bool PrepareForNextIteration()
        {
            base.PrepareForNextIteration();
            return this.ChildStrategy.PrepareForNextIteration();
        }

        /// <inheritdoc/>
        public override int GetScheduledSteps()
        {
            return this.ChildStrategy.GetScheduledSteps();
        }

        /// <inheritdoc/>
        public override bool HasReachedMaxSchedulingSteps()
        {
            return this.ChildStrategy.HasReachedMaxSchedulingSteps();
        }

        // Scheduling Choice(?)s

        /// <inheritdoc/>
        public override void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
            => this.ChildStrategy.ForceNext(next, ops, current);

        /// <inheritdoc/>
        public override void ForceNextBooleanChoice(int maxValue, bool next)
            => this.ChildStrategy.ForceNextBooleanChoice(maxValue, next);

        /// <inheritdoc/>
        public override void ForceNextIntegerChoice(int maxValue, int next)
            => this.ChildStrategy.ForceNextIntegerChoice(maxValue, next);

        /// <inheritdoc/>
        public override bool GetNextOperation(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            // TODO
            return this.ChildStrategy.GetNext(out next, ops, current);
        }

        /// <inheritdoc/>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            return this.ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <inheritdoc/>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            return this.ChildStrategy.GetNextIntegerChoice(maxValue, out next);
        }
    }
}
