using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LivenessCheck
{
    internal class M : Monitor
    {
        List<Id> Workers;

        [Start]
        [Hot]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Done))]
        [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Workers = (List<Id>)this.Payload;
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
