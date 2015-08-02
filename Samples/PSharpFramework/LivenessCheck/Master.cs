using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LivenessCheck
{
    internal class Master : Machine
    {
        List<MachineId> Workers;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Workers = new List<MachineId>();

            for (int idx = 0; idx < 3; idx++)
            {
                var worker = this.CreateMachine(typeof(Worker), this.Id);
                this.Workers.Add(worker);
            }

            this.CreateMonitor(typeof(M), this.Workers);
            
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(FinishedProcessing), nameof(ProcessWorkerIsDone))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            foreach (var worker in this.Workers)
            {
                this.Send(worker, new DoProcessing());
            }
        }

        void ProcessWorkerIsDone()
        {
            this.Monitor<M>(new NotifyWorkerIsDone());
        }
    }
}
