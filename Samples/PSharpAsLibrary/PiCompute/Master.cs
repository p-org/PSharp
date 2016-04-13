using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Pi
{
    internal class Master : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> Workers;

            public Config(List<MachineId> workers)
                : base()
            {
                this.Workers = workers;
            }
        }

        internal class Sum : Event
        {
            public double Result;

            public Sum(double result)
                : base()
            {
                this.Result = result;
            }
        }

        internal class Boot : Event { }

        List<MachineId> Workers;

        double Result;
        int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Workers = (this.ReceivedEvent as Config).Workers;
            this.Result = 0.0;
            this.Counter = 0;
            
            this.Goto(typeof(Active));
        }
        
        [OnEventDoAction(typeof(Boot), nameof(DoBoot))]
        [OnEventDoAction(typeof(Sum), nameof(DoSum))]
        class Active : MachineState { }
        
        void DoBoot()
        {
            int n = 30000;
            foreach (var worker in this.Workers)
            {
                this.Send(worker, new Worker.Intervals(this.Id, n));
            }
        }

        void DoSum()
        {
            double p = (this.ReceivedEvent as Sum).Result;

            this.Counter = this.Counter + 1;
            this.Result = this.Result + p;

            if (this.Counter == this.Workers.Count)
            {
                foreach (var worker in this.Workers)
                {
                    this.Send(worker, new Halt());
                }

                Console.WriteLine("Result is {0}\n", this.Result);

                this.Raise(new Halt());
            }
        }
    }
}
