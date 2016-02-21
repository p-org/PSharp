using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPaxosRacy
{
    /// <summary>
    /// The monitor checks the following property:
    /// 
    /// If the chosen proposal has value v, then every higher numbered
    /// proposal issued by any proposer has value v.
    /// </summary>
    class PaxosInvariantMonitor : Machine
    {
        #region events
        private class eLocal : Event { }

        private class eMonVal : Event { }

        public class eMonitorValueProposed : Event
        {
            public Tuple<GodMachine.Proposal, int> LastValueChosen;

            public eMonitorValueProposed(Tuple<GodMachine.Proposal, int> LastValueChosen)
            {
                this.LastValueChosen = LastValueChosen;
            }
        }

        public class eMonitorValueChosen : Event
        {
            public Tuple<GodMachine.Proposal, int> LastValueChosen;

            public eMonitorValueChosen(Tuple<GodMachine.Proposal, int> LastValueChosen)
            {
                this.LastValueChosen = LastValueChosen;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private Tuple<GodMachine.Proposal, int> LastValueChosen;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForValueChosen))]
        private class Init : MachineState { }

        [IgnoreEvents(typeof(eMonitorValueProposed))]
        [OnEventDoAction(typeof(eMonitorValueChosen), nameof(OnMonitorValueChosen))]
        [OnEventGotoState(typeof(eMonVal), typeof(CheckValueProposed))]
        private class WaitForValueChosen : MachineState { }

        [OnEntry(nameof(OnCheckValueProposedEntry))]
        [OnEventDoAction(typeof(eMonitorValueChosen), nameof(OnMonitorValueChosenCheck))]
        [OnEventDoAction(typeof(eMonitorValueProposed), nameof(OnMonitorValueProposed))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class CheckValueProposed : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[Monitor] Initializing ...\n");
            this.Raise(new eLocal());
        }

        private void OnCheckValueProposedEntry()
        {
            Console.WriteLine("[Monitor] CheckValueProposed ...\n");
        }

        private void Stop()
        {
            Console.WriteLine("[Monitor] Stopping ...\n");
            Raise(new Halt());
        }

        private void OnMonitorValueChosen()
        {
            this.LastValueChosen = (this.ReceivedEvent as eMonitorValueChosen).LastValueChosen;
            Console.WriteLine("[Monitor] LastValueChosen: {0}, {1}, {2}\n",
                this.LastValueChosen.Item1.Round, this.LastValueChosen.Item1.ServerId,
                this.LastValueChosen.Item2);
            Raise(new eMonVal());
        }

        private void OnMonitorValueChosenCheck()
        {
            var receivedValue = (this.ReceivedEvent as eMonitorValueChosen).LastValueChosen;
            Console.WriteLine("[Monitor] ReceivedValue: {0}, {1}, {2}\n",
                    receivedValue.Item1.Round, receivedValue.Item1.ServerId,
                    receivedValue.Item2);
            this.Assert(this.LastValueChosen.Item2 == receivedValue.Item2,
                "this.LastValueChosen {0} == receivedValue {1}",
                this.LastValueChosen.Item2, receivedValue.Item2);
        }

        private void OnMonitorValueProposed()
        {
            Console.WriteLine("[Monitor] eMonitorValueProposed ...\n");

            var receivedValue = (this.ReceivedEvent as eMonitorValueProposed).LastValueChosen;

            if (this.IsProposalLessThan(this.LastValueChosen.Item1, receivedValue.Item1))
            {
                this.Assert(this.LastValueChosen.Item2 == receivedValue.Item2,
                    "this.LastValueChosen {0} == receivedValue {1}",
                    this.LastValueChosen.Item2, receivedValue.Item2);
            }
        }

        private bool IsProposalLessThan(GodMachine.Proposal p1, GodMachine.Proposal p2)
        {
            if (p1.Round < p2.Round)
            {
                return true;
            }
            else if (p1.Round == p2.Round)
            {
                if (p1.ServerId < p2.ServerId)
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
        #endregion
    }
}

