using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class ValidityCheck : Monitor
    {
        Dictionary<int, int> ClientSet;
        Dictionary<int, int> ProposedSet;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(Wait))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.ClientSet = new Dictionary<int, int>();
            this.ProposedSet = new Dictionary<int, int>();
            this.Raise(new local());
        }
        
        [OnEventDoAction(typeof(monitor_client_sent), nameof(AddClientSet))]
        [OnEventDoAction(typeof(monitor_proposer_sent), nameof(AddProposerSet))]
        [OnEventDoAction(typeof(monitor_proposer_chosen), nameof(CheckChosenValmachineity))]
        class Wait : MonitorState { }

        void AddClientSet()
        {
            var index = (int)this.Payload;
            this.ClientSet.Add(index, 0);
        }

        void AddProposerSet()
        {
            var index = (int)this.Payload;
            this.Assert(this.ClientSet.ContainsKey(index));
            this.ProposedSet.Add(index, 0);
        }

        void CheckChosenValmachineity()
        {
            var index = (int)this.Payload;
            this.Assert(this.ProposedSet.ContainsKey(index));
        }

        int override GetHashedState()
        {

        }
    }
}