// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Core.Tests.LogMessages
{
    internal class CustomLogFormatter : RuntimeLogFormatter
    {
        public override string FormatOnEnqueueLogMessage(MachineId machineId, string eventName) => $"<EnqueueLog>.";

        public override string FormatOnDequeueLogMessage(MachineId machineId, string currStateName, string eventName) => $"<DequeueLog>.";

        public override string FormatOnSendLogMessage(MachineId targetMachineId, MachineId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted) =>
            $"<SendLog>.";

        public override string FormatOnCreateMachineLogMessage(MachineId machineId, MachineId creator) => $"<CreateLog>.";

        public override string FormatOnMachineStateLogMessage(MachineId machineId, string stateName, bool isEntry) => $"<StateLog>.";

        public override string FormatOnMachineActionLogMessage(MachineId machineId, string currStateName, string actionName) =>
            $"<ActionLog>.";
    }
}
