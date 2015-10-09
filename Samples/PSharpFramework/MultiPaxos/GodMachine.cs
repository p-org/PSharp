using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class GodMachine : Machine
    {
        List<Id> PaxosNodes;
        Id Client;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.PaxosNodes = new List<Id>();

            this.PaxosNodes.Insert(0, this.CreateMachine(typeof(PaxosNode), 3));
            this.PaxosNodes.Insert(0, this.CreateMachine(typeof(PaxosNode), 2));
            this.PaxosNodes.Insert(0, this.CreateMachine(typeof(PaxosNode), 1));

            foreach (var node in this.PaxosNodes)
            {
                this.Send(node, new allNodes(), this.PaxosNodes);
            }

            this.Client = this.CreateMachine(typeof(Client), this.PaxosNodes);
        }
    }
}
