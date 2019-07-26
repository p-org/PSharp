// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures
{
    internal class EventHashStepSignature : IProgramStepSignature
    {
        internal int Hash;

        internal EventHashStepSignature(IProgramStep step)
        {
            this.Hash = step.EventInfo?.HashedState ?? 1;
        }

        public override bool Equals(object other)
        {
            if (other is EventHashStepSignature)
            {
                EventHashStepSignature otherSig = other as EventHashStepSignature;
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
            return this.Hash;
        }
    }
}
