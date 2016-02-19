using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MultiPaxosRacy
{
    class GodMachine : Machine
    {
        #region events
        private class eLocal : Event { }
        #endregion

        #region fields
        private List<MachineId> PaxosNodes;
        private MachineId Client;

        private MachineId PaxosMonitor;
        private MachineId ValidityMonitor;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eLocal), typeof(End))]
        private class Init : MachineState { }

        [OnEntry(nameof(OnEndEntry))]
        private class End : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[GodMachine] Initializing ...\n");

            PaxosMonitor = CreateMachine(typeof(PaxosInvariantMonitor));
            ValidityMonitor = CreateMachine(typeof(ValidityCheckMonitor));

            // Create the paxos nodes.
            PaxosNodes = new List<MachineId>();
            for (int i = 0; i < 3; i++)
            {
                MachineId pId = CreateMachine(typeof(PaxosNode));
                Send(pId, new PaxosNode.eInitialize(new Tuple<int, MachineId, MachineId>(i + 1, PaxosMonitor, ValidityMonitor)));
                PaxosNodes.Insert(0, pId);
            }

            // Send all paxos nodes the other machines.
            for (int i = 0; i < PaxosNodes.Count; i++)
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(PaxosNode.eAllNodes), PaxosNodes[i]);
                this.Send(PaxosNodes[i], new PaxosNode.eAllNodes(PaxosNodes));
            }

            // Create the client node.
            Client = CreateMachine(typeof(Client));
            Send(Client, new Client.eInitialize(new Tuple<List<MachineId>, MachineId>(
                PaxosNodes, ValidityMonitor)));

            this.Raise(new eLocal());
        }

        private void OnEndEntry()
        {
            Console.WriteLine("[GodMachine] Ending ...\n");

            Raise(new Halt());
        }
        #endregion
    }
}