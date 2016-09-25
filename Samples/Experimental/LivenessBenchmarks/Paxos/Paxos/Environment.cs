using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos
{
    class Environment : Machine
    {
        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var acceptor1 = CreateMachine(typeof(Acceptor));
            var acceptor2 = CreateMachine(typeof(Acceptor));
            var acceptor3 = CreateMachine(typeof(Acceptor));

            List<MachineId> acceptors = new List<MachineId>();
            acceptors.Add(acceptor1);
            acceptors.Add(acceptor2);
            acceptors.Add(acceptor3);
            var proposer1 = CreateMachine(typeof(Proposer),
                new Proposer.Initialize(acceptors));
            var proposer2 = CreateMachine(typeof(Proposer),
                new Proposer.Initialize(acceptors));
            Send(proposer1, new Proposer.Propose());
            Send(proposer2, new Proposer.Propose());
        }
        #endregion
    }
}
