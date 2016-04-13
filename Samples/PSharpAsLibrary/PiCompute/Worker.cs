using System;
using Microsoft.PSharp;

namespace Pi
{
    internal class Worker : Machine
    {
        internal class Config : Event
        {
            public int Id;
            public int NumOfWorkers;

            public Config(int id, int num)
                : base()
            {
                this.Id = id;
                this.NumOfWorkers = num;
            }
        }

        internal class Intervals : Event
        {
            public MachineId Sender;
            public int Interval;

            public Intervals(MachineId sender, int interval)
                : base()
            {
                this.Sender = sender;
                this.Interval = interval;
            }
        }

        int WorkerId;
        int NumOfWorkers;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.WorkerId = (this.ReceivedEvent as Config).Id;
            this.NumOfWorkers = (this.ReceivedEvent as Config).NumOfWorkers;
            this.Goto(typeof(Active));
        }
        
        [OnEventDoAction(typeof(Intervals), nameof(DoIntervals))]
        class Active : MachineState { }
        
        private void DoIntervals()
        {
            var sender = (this.ReceivedEvent as Intervals).Sender;
            var n = (this.ReceivedEvent as Intervals).Interval;

            double h = 1.0 / n;
            double sum = 0;

            for (int idx = this.WorkerId; idx <= n; idx += this.NumOfWorkers)
            {
                double x = h * (idx - 0.5);
                sum = sum + (4.0 / (1.0 + x * x));
            }

            this.Send(sender, new Master.Sum(h * sum));
        }
    }
}
