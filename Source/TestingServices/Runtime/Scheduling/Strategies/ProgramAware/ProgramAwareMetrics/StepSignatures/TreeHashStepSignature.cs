// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures
{
    internal class TreeHashStepSignature : IProgramStepSignature
    {
        internal IProgramStep ProgramStep;
        internal ulong Hash;

        internal TreeHashStepSignature(IProgramStep step)
        {
            this.ProgramStep = step;
            this.ComputeHash();

            step.Signature = this;
        }

        private void ComputeHash()
        {
            // The hash is a function of the parent's hashes and the contents of this step.
            ulong parentHash = 1;
            if (this.ProgramStep.OpType == AsyncOperationType.Receive)
            {
                parentHash = (this.ProgramStep.CreatorParent.Signature as TreeHashStepSignature).Hash;
            }
            else
            {
                parentHash = (this.ProgramStep.PrevMachineStep.Signature as TreeHashStepSignature).Hash;
            }

            ulong stepHash = ((ulong)this.ProgramStep.OpType + 1) * this.ProgramStep.SrcId * (~this.ProgramStep.TargetId);
            this.Hash = (~parentHash) * stepHash;
        }

        public bool Equals(IProgramStepSignature other)
        {
            if (other is TreeHashStepSignature)
            {
                TreeHashStepSignature otherSig = other as TreeHashStepSignature;
                return
                    otherSig.Hash == this.Hash &&
                    // Some easy checks to save us from silly collissions
                    otherSig.ProgramStep.OpType.Equals(this.ProgramStep.OpType) &&
                    otherSig.ProgramStep.SrcId.Equals(this.ProgramStep.SrcId);
            }
            else
            {
                return false;
            }
        }
    }
}
