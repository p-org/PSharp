using System;
using System.Threading;
using Microsoft.PSharp;

namespace Creator
{
    internal class Node : Machine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            //this.Execute();
        }

        void Execute()
        {
            Thread.SpinWait(50);
        }
    }
}
