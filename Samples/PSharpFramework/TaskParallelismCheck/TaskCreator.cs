using System;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace TaskParallelismCheck
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
            Task.Factory.StartNew(() =>
            {
                this.Assert(this.Value == 0, "Value is '{0}' (expected '0').", this.Value);
            });

            this.Value = 1;
        }
    }
}
