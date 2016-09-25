using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos
{
    class Acceptor : Machine
    {
        #region events
        public class Prepare : Event
        {
            public MachineId Sender;
            public int ProposedValue;
            public Prepare(MachineId sender, int proposedValue)
            {
                this.Sender = sender;
                this.ProposedValue = proposedValue;
            }
        }
        public class Accept : Event
        {
            public MachineId Sender;
            public int ProposedValue;
            public Accept(MachineId sender, int proposedValue)
            {
                this.Sender = sender;
                this.ProposedValue = proposedValue;
            }
        }
        #endregion

        #region fields
        int prepared;
        int accepted;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(Prepare), nameof(OnPrepare))]
        [OnEventDoAction(typeof(Accept), nameof(OnAccept))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            this.prepared = 0;
            this.accepted = 0;
        }

        void OnPrepare()
        {
            var e = ReceivedEvent as Prepare;
            if(this.prepared >= e.ProposedValue)
            {
                Send(e.Sender, new Proposer.Declined());
            }
            else
            {
                this.prepared = e.ProposedValue;
                Send(e.Sender, new Proposer.Proposal_OK(this.accepted));
            }
        }

        void OnAccept()
        {
            var e = ReceivedEvent as Accept;
            Console.WriteLine("Accepted: " + this.prepared + "; " + e.ProposedValue);
            if (this.prepared >= e.ProposedValue)
            {
                Send(e.Sender, new Proposer.Declined());
            }
            else
            {
                Console.WriteLine(this.Id.Name + " accepted" + e.ProposedValue);
                this.accepted = e.ProposedValue;
            }
        }
        #endregion
    }
}
