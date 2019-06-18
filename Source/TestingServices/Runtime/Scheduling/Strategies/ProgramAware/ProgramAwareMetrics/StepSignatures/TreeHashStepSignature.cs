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

        private const int MOD = 1000000007;

        private void ComputeHash()
        {
            // The hash is a function of the parent's hashes and the contents of this step.
            ulong parentHash = 1;
            if (this.ProgramStep.ProgramStepType == ProgramStepType.SchedulableStep && this.ProgramStep.OpType == AsyncOperationType.Receive)
            {
                parentHash *= (this.ProgramStep.CreatorParent.Signature as TreeHashStepSignature).Hash;
            }
            else if (this.ProgramStep.PrevMachineStep != null && this.ProgramStep.PrevMachineStep.Signature is TreeHashStepSignature)
            {
                // Else if because in a "tree" hash handlers, should not be connected to previous handler
                // ( then it would have more than one parent and not be a tree )
                parentHash *= (this.ProgramStep.PrevMachineStep.Signature as TreeHashStepSignature).Hash;
            }

            ulong stepHash = 1;
            switch (this.ProgramStep.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    stepHash = ((ulong)this.ProgramStep.OpType + 1) * (this.ProgramStep.SrcId + 1);
                    stepHash %= MOD;
                    stepHash *= ~(this.ProgramStep.TargetId + 1);
                    stepHash %= MOD;
                    break;
                case ProgramStepType.NonDetBoolStep:
                    stepHash = 99991;
                    break;
                case ProgramStepType.NonDetIntStep:
                    stepHash = 99989;
                    break;
                case ProgramStepType.SpecialProgramStepType:
                    stepHash = 99971;
                    break;
            }

            this.Hash = (~parentHash) * stepHash;
            this.Hash %= 1000000007;
        }

#if false
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
#endif
        public override string ToString()
        {
            return $"{this.ProgramStep.TotalOrderingIndex}:{this.ProgramStep.SrcId}:{this.ProgramStep.OpType}:{this.ProgramStep.TargetId}";
        }

        public override bool Equals(object other)
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

        public override int GetHashCode()
        {
            return Convert.ToInt32(this.Hash);
        }
    }
}
