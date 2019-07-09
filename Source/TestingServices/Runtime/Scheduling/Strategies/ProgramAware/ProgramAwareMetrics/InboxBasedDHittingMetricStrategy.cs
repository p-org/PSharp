// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

// #define USE_DETUPLETREENODE

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Microsoft.PSharp.TestingServices.Statistics;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
     internal class InboxBasedDHittingMetricStrategy : BasicProgramModelBasedStrategy
    {
        private readonly int MaxDToCount;
        private readonly WrapperStrategyConfiguration.DHittingSignature StepSignatureType;
        private readonly Dictionary<ulong, Type> SrcIdToMachineType;

        private int iterationNumber;
        private readonly DTupleTreeNode DTupleTreeRoot;
        public DTupleTree DTupleTree;

        private readonly HashSet<ulong> UniqueSigs;

        private readonly TimelyStatisticLogger<ulong[]> StatLogger;

        public InboxBasedDHittingMetricStrategy(ISchedulingStrategy childStrategy, WrapperStrategyConfiguration wrapperStrategyConfiguration)
            : base(childStrategy)
        {
            this.MaxDToCount = wrapperStrategyConfiguration.MaxDTuplesDepth;
            this.StepSignatureType = wrapperStrategyConfiguration.SignatureType;
            this.SrcIdToMachineType = new Dictionary<ulong, Type>();

            this.iterationNumber = 0;
            this.DTupleTreeRoot = new DTupleTreeNode(DTupleTreeNode.CreateRootNodeSignature());
            this.DTupleTree = new DTupleTree(this.MaxDToCount);

            this.UniqueSigs = new HashSet<ulong>();
            this.StatLogger = new TimelyStatisticLogger<ulong[]>();

            this.ResetLocalVariables();
        }

        public override void RecordCreateMachine(Machine createdMachine, Machine creatorMachine)
        {
            base.RecordCreateMachine(createdMachine, creatorMachine);
            this.SrcIdToMachineType[createdMachine.Id.Value] = createdMachine.GetType();
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            this.ComputeStepSignatures();
            this.EnumerateDTuples();

            ulong[] iterStats = new ulong[this.MaxDToCount];
            for (int i = 1; i <= this.MaxDToCount; i++)
            {
                iterStats[i - 1] = this.GetDTupleCount(i);
            }

            this.StatLogger.AddValue(iterStats);

            // reset
            this.ResetLocalVariables();

            // string s = this.ProgramModel.HAXGetProgramTreeString();
            // string dt = DTupleTreeNode.HAXGetDTupleTreeString(this.DTupleTreeRoot);
            // string dtr = this.DTupleTreeRoot.GetReport(this.MaxDToCount);
            // Console.WriteLine(this.GetProgramTrace());
            // PrintDTupleTree(this.DTupleTreeRoot, 0);
        }

        private void ResetLocalVariables()
        {
            this.SrcIdToMachineType.Clear();
            this.SrcIdToMachineType[0] = typeof(TestHarnessMachine);
        }

        private static void PrintDTupleTree(DTupleTreeNode dtr, int depth)
        {
            Console.WriteLine( new string('\t', depth) + $"::{dtr.StepSig}");
            foreach (KeyValuePair<IProgramStepSignature, DTupleTreeNode> kvp in dtr.Children)
            {
                PrintDTupleTree(kvp.Value, depth + 1);
            }
        }

        private void ComputeStepSignatures()
        {
            if (this.StepSignatureType == WrapperStrategyConfiguration.DHittingSignature.TreeHash)
            {
                this.ComputeTreeHashStepSignatures();
            }
            else if ( this.StepSignatureType == WrapperStrategyConfiguration.DHittingSignature.EventTypeIndex)
            {
                this.ComputeEventTypeIndexStepSignatures();
            }
        }

        private void EnumerateDTuples()
        {
#if USE_DETUPLETREENODE
            this.EnumerateDTuplesDTupleTreeNode();
#else
            this.EnumerateDTuplesDTupleTree();
#endif
        }

        private void EnumerateDTuplesDTupleTree()
        {
            HashSet<ulong> doneMachines = new HashSet<ulong>();

            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep &&
                    progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Send)
                {
                    if (doneMachines.Contains(progStep.TargetId))
                    {
                        continue;
                    }

                    List<int> nodesToAddTo = new List<int> { DTupleTree.RootIdx };
                    this.GenerateDTuples(progStep, nodesToAddTo);
                    doneMachines.Add(progStep.TargetId);
                }
            }
        }

        private void GenerateDTuples(IProgramStep currentStep, List<int> nodesToAddTo)
        {
            List<int> newAdditions = new List<int>();
            foreach ( int tn in nodesToAddTo)
            {
                int childIdx = this.DTupleTree.AddOrUpdateChild(tn, currentStep.Signature);
                if ( DTupleTree.GetNodeDepth(childIdx) < this.MaxDToCount)
                {
                    newAdditions.Add(childIdx);
                }
            }

            nodesToAddTo.AddRange(newAdditions);
            if ( currentStep.NextEnqueuedStep != null)
            {
                this.GenerateDTuples(currentStep.NextEnqueuedStep, nodesToAddTo);
            }
        }

        private void EnumerateDTuplesDTupleTreeNode()
        {
            this.iterationNumber++;
            Dictionary<ulong, List<DTupleTreeNode>> machineIdToSteps = new Dictionary<ulong, List<DTupleTreeNode>>();
            foreach ( IProgramStep progStep in this.ProgramModel.OrderedSteps )
            {
                if ( progStep.ProgramStepType == ProgramStepType.SchedulableStep &&
                    progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Send )
                {
                    if (!machineIdToSteps.ContainsKey(progStep.TargetId) )
                    {
                        machineIdToSteps.Add(progStep.TargetId, new List<DTupleTreeNode>());
                        machineIdToSteps[progStep.TargetId].Add(this.DTupleTreeRoot);
                    }

                    // Add to the root
                    List<DTupleTreeNode> newAdditions = new List<DTupleTreeNode>();
                    foreach (DTupleTreeNode dtn in machineIdToSteps[progStep.TargetId])
                    {
                        if (dtn.Depth < this.MaxDToCount)
                        {
                            DTupleTreeNode newNode = dtn.AddOrUpdateChild(progStep.Signature, this.iterationNumber);
                            newAdditions.Add(newNode);
                        }
                    }

                    machineIdToSteps[progStep.TargetId].AddRange(newAdditions);
                }
            }
        }

        public ulong GetDTupleCount(int d)
        {
#if USE_DETUPLETREENODE
            return this.DTupleTreeRoot.GetDTupleCount(d);
#else
            return this.DTupleTree.GetDTupleCount(d);
#endif
        }

        // Specific StepSignature implementations
        private void ComputeTreeHashStepSignatures()
        {
            Dictionary<ulong, ulong> machineIdRemap = new Dictionary<ulong, ulong>();
            machineIdRemap[TESTHARNESSMACHINEID] = TESTHARNESSMACHINEHASH;
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                progStep.Signature = new TreeHashStepSignature(progStep, machineIdRemap);
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Create)
                {
                    machineIdRemap[progStep.TargetId] = (progStep.Signature as TreeHashStepSignature).Hash;
                }

                this.UniqueSigs.Add((progStep.Signature as TreeHashStepSignature).Hash);
            }
        }

        // Note : This does Send events.
        private void ComputeEventTypeIndexStepSignatures()
        {
            Dictionary<Tuple<ulong, string>, int> inboxEventIndexCounter = new Dictionary<Tuple<ulong, string>, int>();

            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Send)
                {
                    // Tuple<ulong, string> ieicKey = new Tuple<ulong, string>(progStep.SrcId, progStep.EventInfo?.EventName ?? "NullEventInfo");
                    Tuple<ulong, string> ieicKey = Tuple.Create<ulong, string>(progStep.TargetId, progStep.EventInfo?.EventName ?? "NullEventInfo");

                    if (!inboxEventIndexCounter.ContainsKey(ieicKey))
                    {
                        inboxEventIndexCounter.Add(ieicKey, 0);
                    }

                    int currentIndex = inboxEventIndexCounter[ieicKey];
                    progStep.Signature = new EventTypeIndexStepSignature(progStep, this.SrcIdToMachineType[progStep.TargetId].GetType(), currentIndex);

                    inboxEventIndexCounter[ieicKey] = currentIndex + 1;
                }
                else
                {
                    progStep.Signature = new EventTypeIndexStepSignature(progStep, this.SrcIdToMachineType[progStep.SrcId].GetType(), -1);
                }
            }
        }

        public override string GetReport()
        {
            // return this.DTupleTreeRoot.GetReport(this.MaxDToCount);

            StringBuilder sb = new StringBuilder();
            foreach (Tuple<int, ulong[]> stat in this.StatLogger)
            {
                string s = string.Join("\t", stat.Item2);
                sb.AppendLine($"{stat.Item1}\t:\t{s}");
            }

            Tuple<int, ulong[]> finalStat = this.StatLogger.GetFinalValue();
            sb.AppendLine("-\t:\t-\t-\t-");
            string s2 = string.Join("\t", finalStat.Item2);
            sb.AppendLine($"{finalStat.Item1}\t:\t{s2}");

            return sb.ToString();
        }
    }
}
