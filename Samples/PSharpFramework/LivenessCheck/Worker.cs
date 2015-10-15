using System;
using Microsoft.PSharp;

namespace LivenessCheck
{
    internal class Worker : Machine
    {
        MachineId Master;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Processing))]
        class Init : MachineState { }

        void Configure()
        {
            this.Master = (this.ReceivedEvent as Config).Id;
            this.Raise(new Unit());
        }
        
        [OnEventGotoState(typeof(DoProcessing), typeof(Done))]
        class Processing : MachineState { }

        [OnEntry(nameof(DoneOnEntry))]
        class Done : MachineState { }

        void DoneOnEntry()
        {
            if (this.Nondet())
            {
                this.Send(this.Master, new FinishedProcessing());
            }
            
            this.Raise(new Halt());
        }
    }
}
