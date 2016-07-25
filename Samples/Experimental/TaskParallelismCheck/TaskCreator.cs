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
            for (int idx = 0; idx < 5; idx++)
            {
                Task.Factory.StartNew(() =>
                {
                    var t = Task.Factory.StartNew(() =>
                    {
                        this.Value++;
                    });

                    Task.Factory.StartNew(() =>
                    {
                        this.Value++;
                    });
                });
            }
            
            this.Assert(this.Value < 10, "Value is '{0}' (expected less than '10').", this.Value);
        }
    }
}
