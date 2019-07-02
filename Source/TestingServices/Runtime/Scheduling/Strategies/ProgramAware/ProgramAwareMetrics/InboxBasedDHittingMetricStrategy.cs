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
using Microsoft.PSharp.TestingServices.Statistics;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
     internal class InboxBasedDHittingMetricStrategy : BasicProgramModelBasedStrategy
    {
        private readonly int MaxDToCount;
        private readonly Type StepSignatureType;

        private int iterationNumber;
        private readonly DTupleTreeNode DTupleTreeRoot;

        private readonly HashSet<ulong> UniqueSigs;

        private readonly TimelyStatisticLogger<Tuple<ulong, ulong, ulong>> StatLogger;

        public InboxBasedDHittingMetricStrategy(ISchedulingStrategy childStrategy, int dToHit, Type stepSignatureType)
            : base(childStrategy)
        {
            this.MaxDToCount = dToHit;
            this.StepSignatureType = stepSignatureType;

            this.iterationNumber = 0;
            this.DTupleTreeRoot = new DTupleTreeNode(DTupleTreeNode.CreateRootNodeSignature());

            this.UniqueSigs = new HashSet<ulong>();
            this.StatLogger = new TimelyStatisticLogger<Tuple<ulong, ulong, ulong>>();
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            this.ComputeStepSignatures();
            this.EnumerateDTuples();

            this.StatLogger.AddValue(new Tuple<ulong, ulong, ulong>(
                this.GetDTupleCount(1), this.GetDTupleCount(2), this.GetDTupleCount(3)));
            // string s = this.ProgramModel.HAXGetProgramTreeString();
            // string dt = DTupleTreeNode.HAXGetDTupleTreeString(this.DTupleTreeRoot);
            // string dtr = this.DTupleTreeRoot.GetReport(this.MaxDToCount);
            // Console.WriteLine(this.GetProgramTrace());
            // PrintDTupleTree(this.DTupleTreeRoot, 0);
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
            if (this.StepSignatureType == typeof(TreeHashStepSignature))
            {
                this.ComputeTreeHashStepSignatures();
            }
            else if ( this.StepSignatureType == typeof(EventTypeIndexStepSignature) )
            {
                this.ComputeEventTypeIndexStepSignatures();
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

        private void ComputeEventTypeIndexStepSignatures()
        {
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
                progStep.Signature = new EventTypeIndexStepSignature(progStep, currentIndex);
                inboxEventIndexCounter[ieicKey] = currentIndex + 1;
            }
        }

        public override string GetReport()
        {
            // return this.DTupleTreeRoot.GetReport(this.MaxDToCount);

            StringBuilder sb = new StringBuilder();
            foreach (Tuple<int, Tuple<ulong, ulong, ulong>> stat in this.StatLogger)
            {
                sb.AppendLine($"{stat.Item1}\t:\t{stat.Item2.Item1}\t{stat.Item2.Item2}\t{stat.Item2.Item3}");
            }

            Tuple<int, Tuple<ulong, ulong, ulong>> finalStat = this.StatLogger.GetFinalValue();
            sb.AppendLine("-\t:\t-\t-\t-");
            sb.AppendLine($"{finalStat.Item1}\t:\t{finalStat.Item2.Item1}\t{finalStat.Item2.Item2}\t{finalStat.Item2.Item3}");

            return sb.ToString();
        }
    }
}
