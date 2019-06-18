// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
    internal class MessageFlowBasedDHittingMetricStrategy : BasicProgramModelBasedStrategy
    {
        private readonly int MaxDToCount;
        // private readonly DTupleTreeNode DTupleTreeRoot;
        private int iterationNumber;

        private readonly DTupleTreeNode DTupleTreeRoot;

        private static HashSet<IProgramStepSignature> UniqueSigs = new HashSet<IProgramStepSignature>();
        // private static HashSet<ulong> UniqueSigs = new HashSet<ulong>();

        public MessageFlowBasedDHittingMetricStrategy(ISchedulingStrategy childStrategy, int dToHit)
            : base(childStrategy)
        {
            this.MaxDToCount = dToHit;
            this.iterationNumber = 0;
            this.DTupleTreeRoot = new DTupleTreeNode(DTupleTreeNode.CreateRootNodeSignature());
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            this.ComputeStepSignatures();
            this.EnumerateDTuples();

            // Console.WriteLine(this.GetProgramTrace());
            DTupleTreeNode.PrintDTupleTree(this.DTupleTreeRoot, 0);

            for (int i = 2; i <= this.MaxDToCount; i++)
            {
                Console.WriteLine($"{i}-tuple count={this.GetDTupleCount(i)}");
            }

            Console.WriteLine($"Unique hashes={UniqueSigs.Count}");
        }

        private void ComputeStepSignatures()
        {
            // TODO: This signature sux without senderId.
            Dictionary<Tuple<ulong, string>, int> inboxEventIndexCounter = new Dictionary<Tuple<ulong, string>, int>();
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                // Tuple<ulong, string> ieicKey = new Tuple<ulong, string>(progStep.SrcId, progStep.EventInfo?.EventName ?? "NullEventInfo");
                Tuple<ulong, string> ieicKey = Tuple.Create<ulong, string>(progStep.SrcId, progStep.EventInfo?.EventName ?? "NullEventInfo");

                if (!inboxEventIndexCounter.ContainsKey(ieicKey))
                {
                    inboxEventIndexCounter.Add(ieicKey, 0);
                }

                int currentIndex = inboxEventIndexCounter[ieicKey];
                // progStep.Signature = new EventTypeIndexStepSignature(progStep, currentIndex);
                progStep.Signature = new TreeHashStepSignature(progStep);
                UniqueSigs.Add(progStep.Signature);
                // UniqueSigs.Add((progStep.Signature as TreeHashStepSignature).Hash);
                inboxEventIndexCounter[ieicKey] = currentIndex + 1;
            }
        }

        private void EnumerateDTuples()
        {
            this.iterationNumber++;
            Dictionary<IProgramStepSignature, List<DTupleTreeNode>> stepIndexToSteps = new Dictionary<IProgramStepSignature, List<DTupleTreeNode>>();
            // Pre-insert an entry for each signature
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                stepIndexToSteps[progStep.Signature] = new List<DTupleTreeNode>();
            }

            // Recursively construct tree
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Send)
                {
                    DTupleTreeNode newNode = this.DTupleTreeRoot.AddOrUpdateChild(progStep.Signature, this.iterationNumber);
                    stepIndexToSteps[progStep.Signature].Add(newNode);

                    if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Send)
                    {
                        this.RecursiveAdd(progStep.Signature, progStep.CreatedStep, stepIndexToSteps);
                    }

                    if (progStep.NextMachineStep != null)
                    {
                        this.RecursiveAdd(progStep.Signature, progStep.NextMachineStep, stepIndexToSteps);
                    }
                }
            }
        }

        private void RecursiveAdd(IProgramStepSignature signature, IProgramStep descendent, Dictionary<IProgramStepSignature, List<DTupleTreeNode>> stepIndexToSteps)
        {
            if (descendent.ProgramStepType == ProgramStepType.SchedulableStep && descendent.OpType == TestingServices.Scheduling.AsyncOperationType.Send)
            {
                // TODO: What if this step isn't a send?
                // TODO: stepIndexToSteps can be made more efficient wrt depth bound
                foreach (DTupleTreeNode dtn in stepIndexToSteps[signature])
                {
                    if (dtn.Depth < this.MaxDToCount)
                    {
                        DTupleTreeNode newNode = dtn.AddOrUpdateChild(descendent.Signature, this.iterationNumber);
                        if (newNode.Depth < this.MaxDToCount)
                        {
                            stepIndexToSteps[descendent.Signature].Add(newNode);
                        }
                    }
                }
            }

            if (descendent.ProgramStepType == ProgramStepType.SchedulableStep && descendent.OpType == TestingServices.Scheduling.AsyncOperationType.Send)
            {
                this.RecursiveAdd(signature, descendent.CreatedStep, stepIndexToSteps);
            }

            if (descendent.NextMachineStep != null)
            {
                this.RecursiveAdd(signature, descendent.NextMachineStep, stepIndexToSteps);
            }
        }

        public ulong GetDTupleCount(int d)
        {
            return this.DTupleTreeRoot.GetDTupleCount(d);
        }
    }
}
