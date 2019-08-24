// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Core.Tests.LogMessages
{
    internal class CustomLogWriter : RuntimeLogWriter
    {
        public override void OnEnqueue(MachineId machineId, string eventName)
        {
        }

        public override void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
        }
    }
}
