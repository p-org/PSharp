// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware
{
    /// <summary>
    /// Counts d-hitting tuples in the execution
    /// </summary>
    internal /*abstract*/ class BasicProgramModelBasedStrategy : AbstractBaseProgramModelStrategy
    {
        private readonly ISchedulingStrategy ChildStrategy;

        protected override bool HashEvents => false;

        internal BasicProgramModelBasedStrategy(ISchedulingStrategy childStrategy)
        {
            this.ChildStrategy = childStrategy;
            this.ProgramModel = new ProgramModel();
        }

        public override string GetDescription()
        {
            return "Wrapper strategy which constructs a model of the program while it runs according to the specified child-strategy:" + this.ChildStrategy.GetType().Name;
        }

        public override bool IsFair()
        {
            return this.ChildStrategy.IsFair();
        }

        public override void Reset()
        {
            base.Reset();
            this.ChildStrategy.Reset();
        }

        public override bool PrepareForNextIteration()
        {
            base.PrepareForNextIteration();
            return this.ChildStrategy.PrepareForNextIteration();
        }

        public override int GetScheduledSteps()
        {
            return this.ChildStrategy.GetScheduledSteps();
        }

        public override bool HasReachedMaxSchedulingSteps()
        {
            return this.ChildStrategy.HasReachedMaxSchedulingSteps();
        }

        // Scheduling Choice(?)s
        public override void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
            => this.ChildStrategy.ForceNext(next, ops, current);

        public override void ForceNextBooleanChoice(int maxValue, bool next)
            => this.ChildStrategy.ForceNextBooleanChoice(maxValue, next);

        public override void ForceNextIntegerChoice(int maxValue, int next)
            => this.ChildStrategy.ForceNextIntegerChoice(maxValue, next);

        public override bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            // TODO
            return this.ChildStrategy.GetNext(out next, ops, current);
        }

        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            return this.ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            return this.ChildStrategy.GetNextIntegerChoice(maxValue, out next);
        }
    }
}
