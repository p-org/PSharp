using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class ValidityCheck : Monitor
    {
        internal class monitor_client_sent : Event
        {
            public int Request;

            public monitor_client_sent(int req)
                : base(-1, -1)
            {
                this.Request = req;
            }
        }

        internal class monitor_proposer_sent : Event
        {
            public int ProposeVal;

            public monitor_proposer_sent(int val)
                : base(-1, -1)
            {
                this.ProposeVal = val;
            }
        }

        internal class monitor_proposer_chosen : Event
        {
            public int ChosenVal;

            public monitor_proposer_chosen(int val)
                : base(-1, -1)
            {
                this.ChosenVal = val;
            }
        }

        Dictionary<int, int> ClientSet;
        Dictionary<int, int> ProposedSet;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.ClientSet = new Dictionary<int, int>();
            this.ProposedSet = new Dictionary<int, int>();
            this.Goto(typeof(Wait));
        }
        
        [OnEventDoAction(typeof(ValidityCheck.monitor_client_sent), nameof(AddClientSet))]
        [OnEventDoAction(typeof(ValidityCheck.monitor_proposer_sent), nameof(AddProposerSet))]
        [OnEventDoAction(typeof(ValidityCheck.monitor_proposer_chosen), nameof(CheckChosenValmachineity))]
        class Wait : MonitorState { }

        void AddClientSet()
        {
            var index = (this.ReceivedEvent as ValidityCheck.monitor_client_sent).Request;
            this.ClientSet.Add(index, 0);
        }

        void AddProposerSet()
        {
            var index = (this.ReceivedEvent as ValidityCheck.monitor_proposer_sent).ProposeVal;
            this.Assert(this.ClientSet.ContainsKey(index), "Client set does not contain {0}", index);

            if (this.ProposedSet.ContainsKey(index))
            {
                this.ProposedSet[index] = 0;
            }
            else
            {
                this.ProposedSet.Add(index, 0);
            }
        }

        void CheckChosenValmachineity()
        {
            var index = (this.ReceivedEvent as ValidityCheck.monitor_proposer_chosen).ChosenVal;
            this.Assert(this.ProposedSet.ContainsKey(index), "Proposed set does not contain {0}", index);
        }
    }
}