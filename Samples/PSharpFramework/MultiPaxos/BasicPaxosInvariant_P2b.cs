using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class BasicPaxosInvariant_P2b : Monitor
    {
        Dictionary<int, Tuple<int, int, int>> LastValueChosen;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(WaitForValueChosen))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.LastValueChosen = new Dictionary<int, Tuple<int, int, int>>();
            this.Raise(new local());
        }

        [OnEventGotoState(typeof(monitor_valueChosen), typeof(CheckValueProposed), nameof(WaitForValueChosenAction))]
        [IgnoreEvents(typeof(monitor_valueProposed))]
        class WaitForValueChosen : MonitorState { }

        void WaitForValueChosenAction()
        {
            var slot = (int)(this.Payload as object[])[0];
            var proposal = (this.Payload as object[])[1] as Tuple<int, int, int>;
            this.LastValueChosen.Add(slot, proposal);
        }

        [OnEventGotoState(typeof(monitor_valueChosen), typeof(CheckValueProposed), nameof(ValueChosenAction))]
        [OnEventGotoState(typeof(monitor_valueProposed), typeof(CheckValueProposed), nameof(ValueProposedAction))]
        class CheckValueProposed : MonitorState { }

        void ValueChosenAction()
        {
            var slot = (int)(this.Payload as object[])[0];
            var proposal = (this.Payload as object[])[1] as Tuple<int, int, int>;

            this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueChosenAction");
        }

        void ValueProposedAction()
        {
            var slot = (int)(this.Payload as object[])[0];
            var proposal = (this.Payload as object[])[1] as Tuple<int, int, int>;

            if (this.LessThan(this.LastValueChosen[slot].Item1, this.LastValueChosen[slot].Item2,
                proposal.Item1, proposal.Item2))
            {
                this.Assert(this.LastValueChosen[slot].Item3 == proposal.Item3, "ValueProposedAction");
            }
        }

        bool LessThan(int round1, int server1, int round2, int server2)
        {
            if (round1 < round2)
            {
                return true;
            }
            else if (round1 == round2)
            {
                if (server1 < server2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}