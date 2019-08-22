// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling
{
    /// <summary>
    /// Util functions for partial-order manipulation
    /// </summary>
    public static class PartialOrderManipulationUtils
    {
        // #region counting

        /// <summary>
        /// Counts how many nodes are reachable from at
        /// </summary>
        /// <param name="at">The root, ideally</param>
        /// <returns>the count of nodes reachable from at ( at included )</returns>
        public static int CountTreeSize(ProgramStep at)
        {
            int cnt = 1;
            if (at.CreatedStep != null)
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
        public static List<ProgramStep> ConsolidatePartialOrderOnOriginalTotalOrderingIndex(ProgramStep root)
        {
            List<ProgramStep> reachableSet = new List<ProgramStep>();
            CollectReachableSet(root, reachableSet);
            List<ProgramStep> sortedSet = reachableSet.OrderBy(x => x.TotalOrderingIndex).ToList();

            return sortedSet;
        }

        /// <summary>
        /// Linearizes a partial order, and then returns a list based on the TotalOrderingIndex assigned by the linearization
        /// </summary>
        /// <param name="root">The root of the partial order</param>
        /// <returns>A List of ProgramStep respecting the partial order</returns>
        public static List<ProgramStep> ConsolidatePartialOrder(ProgramStep root)
        {
            List<ProgramStep> linearizedOrder = LinearizePartialOrder(root);
            for (int i = 0; i < linearizedOrder.Count; i++)
            {
                // linearizedOrder[i].TotalOrderingIndex = i;
            }

            return linearizedOrder;
        }

        /// <summary>
        /// Linearizes the partial order into a total-order respecting the partial order.
        /// </summary>
        /// <returns>A list of the steps respecting the partial ordering</returns>
        private static List<ProgramStep> LinearizePartialOrder(ProgramStep root)
        {
            // Why does C# not have a min-heap?
            SortedSet<ProgramStep> pendingSteps = new SortedSet<ProgramStep>(new ProgramStep.ProgramStepTotalOrderingComparer());
            HashSet<ProgramStep> seenSteps = new HashSet<ProgramStep>();
            List<ProgramStep> totalOrdering = new List<ProgramStep>();

            pendingSteps.Add(root);
            while (pendingSteps.Count > 0)
            {
                ProgramStep at = pendingSteps.Min;
                totalOrdering.Add(at);
                seenSteps.Add(at);
                pendingSteps.Remove(at);

                List<ProgramStep> enabledChildren = GetChildren(at).Where(x => GetParents(x).All(y => NullOrSeen(y, seenSteps))).ToList();
                enabledChildren.ForEach(x => pendingSteps.Add(x));
            }

            return totalOrdering;
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

        private static HashSet<ProgramStep> GetChildren(ProgramStep at)
        {
            HashSet<ProgramStep> children = new HashSet<ProgramStep>();
            children.Add(at.NextMachineStep);
            children.Add(at.CreatedStep);
            children.Add(at.NextInboxOrderingStep);

            if (at.NextMonitorSteps != null)
            {
                at.NextMonitorSteps.Values.Select(x => children.Add(x));
            }

            children.Remove(null);

            return children;
        }

        private static HashSet<ProgramStep> GetParents(ProgramStep at)
        {
            HashSet<ProgramStep> parents = new HashSet<ProgramStep>();
            parents.Add(at.PrevMachineStep);
            parents.Add(at.CreatorParent);
            parents.Add(at.PrevInboxOrderingStep);

            if (at.PrevMonitorSteps != null)
            {
                at.PrevMonitorSteps.Values.Select(x => parents.Add(x));
            }

            parents.Remove(null);

            return parents;
        }

        // #region clone

        /// <summary>
        /// Clones a ProgramModelSummary.
        /// </summary>
        /// <param name="summary">The summary to be cloned</param>
        /// <returns>The cloned summary</returns>
        public static ProgramModelSummary CloneProgramSummary(ProgramModelSummary summary) => CloneProgramSummary(summary, new List<ProgramStep>(), out Dictionary<ProgramStep, ProgramStep> d);

        /// <summary>
        /// Clones a ProgramSummary. Offers user requested step mapping.
        /// </summary>
        /// <param name="summary">The summary to be cloned</param>
        /// <param name="stepsToMap">The extra steps to be mapped</param>
        /// <param name="mappedSteps">Out parameter with the requested steps mapped</param>
        /// <returns>A clone of the program summary </returns>
        public static ProgramModelSummary CloneProgramSummary(ProgramModelSummary summary, List<ProgramStep> stepsToMap, out Dictionary<ProgramStep, ProgramStep> mappedSteps)
        {
            HashSet<ProgramStep> allStepsToMap = new HashSet<ProgramStep>(stepsToMap);
            summary.WithHeldSends.ForEach( s => allStepsToMap.Add(s));
            allStepsToMap.Add(summary.PartialOrderRoot);
            allStepsToMap.Add(summary.BugTriggeringStep);

            ProgramStep newRoot = ClonePartialOrder(summary.PartialOrderRoot, allStepsToMap.ToList(), out Dictionary<ProgramStep, ProgramStep> stepMap, true);
            mappedSteps = stepMap;

            return new ProgramModelSummary(newRoot, mappedSteps[summary.BugTriggeringStep], summary.WithHeldSends.Select(x => stepMap[x]).ToList(), summary.NumSteps, summary.LivenessViolatingMonitorType);
        }

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
                    if (oldToNew.ContainsKey(step))
                    {
                        // TODO: Does this check hide errors?
                        mappedSteps.Add(step, oldToNew[step]);
                    }
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
        /// Walks up the program tree ( and then down the other ) to return the matching step in rootY
        /// </summary>
        /// <param name="rootX">The root of the partial order containing the step to map</param>
        /// <param name="stepX">The step we want to map</param>
        /// <param name="rootY">The root of the partial order to which we want to map</param>
        /// <returns>The step in rootY corresponding to stepX in rootX</returns>
        public static ProgramStep FindMatchingStep(ProgramStep rootX, ProgramStep stepX, ProgramStep rootY)
        {
            Stack<bool> takeMachineThreadEdge = new Stack<bool>();
            return FindMatchingStep(rootX, stepX, rootY, takeMachineThreadEdge);
        }

        private static ProgramStep FindMatchingStep(ProgramStep rootX, ProgramStep stepX, ProgramStep rootY, Stack<bool> takeMachineThreadEdge)
        {
            if (stepX.PrevMachineStep != null)
            {
                takeMachineThreadEdge.Push(true);
                return FindMatchingStep(rootX, stepX.PrevMachineStep, rootY, takeMachineThreadEdge).NextMachineStep;
            }
            else if (stepX.CreatorParent != null)
            {
                takeMachineThreadEdge.Push(false);
                return FindMatchingStep(rootX, stepX.CreatorParent, rootY, takeMachineThreadEdge).CreatedStep;
            }
            else if (rootX == stepX)
            {
                return rootY;
            }
            else
            {
                throw new NotImplementedException("This was implented wrong");
            }
        }

        // #region equivalence_checking
#if WE_DONT_LIKE_EFFICIENCY
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
#endif

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
            while (stepX != null && stepY != null)
            {
                if (stepX.PrevMachineStep != null && stepY.PrevMachineStep != null)
                {
                    stepX = stepX.PrevMachineStep;
                    stepY = stepY.PrevMachineStep;
                }
                else if (stepX.CreatorParent != null && stepY.CreatorParent != null)
                {
                    stepX = stepX.CreatorParent;
                    stepY = stepY.CreatorParent;
                }
                else if ( stepX == rootX && stepY == rootY )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // This should never be hit
            throw new NotImplementedException("This should never be hit");
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

            if (keyStep.OpType == AsyncOperationType.Create)
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
                    x.OpType == AsyncOperationType.Create ||
                    (machineIdMapping.ContainsKey(x.TargetId) && machineIdMapping[x.TargetId] == y.TargetId));
        }

        // #region pruning_slicing

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

                if (root.PrevMonitorSteps != null)
                {
                    foreach (KeyValuePair<Type, ProgramStep> kv in root.PrevMonitorSteps)
                    {
                        kv.Value.NextMonitorSteps[kv.Key] = null;
                    }
                }
            }
            else
            {
                // PrunePartialOrderByTotalOrderingIndex(root.NextInboxOrderingStep, indexLimit);
                PrunePartialOrderByTotalOrderingIndex(root.NextMachineStep, indexLimit);
                PrunePartialOrderByTotalOrderingIndex(root.CreatedStep, indexLimit);
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

        private static bool NullOrSeen(ProgramStep y, HashSet<ProgramStep> seenSteps)
        {
            return y == null || seenSteps.Contains(y);
        }

        // #region serialization

        /// <summary>
        /// Serializes a ProgramSummary into XML
        /// </summary>
        /// <param name="originalSummary">The PrograModelSummary</param>
        /// <param name="livenessTemperatureTreshold">In the case of a liveness bug, what was the LivenessTemperatureTreshold</param>
        /// <returns>The XML representation of the PrograModelSummary</returns>
        public static XDocument SerializeProgramSummaryToXml(ProgramModelSummary originalSummary, int livenessTemperatureTreshold)
        {
            ProgramModelSummary summary = CloneProgramSummary(originalSummary);
            List<ProgramStep> linearization = ConsolidatePartialOrder(summary.PartialOrderRoot);

            XElement metaXml = new XElement("Meta");
            metaXml.Add(new XElement("LivenessTemperatureTreshold", livenessTemperatureTreshold));
            metaXml.Add(new XElement("BugTriggeringStepIndex", summary.BugTriggeringStep.TotalOrderingIndex));
            metaXml.Add(new XElement("IsLivenessBug", summary.IsLivenessBug));
            metaXml.Add(new XElement("WithHeldSendIndices",
                summary.WithHeldSends.Select( x => new XElement("WithHeldSendIndex", x.TotalOrderingIndex)).ToArray()));

            XElement stepsXml = new XElement("Steps", linearization.Select( s => SerializeStep(s)).ToArray());

            return new XDocument(new XElement("ProgramSummary",
                metaXml,
                stepsXml));
        }

        private static XElement SerializeStep(ProgramStep s)
        {
            // Basic info
            List<XElement> elements = new List<XElement>
            {
                new XElement("TotalOrderingIndex", s.TotalOrderingIndex),
                new XElement("ProgramStepType", s.ProgramStepType),
                new XElement("SourceId", s.SrcId),
                new XElement("TargetId", s.TargetId),
            };

            // ProgramStepType specifics
            switch (s.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    elements.Add(new XElement("OpType", s.OpType.ToString()));
                    break;

                case ProgramStepType.NonDetBoolStep:
                    elements.Add(new XElement("NonDetBoolChoice", s.BooleanChoice));
                    break;

                case ProgramStepType.NonDetIntStep:
                    elements.Add(new XElement("NonDetIntChoice", s.IntChoice));
                    break;

                case ProgramStepType.SpecialProgramStepType:
                default:
                    break;
            }

            // Edges

            if (s.NextMachineStep != null)
            {
                elements.Add(new XElement("NextMachineStepIndex", s.NextMachineStep.TotalOrderingIndex));
            }

            if (s.CreatedStep != null)
            {
                elements.Add(new XElement("CreatedStepIndex", s.CreatedStep.TotalOrderingIndex));
            }

            if (s.NextInboxOrderingStep != null)
            {
                elements.Add(new XElement("NextInboxOrderingStepIndex", s.NextInboxOrderingStep.TotalOrderingIndex));
            }

            if (s.NextMonitorSteps != null)
            {
                elements.Add(new XElement("NextMonitorSteps", s.NextMonitorSteps.Select(
                    x => new XElement( "NextMonitorStepIndex", new XAttribute("MonitorType", x.Key), x.Value.TotalOrderingIndex))));
            }

            return new XElement("Step", elements.ToArray());
        }

        /// <summary>
        /// Deserializes a given XElement containing multiple Step elements into a partial order.
        /// </summary>
        /// <param name="stepListXml">The XElement</param>
        /// <param name="assembly">The assembly of the program being described. Used for types</param>
        /// <returns>The root of the partial order</returns>
        public static List<ProgramStep> DeserializeStepsXmlToSchedule(XElement stepListXml, System.Reflection.Assembly assembly)
        {
            List<Tuple<XElement, ProgramStep>> stepList = new List<Tuple<XElement, ProgramStep>>();
            Dictionary<int, ProgramStep> stepMap = new Dictionary<int, ProgramStep>();

            foreach (XElement stepXml in stepListXml.Elements("Step"))
            {
                ProgramStep step = DeserializeStepBasic(stepXml);
                stepList.Add(Tuple.Create(stepXml, step));
                stepMap.Add(step.TotalOrderingIndex, step);
            }

            stepList.ForEach(t => DeserializeStepEdges(t.Item1, t.Item2, stepMap, assembly));
            return stepList.Select(t => t.Item2).OrderBy( x => x.TotalOrderingIndex).ToList();
        }

        private static ProgramStep DeserializeStepBasic(XElement stepXml)
        {
            ProgramStep step = null;
            if ( Enum.TryParse<ProgramStepType>(stepXml.Element("ProgramStepType").Value, out ProgramStepType stepType) &&
                ulong.TryParse(stepXml.Element("SourceId").Value, out ulong srcId) &&
                int.TryParse(stepXml.Element("TotalOrderingIndex").Value, out int totalOrderingIndex))
            {
                switch (stepType)
                {
                    case ProgramStepType.SchedulableStep:
                        if (Enum.TryParse<AsyncOperationType>(stepXml.Element("OpType").Value, out AsyncOperationType opType) &&
                            ulong.TryParse(stepXml.Element("TargetId").Value, out ulong targetId))
                        {
                            step = new ProgramStep(opType, srcId, targetId, null);
                        }

                        break;

                    case ProgramStepType.NonDetBoolStep:
                        if (bool.TryParse(stepXml.Element("NonDetBoolChoice").Value, out bool boolChoice))
                        {
                            step = new ProgramStep(srcId, boolChoice);
                        }

                        break;

                    case ProgramStepType.NonDetIntStep:
                        if (int.TryParse(stepXml.Element("NonDetIntChoice").Value, out int intChoice))
                        {
                            step = new ProgramStep(srcId, intChoice);
                        }

                        break;

                    case ProgramStepType.SpecialProgramStepType:
                        {
                            step = ProgramStep.CreateSpecialProgramStep();
                        }

                        break;
                }

                step.TotalOrderingIndex = totalOrderingIndex;
            }

            return step;
        }

        private static void DeserializeStepEdges(XElement stepXml, ProgramStep step, Dictionary<int, ProgramStep> totalOrderingIndexToStep, System.Reflection.Assembly assembly)
        {
            XElement e = null;
            if ( (e = stepXml.Element("NextMachineStepIndex")) != null)
            {
                step.NextMachineStep = totalOrderingIndexToStep[int.Parse(e.Value)];
                totalOrderingIndexToStep[int.Parse(e.Value)].PrevMachineStep = step;
            }

            if ((e = stepXml.Element("CreatedStepIndex")) != null)
            {
                step.CreatedStep = totalOrderingIndexToStep[int.Parse(e.Value)];
                totalOrderingIndexToStep[int.Parse(e.Value)].CreatorParent = step;
            }

            if ((e = stepXml.Element("NextInboxOrderingStepIndex")) != null)
            {
                step.NextInboxOrderingStep = totalOrderingIndexToStep[int.Parse(e.Value)];
                totalOrderingIndexToStep[int.Parse(e.Value)].PrevInboxOrderingStep = step;
            }

            if ((e = stepXml.Element("NextMonitorSteps")) != null)
            {
                step.NextMonitorSteps = new Dictionary<Type, ProgramStep>();
                foreach (XElement nmsi in e.Elements("NextMonitorStepIndex"))
                {
                    step.NextMonitorSteps.Add(assembly.GetType(nmsi.Attribute("MonitorType").Value), totalOrderingIndexToStep[int.Parse(nmsi.Value)]);
                    if (totalOrderingIndexToStep[int.Parse(nmsi.Value)].PrevMonitorSteps == null)
                    {
                        totalOrderingIndexToStep[int.Parse(nmsi.Value)].PrevMonitorSteps = new Dictionary<Type, ProgramStep>();
                    }

                    totalOrderingIndexToStep[int.Parse(nmsi.Value)].PrevMonitorSteps.Add(assembly.GetType(nmsi.Attribute("MonitorType").Value), step);
                }
            }
        }
    }
}
