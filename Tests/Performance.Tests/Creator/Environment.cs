using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace Creator
{
    internal class Environment : Machine
    {
        internal class Config : Event
        {
            public int NumberOfNodes;

            public Config(int numOfNodes)
                : base()
            {
                this.NumberOfNodes = numOfNodes;
            }
        }

        List<MachineId> Nodes;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            int numOfNodes = (this.ReceivedEvent as Config).NumberOfNodes;

            this.Nodes = new List<MachineId>();

            Profiler profiler = new Profiler();
            profiler.StartMeasuringExecutionTime();

            for (int idx = 0; idx < numOfNodes; idx++)
            {
                var node = this.CreateMachine(typeof(Node));
                this.Nodes.Add(node);
            }

            profiler.StopMeasuringExecutionTime();
            Console.WriteLine($"... Created {numOfNodes} machines in '" +
                profiler.Results() + "' seconds.");
        }
    }
}
