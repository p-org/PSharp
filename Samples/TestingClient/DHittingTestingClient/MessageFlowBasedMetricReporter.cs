using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace DHittingTestingClient
{
    public class MessageFlowBasedMetricReporter : AbstractDHittingReporter
    {

        public MessageFlowBasedMetricReporter(int maxDToCount, DHittingUtils.DHittingSignature stepSignatureType)
            : base(maxDToCount, stepSignatureType)
        {
        }

        protected override void ResetLocalVariables()
        {
            // Nothing to do
        }

        protected override void EnumerateDTuples(List<ProgramStep> schedule)
        {
            Dictionary<IProgramStepSignature, List<int>> stepIndexToDTNodes = new Dictionary<IProgramStepSignature, List<int>>();
            // Pre-insert an entry for each signature
            int[] latestNodeWhichCausesStep = new int[schedule.Count];
            foreach (ProgramStep progStep in schedule)
            {
                stepIndexToDTNodes[progStep.Signature] = new List<int>();
                latestNodeWhichCausesStep[progStep.TotalOrderingIndex] = 0; // Hopefully, I've set it up in a way that 0 causes everything.
            }

            // Recursively construct tree
            foreach (ProgramStep progStep in schedule)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep && progStep.OpType == AsyncOperationType.Send)
                {
                    int newNode = this.DTupleTree.AddOrUpdateChild(DTupleTree.RootIdx, progStep.Signature);
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

        private void RecursiveMarkCausal(ProgramStep progStep, ProgramStep atStep, int[] latestNodeWhichCausesStep)
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
        private void RecursiveAdd(ProgramStep signatureStep, ProgramStep currentNode, Dictionary<IProgramStepSignature, List<int>> stepIndexToDTNodes, int[] latestNodeWhichCausesStep)
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
            ProgramStep nextEnqueuedStep = currentNode.CreatorParent?.NextEnqueuedStep ?? null;
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


        private void InsertIntoDtupleTree(ProgramStep signatureStep, ProgramStep toBeInserted, Dictionary<IProgramStepSignature, List<int>> stepIndexToDTNodes)
        {
            foreach (int dtn in stepIndexToDTNodes[signatureStep.Signature])
            {
                if ( DTupleTree.GetNodeDepth(dtn) < this.MaxDToCount)
                {
                    int newNode = this.DTupleTree.AddOrUpdateChild(dtn, toBeInserted.Signature);
                    if (DTupleTree.GetNodeDepth(newNode) < this.MaxDToCount)
                    {
                        stepIndexToDTNodes[toBeInserted.Signature].Add(newNode);
                    }
                }
            }
        }
    }
}
