// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware
{
    internal class ProgramAgnosticWrapperStrategy : IProgramAwareSchedulingStrategy
    {
        private readonly ISchedulingStrategy ChildStrategy;

        internal ProgramAgnosticWrapperStrategy(ISchedulingStrategy childStrategy)
        {
            this.ChildStrategy = childStrategy;
        }

        public string GetDescription()
        {
            return "Wrapper strategy to count d-tuples hit by the (set of) schedules";
        }

        public bool IsFair()
        {
            return this.ChildStrategy.IsFair();
        }

        public void Reset()
        {
            // TODO
            this.ChildStrategy.Reset();
        }

        public bool PrepareForNextIteration()
        {
            return this.ChildStrategy.PrepareForNextIteration();
        }

        public int GetScheduledSteps()
        {
            return this.ChildStrategy.GetScheduledSteps();
        }

        public bool HasReachedMaxSchedulingSteps()
        {
            return this.ChildStrategy.HasReachedMaxSchedulingSteps();
        }

        // Scheduling Choice(?)s
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
            => this.ChildStrategy.ForceNext(next, ops, current);

        public void ForceNextBooleanChoice(int maxValue, bool next)
            => this.ChildStrategy.ForceNextBooleanChoice(maxValue, next);

        public void ForceNextIntegerChoice(int maxValue, int next)
            => this.ChildStrategy.ForceNextIntegerChoice(maxValue, next);

        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            // TODO
            return this.ChildStrategy.GetNext(out next, ops, current);
        }

        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            return this.ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            return this.ChildStrategy.GetNextIntegerChoice(maxValue, out next);
        }

        // The program-aware part
        public void RecordCreateMachine(Machine createdMachine, Machine creatorMachine)
        {
        }

        public void RecordSendEvent(AsyncMachine sender, MachineId targetMachineId, EventInfo eventInfo, Event e)
        {
        }

        public void RecordReceiveEvent(Machine machine, EventInfo eventInfo)
        {
        }

        public void RecordNonDetBooleanChoice(bool boolChoice)
        {
        }

        public void RecordNonDetIntegerChoice(int intChoice)
        {
        }

        public virtual void NotifySchedulingEnded(bool bugFound)
        {
            // Do nothing
        }

        public string GetProgramTrace()
        {
            return string.Empty;
        }

        public string GetReport()
        {
            return null;
        }

        public void RecordMonitorEvent(Type monitorType, AsyncMachine sender, Event e)
        {
            // Do Nothing
        }

        public void RecordStartMachine(Machine machine, EventInfo initialEvent)
        {
            // Do nothing
        }
#if false
        public void RecordSendEvent(AsyncMachine sender, Machine targetMachine, EventInfo eventInfo)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
