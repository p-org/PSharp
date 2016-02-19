using System;
using Microsoft.PSharp;

namespace BoundedAsync
{
    internal class Process : Machine
    {
        internal class Configure : Event
        {
            public MachineId Id;

            public Configure(MachineId id)
                : base()
            {
                this.Id = id;
            }
        }

        internal class Initialize : Event
        {
            public MachineId Left;
            public MachineId Right;

            public Initialize(MachineId left, MachineId right)
                : base()
            {
                this.Left = left;
                this.Right = right;
            }
        }

        internal class MyCount : Event
        {
            public int Count;

            public MyCount(int count)
                : base()
            {
                this.Count = count;
            }
        }

        internal class Resp : Event { }
        internal class Req : Event { }

        private MachineId Scheduler;
        private MachineId Left;
        private MachineId Right;

        private int Count;

        [Start]
        [OnEventDoAction(typeof(Configure), nameof(ConfigureAction))]
        [OnEventDoAction(typeof(Initialize), nameof(InitializeAction))]
        [OnEventGotoState(typeof(Resp), typeof(Counting))]
        [OnEventGotoState(typeof(MyCount), typeof(Init))]
        class Init : MachineState { }

        void ConfigureAction()
        {
            this.Scheduler = (this.ReceivedEvent as Configure).Id;
            this.Count = 0;
        }

        void InitializeAction()
        {
            this.Left = (this.ReceivedEvent as Initialize).Left;
            this.Right = (this.ReceivedEvent as Initialize).Right;
            this.Send(this.Scheduler, new Req());
        }

        [OnEntry(nameof(CountingOnEntry))]
        [OnEventGotoState(typeof(Resp), typeof(Counting))]
        [OnEventDoAction(typeof(MyCount), nameof(ConfirmInSync))]
        class Counting : MachineState { }

        void CountingOnEntry()
        {
            this.Count++;

            this.Send(this.Left, new MyCount(this.Count));
            this.Send(this.Right, new MyCount(this.Count));
            this.Send(this.Scheduler, new Req());

            if (this.Count == 10)
            {
                this.Raise(new Halt());
            }
        }

        void ConfirmInSync()
        {
            int count = (this.ReceivedEvent as MyCount).Count;
            this.Assert(this.Count <= count && this.Count >= count - 1);
        }
    }
}
