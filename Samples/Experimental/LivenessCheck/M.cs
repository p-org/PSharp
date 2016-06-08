using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LivenessCheck
{
    internal class M : Monitor
    {
        List<MachineId> Workers;

        [Start]
        [Hot]
        [OnEventGotoState(typeof(Unit), typeof(Done))]
        [OnEventDoAction(typeof(MConfig), nameof(Configure))]
        [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
        class Init : MonitorState { }

        void Configure()
        {
            this.Workers = (this.ReceivedEvent as MConfig).Ids;
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
