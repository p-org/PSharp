using Microsoft.PSharp;
using OperationsExample.Events;
using System.Collections.Generic;
using System.Linq;

namespace OperationsExample
{
    internal class GodMachine : Machine
    {

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            MachineId Client = this.CreateMachine(typeof(Client));

            List<MachineId> ClusterNodes = Enumerable.Range(0, 4)
                .Select((i) => this.CreateMachine(typeof(Node)))
                .ToList();
            
            foreach (var i in ClusterNodes.Select((n, i) => new { N = n, Id = i }))
            {
                this.Send(i.N, new Node.Config(i.Id, ClusterNodes));
            }

            this.Send(ClusterNodes[0], new MasterEvents.RegisterClient(Client));
            this.Send(Client, new ClientEvents.Config(ClusterNodes[0]));
        }
    }
}
