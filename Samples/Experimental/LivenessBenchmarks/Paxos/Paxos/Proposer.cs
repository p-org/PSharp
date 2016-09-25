using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos
{
    class Proposer : Machine
    {
        #region events
        public class Initialize : Event
        {
            public List<MachineId> Acceptors;
            public Initialize(List<MachineId> acceptors)
            {
                this.Acceptors = acceptors;
            }
        }
        public class Propose : Event { }
        public class Proposal_OK : Event
        {
            int Accepted;
            public Proposal_OK(int accepted)
            {
                this.Accepted = accepted;
            }
        }
        public class Declined : Event { }
        class Unit : Event { }
        #endregion

        #region fields
        Random RandomGenerator;
        int proposal;
        int agreed;

        List<MachineId> Acceptors;
        
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventGotoState(typeof(Unit), typeof(WaitingForEvents))]
        class Init : MachineState { }

        [OnEventDoAction(typeof(Propose), nameof(OnPropose))]
        [IgnoreEvents(typeof(Proposal_OK), typeof(Declined))]
        class WaitingForEvents : MachineState { }

        [OnEntry(nameof(SimulateTimeout))]
        [OnEventDoAction(typeof(Proposal_OK), nameof(OnProposal_OK))]
        [OnEventDoAction(typeof(Declined), nameof(OnDeclined))]
        [OnEventDoAction(typeof(Propose), nameof(OnPropose1))]
        class WaitingForAcceptorResponse : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var e = ReceivedEvent as Initialize;
            this.Acceptors = e.Acceptors;
            this.RandomGenerator = new Random();
            this.Raise(new Unit());
        }

        void OnPropose()
        {
            int n = RandomGenerator.Next(100);
            this.proposal = n;
            this.agreed = 0;

            foreach(var acceptor in Acceptors)
            {
                Send(acceptor, new Acceptor.Prepare(this.Id, n));
                this.Goto(typeof(WaitingForAcceptorResponse));
            }
        }

        void SimulateTimeout()
        {
            //if (!Random())
            //{
            //    Send(this.Id, new Propose());
            //}
        }

        void OnProposal_OK()
        {
            this.agreed = this.agreed + 1;
            if(this.agreed >= 2)
            {
                foreach(var acceptor in Acceptors)
                {
                    Console.WriteLine("proposing for accept: " + this.proposal);
                    Send(acceptor, new Acceptor.Accept(this.Id, this.proposal));
                }
            }
        }

        void OnDeclined()
        {
            OnPropose();
        }

        void OnPropose1()
        {
            this.Goto(typeof(WaitingForEvents));
            Raise(ReceivedEvent as Propose);
        }
        #endregion
    }
}
