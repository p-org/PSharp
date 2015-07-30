using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class ValidityCheck : Monitor
    {
        List<MachineId> Workers;

        [Start]
        [Hot]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Done))]
        [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Workers = (List<MachineId>)this.Payload;
        }

        void ProcessNotification()
        {
            this.Workers.RemoveAt(0);

            if (this.Workers.Count == 0)
            {
                this.Raise(new Unit());
            }
        }
        
        class Done : MonitorState { }
    }
}
