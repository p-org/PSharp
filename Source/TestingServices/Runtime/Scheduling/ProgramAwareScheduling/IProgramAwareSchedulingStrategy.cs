// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    internal interface IProgramAwareSchedulingStrategy : ISchedulingStrategy
    {
        void RecordCreateMachine(Machine createdMachine, Machine creatorMachine);

#if false
        [Obsolete("targetMachine/EventInfo is not set if the target is halted. Use the MachineId/Event version.")]
        void RecordSendEvent(AsyncMachine sender, Machine targetMachine, EventInfo eventInfo);
#endif
        void RecordReceiveEvent(Machine machine, Event e, EventInfo eventInfo);

        void RecordSendEvent(AsyncMachine sender, MachineId targetMachineId, EventInfo eventInfo, Event e);

        // Non-det choices
        void RecordNonDetBooleanChoice(bool boolChoice);

        void RecordNonDetIntegerChoice(int intChoice);

        // Functions to keep things neat.

        /// <summary>
        /// To be called when the scheduler has stopped scheduling
        /// ( whether it completed, hit the bound or found a bug )
        /// </summary>
        void NotifySchedulingEnded(bool bugFound);

        string GetProgramTrace();

        string GetReport();

        void RecordMonitorEvent(Type monitorType, AsyncMachine sender, Event e);

        void RecordStartMachine(Machine machine, Event initialEvent1, EventInfo initialEvent);
    }
}
