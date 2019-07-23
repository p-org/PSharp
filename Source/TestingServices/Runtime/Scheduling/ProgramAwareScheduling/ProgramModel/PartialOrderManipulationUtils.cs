// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    /// <summary>
    /// Util functions for partial-order manipulation
    /// </summary>
    public static class PartialOrderManipulationUtils
    {
        /// <summary>
        /// Makes a copy of the partial order so we can manipualte it happily
        /// </summary>
        /// <param name="root">The root of the partial order</param>
        /// <returns>The root of the new partial order</returns>
        public static IProgramStep ClonePartialOrder(IProgramStep root)
        {
            Dictionary<IProgramStep, IProgramStep> oldToNew = new Dictionary<IProgramStep, IProgramStep>();
            int cnt = CountTreeSize(root);
            IProgramStep newRoot = ClonePartialOrder(root, oldToNew);

            // Now copy the edges using the map
            foreach (KeyValuePair<IProgramStep, IProgramStep> stepMapPair in oldToNew)
            {
                IProgramStep oldStep = stepMapPair.Key;
                IProgramStep newStep = stepMapPair.Value;

                if (oldStep.NextMachineStep != null)
                {
                    newStep.NextMachineStep = oldToNew[oldStep.NextMachineStep];
                    oldToNew[oldStep.NextMachineStep].PrevMachineStep = newStep;
                }

                if (oldStep.CreatedStep != null)
                {
                    newStep.CreatedStep = oldToNew[oldStep.CreatedStep];
                    oldToNew[oldStep.CreatedStep].CreatorParent = newStep;
                }

                if (oldStep.NextEnqueuedStep != null)
                {
                    newStep.NextEnqueuedStep = oldToNew[oldStep.NextEnqueuedStep];
                    oldToNew[oldStep.NextEnqueuedStep].PrevEnqueuedStep = newStep;
                }

                if (oldStep.NextMonitorSteps != null)
                {
                    newStep.NextMonitorSteps = new Dictionary<Type, IProgramStep>();
                    foreach (KeyValuePair<Type, IProgramStep> kp in oldStep.NextMonitorSteps)
                    {
                        newStep.NextMonitorSteps.Add(kp.Key, oldToNew[kp.Value]);
                        if (oldToNew[kp.Value].PrevMonitorSteps == null)
                        {
                            oldToNew[kp.Value].PrevMonitorSteps = new Dictionary<Type, IProgramStep>();
                        }

                        oldToNew[kp.Value].PrevMonitorSteps.Add(kp.Key, newStep);
                    }
                }
            }

            return newRoot;
        }

        /// <summary>
        /// Returns a clone of the partial order recorded by the ProgramModel based strategy.
        /// </summary>
        /// <param name="strategy">The strategy from which to get the Partial Order</param>
        /// <returns>A clone of the partial order as returned by <see cref="ClonePartialOrder(IProgramStep)"/></returns>
        public static IProgramStep ClonePartialOrderFromProgramModelBasedStrategy(ISchedulingStrategy strategy)
        {
            if (strategy is BasicProgramModelBasedStrategy)
            {
                return ClonePartialOrder((strategy as AbstractBaseProgramModelStrategy).GetRootStep());
            }
            else
            {
                throw new ArgumentException("Passed strategy was not ProgamModel based");
            }
        }

        private static IProgramStep ClonePartialOrder(IProgramStep step, Dictionary<IProgramStep, IProgramStep> oldToNewStep)
        {
            if (oldToNewStep.TryGetValue(step, out IProgramStep newStep))
            {
                return newStep;
            }

            newStep = step.Clone();
            oldToNewStep.Add(step, newStep);

            if (step.NextMachineStep != null)
            {
                ClonePartialOrder(step.NextMachineStep, oldToNewStep);
            }

            if (step.CreatedStep != null)
            {
                ClonePartialOrder(step.CreatedStep, oldToNewStep);
            }

            return newStep;
        }

        /// <summary>
        /// Counts how many nodes are reachable from at
        /// </summary>
        /// <param name="at">The root, ideally</param>
        /// <returns>the count of nodes reachable from at ( at included )</returns>
        public static int CountTreeSize(IProgramStep at)
        {
            int cnt = 1;
            if ( at.CreatedStep != null)
            {
                cnt += CountTreeSize(at.CreatedStep);
            }

            if (at.NextMachineStep != null && at.NextMachineStep != at.CreatedStep)
            {
                cnt += CountTreeSize(at.NextMachineStep);
            }

            return cnt;
        }

        /// <summary>
        /// Returns a set of nodes in the subtree
        /// </summary>
        /// <param name="root">The root of the subtree</param>
        /// <returns>A hashset containing all nodes in the subtree</returns>
        public static HashSet<IProgramStep> GetStepsInSubtree(IProgramStep root)
        {
            HashSet<IProgramStep> stepsInSubtree = new HashSet<IProgramStep>();
            GetStepsInSubtree(root, stepsInSubtree);
            return stepsInSubtree;
        }

        private static void GetStepsInSubtree(IProgramStep at, HashSet<IProgramStep> stepsInSubtree)
        {
            stepsInSubtree.Add(at);
            if (at.CreatedStep != null)
            {
                GetStepsInSubtree(at.CreatedStep, stepsInSubtree);
            }

            if (at.NextMachineStep != null)
            {
                GetStepsInSubtree(at.NextMachineStep, stepsInSubtree);
            }
        }
    }
}
