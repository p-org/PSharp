// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

#define HANDLERS_ARE_LINKED // Handlers are linked at the moment

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Microsoft.PSharp.TestingServices.Statistics;

// Considers the possibility of re-ordered events in inbox of machine A effecting the downstream behaviour of a machine B.
// A send event A is considered to come before a send event B if any information in A could possibly effect B.
//  Implementation-wise: Every send event A happens before a send event B
//      if there is a path from the corresponding receive event of A to the send event of B by traversing
//          - NextMachineStep edges
//          - CreatedStep edges.
//          - NextReceive edges ( only important if adjacent handlers are not connected my NextMachineStep )
// TODO: The above implementation
namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
    internal class MessageFlowBasedDHittingMetricStrategy : BasicProgramModelBasedStrategy
    {
        // private const bool AddCausalRelations = false;
        private readonly int MaxDToCount;
        // private readonly DTupleTreeNode DTupleTreeRoot;
        private int iterationNumber;

        private readonly DTupleTreeNode DTupleTreeRoot;

        private static HashSet<IProgramStepSignature> UniqueSigs = new HashSet<IProgramStepSignature>();
        private static HashSet<ulong> UniqueMachineSigs = new HashSet<ulong>();

        private readonly TimelyStatisticLogger<Tuple<ulong, ulong, ulong>> StatLogger;
        // private static HashSet<ulong> UniqueSigs = new HashSet<ulong>();

        // TODO: Move RootSignatures to respective signature classes. Declare const variables for primes.

        public MessageFlowBasedDHittingMetricStrategy(ISchedulingStrategy childStrategy, int dToHit)
            : base(childStrategy)
        {
            this.MaxDToCount = dToHit;
            this.iterationNumber = 0;
            this.DTupleTreeRoot = new DTupleTreeNode(TreeHashStepSignature.CreateRootStepSignature());

            this.StatLogger = new TimelyStatisticLogger<Tuple<ulong, ulong, ulong>>();
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            this.ComputeStepSignatures();
            this.EnumerateDTuples();

            this.StatLogger.AddValue(new Tuple<ulong, ulong, ulong>(
                this.GetDTupleCount(1), this.GetDTupleCount(2), this.GetDTupleCount(3)));

            // string programTreeSerialized = this.ProgramModel.HAXGetProgramTreeString();
            // string programSerialized = this.GetProgramTrace();
            // Console.WriteLine(programTreeSerialized);
            // DTupleTreeNode.PrintDTupleTree(this.DTupleTreeRoot, 0);
            // string dTupleTreeSerialized = DTupleTreeNode.HAXGetDTupleTreeString(this.DTupleTreeRoot);
            // Console.WriteLine(dTupleTreeSerialized);
            // for (int i = 2; i <= this.MaxDToCount; i++)
            // {
            //      Console.WriteLine($"{i}-tuple count={this.GetDTupleCount(i)}");
            // }
            // Console.WriteLine($"Unique hashes={UniqueSigs.Count}");
        }

        private void ComputeStepSignatures()
        {
            Dictionary<ulong, ulong> machineIdRemap = new Dictionary<ulong, ulong>();
            machineIdRemap[TESTHARNESSMACHINEID] = TESTHARNESSMACHINEHASH;
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                progStep.Signature = new TreeHashStepSignature(progStep, machineIdRemap);

                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == AsyncOperationType.Create)
                {
                    machineIdRemap[progStep.TargetId] = (progStep.Signature as TreeHashStepSignature).Hash;
                }

                UniqueSigs.Add(progStep.Signature);
            }

            foreach (var kvp in machineIdRemap)
            {
                UniqueMachineSigs.Add(kvp.Value);
            }
        }

        private void EnumerateDTuples()
        {
            this.iterationNumber++;
            Dictionary<IProgramStepSignature, List<DTupleTreeNode>> stepIndexToDTNodes = new Dictionary<IProgramStepSignature, List<DTupleTreeNode>>();
            // Pre-insert an entry for each signature
            int[] latestNodeWhichCausesStep = new int[this.ProgramModel.OrderedSteps.Count];
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                stepIndexToDTNodes[progStep.Signature] = new List<DTupleTreeNode>();
                latestNodeWhichCausesStep[progStep.TotalOrderingIndex] = 0; // Hopefully, I've set it up in a way that 0 causes everything.
            }

            // Recursively construct tree
            foreach (IProgramStep progStep in this.ProgramModel.OrderedSteps)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == AsyncOperationType.Send)
                {
                    DTupleTreeNode newNode = this.DTupleTreeRoot.AddOrUpdateChild(progStep.Signature, this.iterationNumber);
                    // This will always be the last one added. Makes things easy, because we need to pop it at one point.
                    stepIndexToDTNodes[progStep.Signature].Add(newNode);

                    // This step does indeed 'race' with signatureStep.
                    // this.RecursiveAdd(progStep, progStep.CreatedStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
                    if (progStep.NextEnqueuedStep != null)
                    {
                        // First, Mark all caused steps.
                        this.RecursiveMarkCausal(progStep, progStep, latestNodeWhichCausesStep);

                        // NextEnqueuedStep is not causally related
                        if (latestNodeWhichCausesStep[progStep.NextEnqueuedStep.TotalOrderingIndex] < progStep.TotalOrderingIndex)
                        {
                            // TODO: This is wrong. It needs a causality check.
                            this.InsertIntoDtupleTree(progStep, progStep.NextEnqueuedStep, stepIndexToDTNodes);
                            if (progStep.NextEnqueuedStep.CreatedStep != null)
                            {
                                this.RecursiveAdd(progStep, progStep.NextEnqueuedStep.CreatedStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
                            }
                        }
                    }

                    // We must now recurse on the causal tree of this guy to discover the 'transitive' relations.
                    // These must be added everywhere except at the depth-1 nodes
                    stepIndexToDTNodes[progStep.Signature].RemoveAt(stepIndexToDTNodes[progStep.Signature].Count - 1);

                    // Consider next steps in this handler
                    if (progStep.NextMachineStep != null && progStep.NextMachineStep.OpType != AsyncOperationType.Receive)
                    {
                        this.RecursiveAdd(progStep, progStep.NextMachineStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
                    }

                    // Consider steps caused by a send.
                    if (progStep.OpType == AsyncOperationType.Send && progStep.CreatedStep != null)
                    {
                        this.RecursiveAdd(progStep, progStep.CreatedStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
                    }
                }
            }
        }

        private void RecursiveMarkCausal(IProgramStep progStep, IProgramStep atStep, int[] latestNodeWhichCausesStep)
        {
            latestNodeWhichCausesStep[atStep.TotalOrderingIndex] = progStep.TotalOrderingIndex;

            if (atStep.NextMachineStep != null && atStep.NextMachineStep.OpType != AsyncOperationType.Receive)
            {
                this.RecursiveMarkCausal(progStep, atStep.NextMachineStep, latestNodeWhichCausesStep);
            }

            if (atStep.OpType == AsyncOperationType.Send && atStep.CreatedStep != null)
            {
                this.RecursiveMarkCausal(progStep, atStep.CreatedStep, latestNodeWhichCausesStep);
            }
        }

        // Recursively searches the program-subtree rooted at currentNode for send steps that come after the step with specified signature.
        // Any step which comes after the send step will have access to the information sent by it BUT not be 'caused' by it.
        // Any such step found is added as a child of ALL nodes in the dt-tree with the specified signature ( as long as depth limit is respected )
        // As these steps are added to the tree, the stepIndexToDTNodes index for the signature of the child-step is updated.
        // ( The children of these newly added steps will not be added till RecursiveAdd is called with their signatures)
        private void RecursiveAdd(IProgramStep signatureStep, IProgramStep currentNode, Dictionary<IProgramStepSignature, List<DTupleTreeNode>> stepIndexToDTNodes, int[] latestNodeWhichCausesStep)
        {
            // We do need to recurse on the events in the causal tree of events happening after signatureStep.
            // Consider next steps in this handler
            if (currentNode.NextMachineStep != null && currentNode.NextMachineStep.OpType != AsyncOperationType.Receive)
            {
                this.RecursiveAdd(signatureStep, currentNode.NextMachineStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
            }

            // Consider steps caused by a send.
            if (currentNode.OpType == AsyncOperationType.Send && currentNode.CreatedStep != null)
            {
                this.RecursiveAdd(signatureStep, currentNode.CreatedStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
            }

            // And now we go about adding the ones that 'race' with signatureStep or some step caused by signatureStep
            IProgramStep nextEnqueuedStep = currentNode.CreatorParent?.NextEnqueuedStep ?? null;
            if (currentNode.OpType == AsyncOperationType.Receive && nextEnqueuedStep != null
                // wE should recurse past the causal one // && latestNodeWhichCausesStep[nextEnqueuedStep.TotalOrderingIndex] < signatureStep.TotalOrderingIndex // Make sure it's not causally related to signatureStep.
                )
            {
                // This step does indeed 'race' with signatureStep.
                // Add a check as below // this.InsertIntoDtupleTree(signatureStep, nextEnqueuedStep, stepIndexToDTNodes);
                if (latestNodeWhichCausesStep[nextEnqueuedStep.TotalOrderingIndex] < signatureStep.TotalOrderingIndex)
                {
                    // Make sure it's not causally related to signatureStep before adding it.
                    this.InsertIntoDtupleTree(signatureStep, nextEnqueuedStep, stepIndexToDTNodes);
                }

                // Recurse on it. (A,B) in S ^ (B,C) in S means (A,C) in S.
                // We need to track (A,C) in our tree so we can capture all subsequences of a long chain.
                if (nextEnqueuedStep.CreatedStep != null)
                {
                    this.RecursiveAdd(signatureStep, nextEnqueuedStep.CreatedStep, stepIndexToDTNodes, latestNodeWhichCausesStep);
                }

                // else the message was enqueued and never dequeued
            }
        }

        private void InsertIntoDtupleTree(IProgramStep signatureStep, IProgramStep toBeInserted, Dictionary<IProgramStepSignature, List<DTupleTreeNode>> stepIndexToDTNodes)
        {
            foreach (DTupleTreeNode dtn in stepIndexToDTNodes[signatureStep.Signature])
            {
                if (dtn.Depth < this.MaxDToCount)
                {
                    DTupleTreeNode newNode = dtn.AddOrUpdateChild(toBeInserted.Signature, this.iterationNumber);
                    if (newNode.Depth < this.MaxDToCount)
                    {
                        stepIndexToDTNodes[toBeInserted.Signature].Add(newNode);
                    }
                }
            }
        }

        public ulong GetDTupleCount(int d)
        {
            return this.DTupleTreeRoot.GetDTupleCount(d);
        }

        public override string GetReport()
        {
            // return this.DTupleTreeRoot.GetReport(this.MaxDToCount);

            StringBuilder sb = new StringBuilder();
            foreach ( Tuple<int, Tuple<ulong, ulong, ulong>> stat in this.StatLogger)
            {
                sb.AppendLine($"{stat.Item1}\t:\t{stat.Item2.Item1}\t{stat.Item2.Item2}\t{stat.Item2.Item3}");
            }

            return sb.ToString();
        }
    }
}
