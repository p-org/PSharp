using System;
using Microsoft.PSharp;

namespace LivenessCheck
{
    internal class Worker : Machine
    {
        MachineId Master;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Processing))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Master = (MachineId)this.Payload;
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
