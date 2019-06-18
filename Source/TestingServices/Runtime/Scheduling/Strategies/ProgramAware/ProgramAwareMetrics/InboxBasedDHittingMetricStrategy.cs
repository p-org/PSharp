// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
     internal class InboxBasedDHittingMetricStrategy : BasicProgramModelBasedStrategy
    {
        private readonly int MaxDToCount;
        // private readonly DTupleTreeNode DTupleTreeRoot;
        private int iterationNumber;
        private readonly DTupleTreeNode DTupleTreeRoot;

        public InboxBasedDHittingMetricStrategy(ISchedulingStrategy childStrategy, int dToHit)
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
            // PrintDTupleTree(this.DTupleTreeRoot, 0);

            for (int i = 2; i <= this.MaxDToCount; i++)
            {
                Console.WriteLine( $"{i}-tuple count={this.GetDTupleCount(i)}");
            }
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
            Dictionary<Tuple<ulong, string>, int> inboxEventIndexCounter = new Dictionary<Tuple<ulong, string>, int>();
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                // Tuple<ulong, string> ieicKey = new Tuple<ulong, string>(progStep.SrcId, progStep.EventInfo?.EventName ?? "NullEventInfo");
                Tuple<ulong, string> ieicKey = Tuple.Create<ulong, string>(progStep.SrcId, progStep.EventInfo?.EventName ?? "NullEventInfo");

                if (!inboxEventIndexCounter.ContainsKey(ieicKey) )
                {
                    inboxEventIndexCounter.Add(ieicKey, 0);
                }

                int currentIndex = inboxEventIndexCounter[ieicKey];
                progStep.Signature = new EventTypeIndexStepSignature(progStep, currentIndex);
                inboxEventIndexCounter[ieicKey] = currentIndex + 1;
            }
        }

        private void EnumerateDTuples()
        {
            this.iterationNumber++;
            Dictionary<ulong, List<DTupleTreeNode>> machineIdToSteps = new Dictionary<ulong, List<DTupleTreeNode>>();
            foreach ( IProgramStep progStep in this.ProgramModel.OrderedSteps )
            {
                if ( progStep.ProgramStepType == ProgramStepType.SchedulableStep &&
                    progStep.OpType == TestingServices.Scheduling.AsyncOperationType.Receive )
                {
                    if (!machineIdToSteps.ContainsKey(progStep.SrcId) )
                    {
                        machineIdToSteps.Add(progStep.SrcId, new List<DTupleTreeNode>());
                        machineIdToSteps[progStep.SrcId].Add(this.DTupleTreeRoot);
                    }

                    // Add to the root
                    List<DTupleTreeNode> newAdditions = new List<DTupleTreeNode>();
                    foreach (DTupleTreeNode dtn in machineIdToSteps[progStep.SrcId])
                    {
                        if (dtn.Depth < this.MaxDToCount)
                        {
                            DTupleTreeNode newNode = dtn.AddOrUpdateChild(progStep.Signature, this.iterationNumber);
                            newAdditions.Add(newNode);
                        }
                    }

                    machineIdToSteps[progStep.SrcId].AddRange(newAdditions);
                }
            }
        }

        public ulong GetDTupleCount(int d)
        {
            return this.DTupleTreeRoot.GetDTupleCount(d);
        }
    }
}
