using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace LeaderElection
{
    internal class Driver : Machine
    {
        internal class Config : Event
        {
            public int Input;

            public Config(int input)
                : base()
            {
                this.Input = input;
            }
        }

        List<MachineId> Processes;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var numOfProcesses = (this.ReceivedEvent as Config).Input;

            this.Processes = new List<MachineId>();
            for (int idx = 0; idx < numOfProcesses; idx++)
            {
                this.Processes.Add(this.CreateMachine(typeof(LeaderElectionProcess)));
            }

            for (int idx = 0; idx < numOfProcesses; idx++)
            {
                var process = this.Processes[idx];
                var rightProcess = this.Processes[(idx + 1) % numOfProcesses];
                this.Send(process, new LeaderElectionProcess.Config(idx, rightProcess));
            }

            for (int idx = 0; idx < numOfProcesses; idx++)
            {
                var process = this.Processes[idx];
                this.Send(process, new LeaderElectionProcess.Start());
            }
            
            this.Raise(new Halt());
        }
    }
}
