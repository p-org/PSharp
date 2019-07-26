// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;

namespace DHittingTestingClient
{
    public class EventTypeIndexStepSignature : IProgramStepSignature
    {
        internal readonly ProgramStepEventInfo EventInfo;
        internal readonly int EventIndex;

        internal readonly ProgramStep HAXstep;
        private Type MachineType;

        internal EventTypeIndexStepSignature(ProgramStep step, Type machineType, int eventIndex)
        {
            this.EventInfo = step.EventInfo;
            this.EventIndex = eventIndex;
            this.MachineType = machineType;
            step.Signature = this;

            this.HAXstep = step;
        }

        public override int GetHashCode()
        {
            return this.EventInfo.Event.GetType().GetHashCode() * (this.EventIndex + 1);
        }

        public override bool Equals(object other)
        {
            if (other is EventTypeIndexStepSignature)
            {
                EventTypeIndexStepSignature otherSig = other as EventTypeIndexStepSignature;
                return
                    otherSig.MachineType.Equals(this.MachineType) &&
                    otherSig.EventInfo.Event.GetType().Equals(this.EventInfo.Event.GetType()) &&
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
