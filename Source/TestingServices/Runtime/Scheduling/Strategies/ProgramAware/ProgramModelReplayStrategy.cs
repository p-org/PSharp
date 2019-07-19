// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware
{
    /// <summary>
    /// Accepts a partial-order ( represented by a single root-node and the closure of nodes reachable through edges ).
    /// (Attempts to ) replay the schedule represented by it, subject to inconsistencies between the partial-order and actual program behaviour.
    /// </summary>
    public class ProgramGraphReplayStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The root of the partial order
        /// </summary>
        public IProgramStep RootStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramGraphReplayStrategy"/> class.
        /// </summary>
        /// <param name="rootStep">The first (root) step of the program. All steps must be reachable from this to be replayed.</param>
        public ProgramGraphReplayStrategy(IProgramStep rootStep)
        {
            this.RootStep = rootStep;
        }

        /// <inheritdoc/>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new System.NotImplementedException("You cannot force this replay strategy");
        }

        /// <inheritdoc/>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new System.NotImplementedException("You cannot force this replay strategy.");
        }

        /// <inheritdoc/>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new System.NotImplementedException("You cannot force this replay strategy");
        }

        /// <inheritdoc/>
        public string GetDescription()
        {
            return "Replays the partial order represented by the program model";
        }

        /// <inheritdoc/>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public int GetScheduledSteps()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool IsFair()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public bool PrepareForNextIteration()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
