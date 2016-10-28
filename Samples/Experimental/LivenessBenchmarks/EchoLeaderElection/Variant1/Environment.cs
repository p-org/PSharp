using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Variant1
{
    class Environment : Machine
    {
        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        #endregion

        #region actions
        void InitOnEntry()
        {
            var doneMachine = CreateMachine(typeof(DoneMachine));
            var nrLeadersMachine = CreateMachine(typeof(NrLeadersMachine));
            var leaderMachine = CreateMachine(typeof(Leader));

            var n0 = CreateMachine(typeof(Node), new Node.Initialize(0));
            var n1 = CreateMachine(typeof(Node), new Node.Initialize(1));
            var n2 = CreateMachine(typeof(Node), new Node.Initialize(2));
            Send(n0, new Node.Config(n1, 1, n2, 2, doneMachine, nrLeadersMachine, leaderMachine));
            Send(n1, new Node.Config(n0, 0, n2, 2, doneMachine, nrLeadersMachine, leaderMachine));
            Send(n2, new Node.Config(n0, 0, n1, 1, doneMachine, nrLeadersMachine, leaderMachine)); 

            while (true)
            {
                Send(doneMachine, new DoneMachine.SetValue(0));
                Send(nrLeadersMachine, new NrLeadersMachine.SetValue(0));
                Send(leaderMachine, new Leader.SetValue(3));
                Send(n0, new Node.Tok());
                Send(n1, new Node.Tok());
                Send(n2, new Node.Tok());
                Send(doneMachine, new DoneMachine.GetValue(Id));
                Receive(typeof(DoneMachine.GotValue), new Func<Event, bool>(e => ((DoneMachine.GotValue)e).done == 3));
            }
        }
        #endregion
    }
}
