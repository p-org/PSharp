using System;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace AsyncAwaitCheck
{
    internal class TaskCreator : Machine
    {
        int Value;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Value = 0;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            Process();
            this.Assert(this.Value < 3, "Value is '{0}' (expected less than '3').", this.Value);
        }

        async void Process()
        {
            Task t = Increment();
            this.Value++;
            await t;
            this.Value++;
        }

        Task Increment()
        {
            Task t = new Task(() => {
                this.Value++;
            });

            t.Start();
            return t;
        }
    }
}
