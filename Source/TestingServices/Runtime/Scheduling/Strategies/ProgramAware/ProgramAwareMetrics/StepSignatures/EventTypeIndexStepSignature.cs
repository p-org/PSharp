// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
    internal class EventTypeIndexStepSignature : IProgramStepSignature
    {
        internal readonly EventInfo EventInfo;
        internal readonly int EventIndex;

        internal readonly IProgramStep HAXstep;

        internal EventTypeIndexStepSignature(IProgramStep step, int eventIndex)
        {
            this.EventInfo = step.EventInfo;
            this.EventIndex = eventIndex;

            step.Signature = this;

            this.HAXstep = step;
        }

        public override int GetHashCode()
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.string.gethashcode?view=netframework-4.8
            // If two string objects are equal, the GetHashCode method returns identical values...
            return this.EventInfo.EventName.GetHashCode() * this.EventIndex;
        }

        public override bool Equals(object other)
        {
            if (other is EventTypeIndexStepSignature)
            {
                EventTypeIndexStepSignature otherSig = other as EventTypeIndexStepSignature;
                return
                    otherSig.EventInfo.EventName.Equals(this.EventInfo.EventName) &&
                    otherSig.EventIndex == this.EventIndex;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return $"{this.HAXstep.TotalOrderingIndex}:{this.HAXstep.SrcId}:{this.HAXstep.OpType}:{this.HAXstep.TargetId}";
            // return $"{this.HAXstep.TotalOrderingIndex}:{this.HAXstep.ProgramStepType}:{this.HAXstep.SrcId}:{this.HAXstep.OpType}:{this.HAXstep.TargetId}";
            // return $"{this.EventInfo.OriginInfo.SenderMachineName}:{this.EventInfo.EventName}:{this.EventInfo}";
        }
    }
}
