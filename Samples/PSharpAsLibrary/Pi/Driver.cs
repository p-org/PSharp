using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Pi
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

        MachineId Master;
        List<MachineId> Workers;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var numOfWorkers = (this.ReceivedEvent as Config).Input;

            this.Workers = new List<MachineId>();
            for (int idx = 0; idx < numOfWorkers; idx++)
            {
                this.Workers.Add(this.CreateMachine(typeof(Worker),
                    new Worker.Config(idx, numOfWorkers)));
            }

            this.Master = this.CreateMachine(typeof(Master),
                    new Master.Config(this.Workers));
            
            this.Send(this.Master, new Master.Boot());
            this.Raise(new Halt());
        }
    }
}
