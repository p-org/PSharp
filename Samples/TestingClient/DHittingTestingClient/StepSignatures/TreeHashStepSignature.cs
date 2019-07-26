// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace DHittingTestingClient
{
    /// <summary>
    /// Defines the signature of a step as a function of the steps which 'caused' this step.
    /// We define a tree where there is an edge from a step A to a step B if:
    ///     1. B is the step after A in a handler.
    ///     2. B is the start step of a create step A
    ///     3. B is the receive step of a send step A
    /// A step A causes a step B if there is a path from A to B in this tree.
    /// </summary>
    public class TreeHashStepSignature : IProgramStepSignature
    {
        internal ulong Hash;

        private TreeHashStepSignature()
        {
            // This exists only for the RootSignature
        }

        internal TreeHashStepSignature(ProgramStep step, Dictionary<ulong, ulong> machineIdRemap)
        {
            this.ComputeHash(step, machineIdRemap);

            step.Signature = this;
        }

        private const int MOD = 1000000007;

        private void ComputeHash(ProgramStep step, Dictionary<ulong, ulong> machineIdRemap)
        {
            // The hash is a function of the parent's hashes and the contents of this step.
            ulong parentHash = 1;
            // if (this.ProgramStep.ProgramStepType == ProgramStepType.SchedulableStep && this.ProgramStep.OpType == AsyncOperationType.Receive)
            if (step.ProgramStepType == ProgramStepType.SchedulableStep && step.CreatorParent != null)
            {
                parentHash *= (step.CreatorParent.Signature as TreeHashStepSignature).Hash;
            }
            else if (step.PrevMachineStep != null && step.PrevMachineStep.Signature is TreeHashStepSignature)
            {
                // Else if because in a "tree" hash handlers, should not be connected to previous handler
                // ( then it would have more than one parent and not be a tree )
                parentHash *= (step.PrevMachineStep.Signature as TreeHashStepSignature).Hash;
            }

            ulong stepHash = 1;
            switch (step.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    stepHash = ((ulong)step.OpType + 1) * (machineIdRemap[step.SrcId] + 1);
                    stepHash %= MOD;
                    if (step.OpType != AsyncOperationType.Create)
                    {
                        stepHash *= ~(machineIdRemap[step.TargetId] + 1);
                    }

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

        public override string ToString()
        {
            return "TreeHash:" + this.Hash;
            // return $"{this.ProgramStep.TotalOrderingIndex}:{this.ProgramStep.SrcId}:{this.ProgramStep.OpType}:{this.ProgramStep.TargetId}";
        }

        public override bool Equals(object other)
        {
            if (other is TreeHashStepSignature)
            {
                TreeHashStepSignature otherSig = other as TreeHashStepSignature;
                return otherSig.Hash == this.Hash;
                // return
                //    otherSig.Hash == this.Hash &&
                //    // Some easy checks to save us from silly collissions
                //    otherSig.ProgramStep.OpType.Equals(this.ProgramStep.OpType);
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

        private const ulong ROOTHASH = 499979;

        internal static IProgramStepSignature CreateRootStepSignature()
        {
            return new TreeHashRootStepSignature(ROOTHASH);
        }

        private class TreeHashRootStepSignature : TreeHashStepSignature
        {
            internal TreeHashRootStepSignature(ulong hash)
            {
                this.Hash = hash;
            }

            public override string ToString()
            {
                return "RootStep";
            }
        }
    }
}
