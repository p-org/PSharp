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
        /// <param name="stepsToMap">The steps for which the mapping is to be returned</param>
        /// <param name="mappedSteps">Returns a dictionary of oldStep to newStep</param>
        /// <returns>The root of the new partial order</returns>
        public static IProgramStep ClonePartialOrder(IProgramStep root, List<IProgramStep> stepsToMap, out Dictionary<IProgramStep, IProgramStep> mappedSteps)
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

            mappedSteps = new Dictionary<IProgramStep, IProgramStep>();
            if (stepsToMap != null)
            {
                foreach (IProgramStep step in stepsToMap)
                {
                    mappedSteps.Add(step, oldToNew[step]);
                }
            }

            return newRoot;
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

        /// <summary>
        /// Slices out the subtree rooted at step from the partial order rooted at root.
        /// The subtree is defined as the set of nodes reachable through CreatedStep and NextMachineStep
        /// </summary>
        /// <param name="step">The root of the subtree to be removed</param>
        public static void SliceStep(IProgramStep step)
        {
            HashSet<IProgramStep> stepsInSubtree = GetStepsInSubtree(step);
            SliceStep(step, stepsInSubtree);
        }

        private static void SliceStep(IProgramStep step, HashSet<IProgramStep> stepsInSubtree)
        {
            if (step.PrevMachineStep != null && !stepsInSubtree.Contains(step.PrevMachineStep))
            {
                // Connect it to the next one?
                step.PrevMachineStep.NextMachineStep = null;
            }

            if (step.CreatorParent != null && !stepsInSubtree.Contains(step.CreatorParent))
            {
                step.CreatorParent.CreatedStep = null;
            }

            if (step.PrevEnqueuedStep != null && !stepsInSubtree.Contains(step.PrevEnqueuedStep))
            {
                IProgramStep firstSendOutsideSubtree = RecurseEnqueueTillOutOfSubtree(step, stepsInSubtree);
                step.PrevEnqueuedStep.NextEnqueuedStep = firstSendOutsideSubtree;
                if (firstSendOutsideSubtree != null)
                {
                    firstSendOutsideSubtree.PrevEnqueuedStep = step.PrevEnqueuedStep;
                }
            }

            if (step.PrevMonitorSteps != null)
            {
                foreach (KeyValuePair<Type, IProgramStep> kp in step.PrevMonitorSteps)
                {
                    if (!stepsInSubtree.Contains(kp.Value))
                    {
                        IProgramStep firstMonitorOutsideSubtree = RecurseMonitorTillOutOfSubtree(step, kp.Key, stepsInSubtree);
                        kp.Value.NextMonitorSteps[kp.Key] = firstMonitorOutsideSubtree;
                        if (firstMonitorOutsideSubtree != null)
                        {
                            firstMonitorOutsideSubtree.PrevMonitorSteps[kp.Key] = kp.Value;
                        }
                    }
                }
            }
#if nottrue
            throw new NotImplementedException("This needs to be tested");
#endif
            // Now recurse, and fix other links too.
            if (step.CreatedStep != null)
            {
                SliceStep(step.CreatedStep, stepsInSubtree);
            }

            if (step.NextMachineStep != null)
            {
                SliceStep(step.NextMachineStep, stepsInSubtree);
            }
        }

        private static IProgramStep RecurseEnqueueTillOutOfSubtree(IProgramStep step, HashSet<IProgramStep> stepsInSubtree)
        {
            if (step.NextEnqueuedStep == null)
            {
                return null;
            }
            else if (stepsInSubtree.Contains(step.NextEnqueuedStep))
            {
                return RecurseEnqueueTillOutOfSubtree(step.NextEnqueuedStep, stepsInSubtree);
            }
            else
            {
                return step.NextEnqueuedStep;
            }
        }

        private static IProgramStep RecurseMonitorTillOutOfSubtree(IProgramStep step, Type monitorType, HashSet<IProgramStep> stepsInSubtree)
        {
            if (step.NextMonitorSteps == null || !step.NextMonitorSteps.ContainsKey(monitorType))
            {
                return null;
            }
            else if (stepsInSubtree.Contains(step.NextEnqueuedStep))
            {
                return RecurseMonitorTillOutOfSubtree(step.NextMonitorSteps[monitorType], monitorType, stepsInSubtree);
            }
            else
            {
                return step.NextMonitorSteps[monitorType];
            }
        }
    }
}
