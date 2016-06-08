using System;
using Microsoft.PSharp;

namespace BoundedAsync
{
    internal class Scheduler : Machine
    {
        private class Resp : Event { }
        private class Unit : Event { }

        MachineId Process1;
        MachineId Process2;
        MachineId Process3;

        int Count;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Sync))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Process1 = this.CreateMachine(typeof(Process));
            this.Process2 = this.CreateMachine(typeof(Process));
            this.Process3 = this.CreateMachine(typeof(Process));

            this.Send(this.Process1, new Process.Configure(this.Id));
            this.Send(this.Process2, new Process.Configure(this.Id));
            this.Send(this.Process3, new Process.Configure(this.Id));

            this.Send(this.Process1, new Process.Initialize(this.Process3, this.Process2));
            this.Send(this.Process2, new Process.Initialize(this.Process3, this.Process1));
            this.Send(this.Process3, new Process.Initialize(this.Process1, this.Process2));

            this.Count = 0;

            this.Raise(new Unit());
        }

        [OnExit(nameof(ActiveOnExit))]
        [OnEventGotoState(typeof(Resp), typeof(Sync))]
        [OnEventDoAction(typeof(Process.Req), nameof(CountReq))]
        class Sync : MachineState { }

        void ActiveOnExit()
        {
            this.Send(this.Process1, new Process.Resp());
            this.Send(this.Process2, new Process.Resp());
            this.Send(this.Process3, new Process.Resp());
        }

        void CountReq()
        {
            this.Count++;
            if (this.Count == 3)
            {
                this.Count = 0;
                this.Raise(new Resp());
            }
        }
    }
}
