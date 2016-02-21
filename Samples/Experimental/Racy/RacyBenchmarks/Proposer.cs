using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPaxosRacy
{
    class Proposer : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        private class eSuccess : Event { }

        private class eGotoPhase2 : Event { }

        private class eGotoPhase1 : Event { }

        private class eGotoDone : Event { }

        public class eInitialize : Event
        {
            public Tuple<MachineId, List<MachineId>, List<MachineId>, int, int> initPayload;

            public eInitialize(Tuple<MachineId, List<MachineId>, List<MachineId>, int, int> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        public class eAccepted : Event
        {
            public Tuple<GodMachine.Proposal, int> acceptedProposal;

            public eAccepted(Tuple<GodMachine.Proposal, int> acceptedProposal)
            {
                this.acceptedProposal = acceptedProposal;
            }
        }


        public class eAgree : Event
        {
            public Tuple<GodMachine.Proposal, int> agreeProposal;

            public eAgree(Tuple<GodMachine.Proposal, int> agreeProposal)
            {
                this.agreeProposal = agreeProposal;
            }
        }


        public class eReject : Event
        {
            public GodMachine.Proposal rejectProposal;

            public eReject(GodMachine.Proposal rejectProposal)
            {
                this.rejectProposal = rejectProposal;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private MachineId PaxosInvariantMonitor;

        private List<MachineId> Proposers;
        private List<MachineId> Acceptors;
        private MachineId Timer;

        private GodMachine.Proposal NextProposal;
        private List<Tuple<GodMachine.Proposal, int>> ReceivedAgreeList;

        private int ProposeVal;
        private int Majority;
        private int Identity;
        private int MaxRound;
        private int CountAccept;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(ProposeValuePhase1))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnProposePhaseValue1Entry))]
        [IgnoreEvents(typeof(eAccepted))]
        [OnEventDoAction(typeof(eReject), nameof(OnReject))]
        [OnEventDoAction(typeof(eSuccess), nameof(OnSuccess))]
        [OnEventGotoState(typeof(eGotoPhase2), typeof(ProposeValuePhase2))]
        [OnEventGotoState(typeof(Timer.eTimeout), typeof(ProposeValuePhase1))]
        [OnEventGotoState(typeof(eStop), typeof(Done))]
        [OnEventDoAction(typeof(eAgree), nameof(CheckCountAgree))]
        private class ProposeValuePhase1 : MachineState { }

        [OnEntry(nameof(OnProposeValuePhase2Entry))]
        [IgnoreEvents(typeof(eAgree))]
        [OnEventDoAction(typeof(eReject), nameof(OnReject2))]
        [OnEventGotoState(typeof(eGotoPhase1), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(eAccepted), nameof(CheckCountAccepted))]
        [OnEventDoAction(typeof(eSuccess), nameof(OnSuccess2))]
        [OnEventGotoState(typeof(eGotoDone), typeof(Done))]
        [OnEventDoAction(typeof(Timer.eTimeout), nameof(OnTimeout))]
        [OnEventGotoState(typeof(eStop), typeof(Done))]
        private class ProposeValuePhase2 : MachineState { }

        [OnEntry(nameof(OnDoneEntry))]
        [IgnoreEvents(typeof(eReject), typeof(eAgree), 
            typeof(Timer.eTimeout), typeof(eAccepted))]
        private class Done : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[Proposer-{0}] Initializing ...\n", this.Id);
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            this.PaxosInvariantMonitor = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            this.Proposers = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            this.Acceptors = (this.ReceivedEvent as eInitialize).initPayload.Item3;
            this.Identity = (this.ReceivedEvent as eInitialize).initPayload.Item4;
            this.ProposeVal = (this.ReceivedEvent as eInitialize).initPayload.Item5;

            Console.WriteLine("[Proposer-{0}] Initializing ...\n", this.Identity);

            this.MaxRound = 0;
            this.Timer = CreateMachine(typeof(Timer));
            Send(this.Timer, new Timer.eInitialize(this.Id));

            this.Majority = (this.Acceptors.Count / 2) + 1;
            this.Assert(this.Majority == 2, "Machine majority {0} " +
                "is not equal to 2.\n", this.Majority);

            this.ReceivedAgreeList = new List<Tuple<GodMachine.Proposal, int>>();

            this.Raise(new eLocal());
        }

        private void OnProposePhaseValue1Entry()
        {
            this.NextProposal = GetNextProposal(this.MaxRound);

            Console.WriteLine("[Proposer-{0}] Propose 1: round {1}, value {2}\n",
                this.Id, this.NextProposal.Round, this.ProposeVal);

            this.BroadcastAcceptors(typeof(Acceptor.ePrepare), new Tuple<MachineId, GodMachine.Proposal, int>(
                this.Id, this.NextProposal, this.ProposeVal));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(Timer.eStartTimer), this.Timer);
            this.Send(this.Timer, new Timer.eStartTimer());
            this.NextProposal.ServerId = 0;
        }

        private void OnProposeValuePhase2Entry()
        {
            this.CountAccept = 0;
            this.ProposeVal = this.GetHighestProposedValue();

            Console.WriteLine("[Proposer-{0}] Propose 2: round {1}, value {2}\n",
                this.Id, this.NextProposal.Round, this.ProposeVal);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Identity,
                    typeof(PaxosInvariantMonitor.eMonitorValueProposed), typeof(PaxosInvariantMonitor));
            this.Send(this.PaxosInvariantMonitor, new PaxosInvariantMonitor.eMonitorValueProposed(
                new Tuple<GodMachine.Proposal, int>(new GodMachine.Proposal(
                    this.NextProposal.Round, this.NextProposal.ServerId),
                    this.ProposeVal)));

            this.BroadcastAcceptors(typeof(Acceptor.eAccept), new Tuple<MachineId, GodMachine.Proposal, int>(
                this.Id, this.NextProposal, this.ProposeVal));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Identity, typeof(Timer.eStartTimer), this.Timer);
            this.Send(this.Timer, new Timer.eStartTimer());
        }

        private void OnDoneEntry()
        {
            Console.WriteLine("[Proposer-{0}] Stopping ...\n", this.Identity);

            foreach (var acceptor in this.Acceptors)
            {
                this.Send(acceptor, new Acceptor.eStop());
            }

            foreach (var proposer in this.Proposers)
            {
                if (!proposer.Equals(this))
                {
                    this.Send(proposer, new Proposer.eStop());
                }
            }

            this.Send(this.Timer, new Timer.eStop());
            this.Send(this.PaxosInvariantMonitor, new PaxosInvariantMonitor.eStop());

            Raise(new Halt());
        }

        private void CheckCountAgree()
        {
            Console.WriteLine("[Proposer-{0}] CheckCountAgree ...\n", this.Id);

            this.ReceivedAgreeList.Add((this.ReceivedEvent as eAgree).agreeProposal);

            if (this.ReceivedAgreeList.Count == this.Majority)
            {
                this.Raise(new eSuccess());
            }
        }

        private void CheckCountAccepted()
        {
            Console.WriteLine("[Proposer-{0}] CheckCountAccepted ...\n", this.Id);

            if (this.AreProposalsEqual((this.ReceivedEvent as eAccepted).acceptedProposal.Item1,
                this.NextProposal))
            {
                this.CountAccept++;
            }

            if (this.CountAccept == this.Majority)
            {
                this.Raise(new eSuccess());
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Proposer-{0}] Stopping ...\n", this.Id);
            Raise(new Halt());
        }

        private void OnReject()
        {
            Console.WriteLine("[Proposer-{0}] ProposeValuePhase1 (REJECT) ...\n", this.Identity);

            int round = (this.ReceivedEvent as eReject).rejectProposal.Round;

            if (this.NextProposal.Round <= round)
            {
                this.MaxRound = round;
            }

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(Timer.eCancelTimer), this.Timer);
            this.Send(this.Timer, new Timer.eCancelTimer());
        }

        private void OnSuccess()
        {
            Console.WriteLine("[Proposer-{0}] ProposeValuePhase1 (SUCCESS) ...\n", this.Id);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Identity, typeof(Timer.eCancelTimer), this.Timer);
            this.Send(this.Timer, new Timer.eCancelTimer());

            Raise(new eGotoPhase2());
        }

        private void OnSuccess2()
        {
            Console.WriteLine("[Proposer-{0}] ProposeValuePhase2 (SUCCESS) ...\n", this.Identity);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Id,
                    typeof(PaxosInvariantMonitor.eMonitorValueChosen), typeof(PaxosInvariantMonitor));
            this.Send(this.PaxosInvariantMonitor, new PaxosInvariantMonitor.eMonitorValueChosen(
                new Tuple<GodMachine.Proposal, int>(this.NextProposal, this.ProposeVal)));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(Timer.eCancelTimer), this.Timer);
            this.Send(this.Timer, new Timer.eCancelTimer());

            Raise(new eGotoDone());
        }

        private void OnReject2()
        {
            Console.WriteLine("[Proposer-{0}] ProposeValuePhase2 (REJECT) ...\n", this.Identity);

            int round = (this.ReceivedEvent as eReject).rejectProposal.Round;

            if (this.NextProposal.Round <= round)
            {
                this.MaxRound = round;
            }

            this.ReceivedAgreeList.Clear();

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(Timer.eCancelTimer), this.Timer);
            this.Send(this.Timer, new Timer.eCancelTimer());

            Raise(new eGotoPhase1());
        }

        private void OnTimeout()
        {
            this.ReceivedAgreeList.Clear();
            Raise(new eGotoPhase1());
        }
        #endregion

        #region helper methods
        private GodMachine.Proposal GetNextProposal(int maxRound)
        {
            if (MaxRound >= 2)
                this.Raise(new Halt());
            return new GodMachine.Proposal(maxRound + 1, this.Identity);
        }


        private void BroadcastAcceptors(Type e, Tuple<MachineId, GodMachine.Proposal, int> pay)
        {
            for (int i = 0; i < this.Acceptors.Count; i++)
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, e, this.Acceptors[i]);
                this.Send(this.Acceptors[i], Activator.CreateInstance(e, pay) as Event);
            }
        }
        private int GetHighestProposedValue()
        {
            GodMachine.Proposal tempProposal = new GodMachine.Proposal(-1, 0);
            int tempVal = -1;

            foreach (var receivedAgree in this.ReceivedAgreeList)
            {
                if (this.IsProposalLessThan(tempProposal, receivedAgree.Item1))
                {
                    tempProposal = receivedAgree.Item1;
                    tempVal = receivedAgree.Item2;
                }
            }

            if (tempVal != -1)
            {
                return tempVal;
            }
            else
            {
                return ProposeVal;
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

        private bool AreProposalsEqual(GodMachine.Proposal p1, GodMachine.Proposal p2)
        {
            if (p1.Round == p2.Round && p1.ServerId == p2.ServerId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}

