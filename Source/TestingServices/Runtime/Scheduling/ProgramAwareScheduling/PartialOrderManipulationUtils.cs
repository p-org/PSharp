// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling
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
        /// <param name="copyTotalOrderingIndex">Specifies whether or not to copy TotalOrderingIndex from the source partial order</param>
        /// <returns>The root of the new partial order</returns>
        public static ProgramStep ClonePartialOrder(ProgramStep root, List<ProgramStep> stepsToMap, out Dictionary<ProgramStep, ProgramStep> mappedSteps, bool copyTotalOrderingIndex)
        {
            Dictionary<ProgramStep, ProgramStep> oldToNew = new Dictionary<ProgramStep, ProgramStep>();

            ProgramStep newRoot = ClonePartialOrder(root, oldToNew, copyTotalOrderingIndex);

            // Now copy the edges using the map
            foreach (KeyValuePair<ProgramStep, ProgramStep> stepMapPair in oldToNew)
            {
                ProgramStep oldStep = stepMapPair.Key;
                ProgramStep newStep = stepMapPair.Value;

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

                if (oldStep.NextInboxOrderingStep != null)
                {
                    newStep.NextInboxOrderingStep = oldToNew[oldStep.NextInboxOrderingStep];
                    oldToNew[oldStep.NextInboxOrderingStep].PrevInboxOrderingStep = newStep;
                }

                if (oldStep.NextMonitorSteps != null)
                {
                    newStep.NextMonitorSteps = new Dictionary<Type, ProgramStep>();
                    foreach (KeyValuePair<Type, ProgramStep> kp in oldStep.NextMonitorSteps)
                    {
                        newStep.NextMonitorSteps.Add(kp.Key, oldToNew[kp.Value]);
                        if (oldToNew[kp.Value].PrevMonitorSteps == null)
                        {
                            oldToNew[kp.Value].PrevMonitorSteps = new Dictionary<Type, ProgramStep>();
                        }

                        oldToNew[kp.Value].PrevMonitorSteps.Add(kp.Key, newStep);
                    }
                }
            }

            mappedSteps = new Dictionary<ProgramStep, ProgramStep>();
            if (stepsToMap != null)
            {
                foreach (ProgramStep step in stepsToMap)
                {
                    mappedSteps.Add(step, oldToNew[step]);
                }
            }

            return newRoot;
        }

        private static ProgramStep ClonePartialOrder(ProgramStep step, Dictionary<ProgramStep, ProgramStep> oldToNewStep, bool copyTotalOrderingIndex)
        {
            if (oldToNewStep.TryGetValue(step, out ProgramStep newStep))
            {
                return newStep;
            }

            newStep = step.Clone(copyTotalOrderingIndex);
            oldToNewStep.Add(step, newStep);

            if (step.NextMachineStep != null)
            {
                ClonePartialOrder(step.NextMachineStep, oldToNewStep, copyTotalOrderingIndex);
            }

            if (step.CreatedStep != null)
            {
                ClonePartialOrder(step.CreatedStep, oldToNewStep, copyTotalOrderingIndex);
            }

            return newStep;
        }

        /// <summary>
        /// Maps the step of the partial order rooted at keyRoot to those rooted at valueRoot.
        /// </summary>
        /// <param name="keyRoot">The root of the partial order, whose steps which will be the keys of the mapping</param>
        /// <param name="valueRoot">The root of the partial order, whose steps which will be the values of the mapping</param>
        /// <param name="mapping">The mapping</param>
        /// <returns>The number of steps successfully mapped</returns>
        public static int MapPartialOrders(ProgramStep keyRoot, ProgramStep valueRoot, out Dictionary<ProgramStep, ProgramStep> mapping)
        {
            mapping = new Dictionary<ProgramStep, ProgramStep>();
            Dictionary<ulong, ulong> machineIdMapping = new Dictionary<ulong, ulong>();
            machineIdMapping.Add(0, 0);
            MapPartialOrdersImpl(keyRoot, valueRoot, machineIdMapping, mapping);
            return mapping.Count;
        }

        private static void MapPartialOrdersImpl(ProgramStep keyStep, ProgramStep valueStep, Dictionary<ulong, ulong> machineIdMapping, Dictionary<ProgramStep, ProgramStep> stepMapping)
        {
            stepMapping[keyStep] = valueStep;

            if (keyStep.OpType == TestingServices.Scheduling.AsyncOperationType.Create)
            {
                machineIdMapping.Add(keyStep.TargetId, valueStep.TargetId);
            }

            if (CheckEquivalence(keyStep.CreatedStep, valueStep.CreatedStep, machineIdMapping))
            {
                MapPartialOrdersImpl(keyStep.CreatedStep, valueStep.CreatedStep, machineIdMapping, stepMapping);
            }

            // else if (keyStep.CreatedStep != null || valueStep.CreatedStep != null)
            // {
            // Console.WriteLine("Put a breakpoint here");
            // }

            if (CheckEquivalence(keyStep.NextMachineStep, valueStep.NextMachineStep, machineIdMapping))
            {
                MapPartialOrdersImpl(keyStep.NextMachineStep, valueStep.NextMachineStep, machineIdMapping, stepMapping);
            }

            // else if (keyStep.NextMachineStep != null || valueStep.NextMachineStep != null)
            // {
            //    Console.WriteLine("Put a breakpoint here");
            // }
        }

        private static bool CheckEquivalence(ProgramStep x, ProgramStep y, Dictionary<ulong, ulong> machineIdMapping)
        {
            return x != null && y != null && x.OpType == y.OpType &&
                machineIdMapping.ContainsKey(x.SrcId) && machineIdMapping[x.SrcId] == y.SrcId && (
                    x.OpType == TestingServices.Scheduling.AsyncOperationType.Create ||
                    (machineIdMapping.ContainsKey(x.TargetId) && machineIdMapping[x.TargetId] == y.TargetId));
        }

        /// <summary>
        /// Prunes the partial order by removing any edges from
        /// a node with TotalOrderingIndex &lt; indexLimit to a node with TotalOrderingIndex &gt; indexLimit
        /// </summary>
        /// <param name="root">The root of the partial order</param>
        /// <param name="indexLimit">The limit</param>
        public static void PrunePartialOrderByTotalOrderingIndex(ProgramStep root, int indexLimit)
        {
            if (root == null)
            {
                return;
            }

            if (root.TotalOrderingIndex > indexLimit)
            {
                // No need to recurse
                if (root.PrevInboxOrderingStep != null)
                {
                    root.PrevInboxOrderingStep.NextInboxOrderingStep = null;
                }

                if (root.PrevMachineStep != null)
                {
                    root.PrevMachineStep.NextMachineStep = null;
                }

                if (root.CreatorParent != null)
                {
                    root.CreatorParent.CreatedStep = null;
                }
            }
            else
            {
                PrunePartialOrderByTotalOrderingIndex(root.NextInboxOrderingStep, indexLimit);
                PrunePartialOrderByTotalOrderingIndex(root.NextMachineStep, indexLimit);
                PrunePartialOrderByTotalOrderingIndex(root.CreatedStep, indexLimit);
            }
        }

        /// <summary>
        /// Counts how many nodes are reachable from at
        /// </summary>
        /// <param name="at">The root, ideally</param>
        /// <returns>the count of nodes reachable from at ( at included )</returns>
        public static int CountTreeSize(ProgramStep at)
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
        /// Returns an ordered list of ProgramSteps ordered by TotalOrderingIndex.
        /// </summary>
        /// <param name="root">The root of the Partial Order</param>
        /// <returns>An ordered list of ProgramSteps ordered by TotalOrderingIndex.</returns>
        public static List<ProgramStep> ConsolidatePartialOrder(ProgramStep root)
        {
            List<ProgramStep> reachableSet = new List<ProgramStep>();
            CollectReachableSet(root, reachableSet);
            List<ProgramStep> sortedSet = reachableSet.OrderBy(x => x.TotalOrderingIndex).ToList();

            return sortedSet;
        }

        /// <summary>
        /// Adds any node reachable from root to the reachableSet.
        /// </summary>
        /// <param name="root">The root of the partial order</param>
        /// <param name="reachableSet">The list to which reachable steps must be added</param>
        public static void CollectReachableSet(ProgramStep root, List<ProgramStep> reachableSet)
        {
            if (root != null)
            {
                reachableSet.Add(root);
                CollectReachableSet(root.NextMachineStep, reachableSet);
                CollectReachableSet(root.CreatedStep, reachableSet);
            }
        }

        /// <summary>
        /// Returns a set of nodes in the subtree
        /// </summary>
        /// <param name="root">The root of the subtree</param>
        /// <returns>A hashset containing all nodes in the subtree</returns>
        public static HashSet<ProgramStep> GetStepsInSubtree(ProgramStep root)
        {
            HashSet<ProgramStep> stepsInSubtree = new HashSet<ProgramStep>();
            GetStepsInSubtree(root, stepsInSubtree);
            return stepsInSubtree;
        }

        private static void GetStepsInSubtree(ProgramStep at, HashSet<ProgramStep> stepsInSubtree)
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
        public static void SliceStep(ProgramStep step)
        {
            HashSet<ProgramStep> stepsInSubtree = GetStepsInSubtree(step);
            SliceStep(step, stepsInSubtree);
        }

        private static void SliceStep(ProgramStep step, HashSet<ProgramStep> stepsInSubtree)
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

            if (step.PrevInboxOrderingStep != null && !stepsInSubtree.Contains(step.PrevInboxOrderingStep))
            {
                ProgramStep firstSendOutsideSubtree = RecurseEnqueueTillOutOfSubtree(step, stepsInSubtree);
                step.PrevInboxOrderingStep.NextInboxOrderingStep = firstSendOutsideSubtree;
                if (firstSendOutsideSubtree != null)
                {
                    firstSendOutsideSubtree.PrevInboxOrderingStep = step.PrevInboxOrderingStep;
                }
            }

            if (step.PrevMonitorSteps != null)
            {
                foreach (KeyValuePair<Type, ProgramStep> kp in step.PrevMonitorSteps)
                {
                    if (!stepsInSubtree.Contains(kp.Value))
                    {
                        ProgramStep firstMonitorOutsideSubtree = RecurseMonitorTillOutOfSubtree(step, kp.Key, stepsInSubtree);
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

        private static ProgramStep RecurseEnqueueTillOutOfSubtree(ProgramStep step, HashSet<ProgramStep> stepsInSubtree)
        {
            if (step.NextInboxOrderingStep == null)
            {
                return null;
            }
            else if (stepsInSubtree.Contains(step.NextInboxOrderingStep))
            {
                return RecurseEnqueueTillOutOfSubtree(step.NextInboxOrderingStep, stepsInSubtree);
            }
            else
            {
                return step.NextInboxOrderingStep;
            }
        }

        private static ProgramStep RecurseMonitorTillOutOfSubtree(ProgramStep step, Type monitorType, HashSet<ProgramStep> stepsInSubtree)
        {
            if (step.NextMonitorSteps == null || !step.NextMonitorSteps.ContainsKey(monitorType))
            {
                return null;
            }
            else if (stepsInSubtree.Contains(step.NextInboxOrderingStep))
            {
                return RecurseMonitorTillOutOfSubtree(step.NextMonitorSteps[monitorType], monitorType, stepsInSubtree);
            }
            else
            {
                return step.NextMonitorSteps[monitorType];
            }
        }

        /// <summary>
        /// Checks if two steps (stepX and stepY) of two partial orders ( rooted at rootX and rootY, resp. ) match.
        /// </summary>
        /// <param name="rootX">The root of the first partial order</param>
        /// <param name="stepX">The step in the first partial order</param>
        /// <param name="rootY">The root of the second partial order</param>
        /// <param name="stepY">The step in the second partial order</param>
        /// <returns>True if the steps match</returns>
        public static bool StepsMatch(ProgramStep rootX, ProgramStep stepX, ProgramStep rootY, ProgramStep stepY)
        {
            MapPartialOrders(rootX, rootY, out Dictionary<ProgramStep, ProgramStep> stepMapping);
            return stepMapping.ContainsKey(stepX) && stepMapping[stepX] == stepY;
        }
    }
}
