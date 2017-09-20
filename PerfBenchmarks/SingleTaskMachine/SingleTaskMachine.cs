using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace SingleTaskMachine
{ 
    /// <summary>
    /// Machine that hosts a single task
    /// </summary>
    [Fast]
    internal class SingleTaskMachine : Machine
    {
        [Start]
        [OnEntry(nameof(Run))]
        class InitState : MachineState { }

        /// <summary>
        /// Executes the payload
        /// </summary>
        async Task Run()
        {
            var function = (this.ReceivedEvent as SingleTaskMachineEvent).function;
            await function(this);
            this.Raise(new Halt());
        }

        /// <summary>
        /// Public Send
        /// </summary>
        public void MySend(MachineId target, Event e)
        {
            this.Send(target, e);
        }

    }
}