// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

// #define USE_DETUPLETREENODE

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using System.Diagnostics;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace DHittingTestingClient
{
    public class InboxBasedDHittingMetricReporter : AbstractDHittingReporter
    {
        public InboxBasedDHittingMetricReporter(int maxDToCount, DHittingUtils.DHittingSignature stepSignatureType)
            : base(maxDToCount, stepSignatureType)
        { }



        protected override void ResetLocalVariables()
        {
            // Nothing to do
        }

        protected override void EnumerateDTuples(List<ProgramStep> schedule)
        {
            this.DebugStatStopWatch.Start();

            HashSet<ulong> doneMachines = new HashSet<ulong>();

            foreach (ProgramStep progStep in schedule)
            {
                if (progStep.ProgramStepType == ProgramStepType.SchedulableStep &&
                    progStep.OpType == Microsoft.PSharp.TestingServices.Scheduling.AsyncOperationType.Send)
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

            this.DebugStatStopWatch.Stop();
        }

        private void GenerateDTuples(ProgramStep currentStep, List<int> nodesToAddTo)
        {
            List<int> newAdditions = new List<int>();
            foreach (int tn in nodesToAddTo)
            {
                int childIdx = this.DTupleTree.AddOrUpdateChild(tn, currentStep.Signature);
                if (DTupleTree.GetNodeDepth(childIdx) < this.MaxDToCount)
                {
                    newAdditions.Add(childIdx);
                }

                this.DebugStatAddOrUpdateChildCalls++;
            }

            nodesToAddTo.AddRange(newAdditions);
            if (currentStep.NextEnqueuedStep != null)
            {
                this.GenerateDTuples(currentStep.NextEnqueuedStep, nodesToAddTo);
            }
        }
    }
}
