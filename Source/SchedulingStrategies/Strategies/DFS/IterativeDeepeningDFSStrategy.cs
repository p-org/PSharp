// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// A depth-first search scheduling strategy that uses iterative deepening.
    /// </summary>
    public sealed class IterativeDeepeningDFSStrategy : DFSStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// The max depth.
        /// </summary>
        private int MaxDepth;

        /// <summary>
        /// The current depth.
        /// </summary>
        private int CurrentDepth;

        /// <summary>
        /// Creates a DFS strategy that uses iterative deepening.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="logger">ILogger</param>
        public IterativeDeepeningDFSStrategy(int maxSteps, ILogger logger)
            : base(maxSteps, logger)
        {
            MaxDepth = maxSteps;
            CurrentDepth = 1;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public override bool PrepareForNextIteration()
        {
            bool doNext = PrepareForNextIteration();
            if (!doNext)
            {
                Reset();
                CurrentDepth++;
                if (CurrentDepth <= MaxDepth)
                {
                    Logger.WriteLine($"<IterativeDeepeningDFSLog> Depth bound increased to {CurrentDepth} (max is {MaxDepth}).");
                    doNext = true;
                }
            }

            return doNext;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public new bool HasReachedMaxSchedulingSteps()
        {
            return ScheduledSteps == CurrentDepth;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public new string GetDescription()
        {
            return $"DFS with iterative deepening (max depth is {MaxDepth})";
        }
    }
}
