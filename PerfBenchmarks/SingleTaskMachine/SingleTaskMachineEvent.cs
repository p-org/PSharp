using System;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace SingleTaskMachine
{
    /// <summary>
    /// Event carrying payload for <see cref="SingleTaskMachine"/>
    /// </summary>
    internal class SingleTaskMachineEvent : Event
    {
        /// <summary>
        /// Payload
        /// </summary>
        public Func<SingleTaskMachine, Task> function;

        /// <summary>
        /// Constructor
        /// </summary>
        public SingleTaskMachineEvent(Func<SingleTaskMachine, Task> function)
        {
            this.function = function;
        }

    }
}