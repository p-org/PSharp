using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPaxosRacy
{
    class Acceptor : Machine
    {
        #region events
        private class eLocal : Event { }

        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public int Id;

            public eInitialize(int Id)
            {
                this.Id = Id;
            }
        }

        public class ePrepare : Event
        {
            public Tuple<MachineId, GodMachine.Proposal, int> prepareProposal;

            public ePrepare(Tuple<MachineId, GodMachine.Proposal, int> prepareProposal)
            {
                this.prepareProposal = prepareProposal;
            }
        }

        public class eAccept : Event
        {
            public Tuple<MachineId, GodMachine.Proposal, int> acceptProposal;

            public eAccept(Tuple<MachineId, GodMachine.Proposal, int> acceptProposal)
            {
                this.acceptProposal = acceptProposal;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private int Identity;

        private GodMachine.Proposal LastSeenProposal;
        private int LastSeenProposalValue;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(Wait))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnWait))]
        [OnEventDoAction(typeof(ePrepare), nameof(Prepare))]
        [OnEventDoAction(typeof(eAccept), nameof(Accept))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class Wait : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[Acceptor-{0}] Initializing ...\n", this.Identity);
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            this.Identity = (this.ReceivedEvent as eInitialize).Id;

            this.LastSeenProposal = new GodMachine.Proposal(-1, -1);
            this.LastSeenProposalValue = -1;

            this.Raise(new eLocal());
        }

        private void OnWait()
        {
            Console.WriteLine("[Acceptor-{0}] Waiting ...\n", this.Identity);
        }

        private void Prepare()
        {
            var receivedMessage = (this.ReceivedEvent as ePrepare).prepareProposal;

            Console.WriteLine("{0}-{1} Preparing: {2}, {3}, {4}\n",
                    this, this.Identity, receivedMessage.Item2.Round, receivedMessage.Item2.ServerId,
                    receivedMessage.Item3);

            if (this.LastSeenProposalValue == -1)
            {
                Console.WriteLine("{0}-{1} Sending Agree: 0, 0, -1\n", this, this.Identity);
                this.Send(receivedMessage.Item1, new Proposer.eAgree(
                    new Tuple<GodMachine.Proposal, int>(new GodMachine.Proposal(0, 0), -1)));

                this.LastSeenProposal = receivedMessage.Item2;
                this.LastSeenProposalValue = receivedMessage.Item3;
            }
            else if (this.IsProposalLessThan(receivedMessage.Item2, this.LastSeenProposal))
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Identity, typeof(Proposer.eReject), receivedMessage.Item1);
                this.Send(receivedMessage.Item1, new Proposer.eReject(new GodMachine.Proposal(
                    this.LastSeenProposal.Round, this.LastSeenProposal.ServerId)));
            }
            else
            {
                Console.WriteLine("{0}-{1} Sending Agree: {2}, {3}, {4}\n", this, this.Identity,
                    this.LastSeenProposal.Round, this.LastSeenProposal.ServerId, this.LastSeenProposalValue);
                this.Send(receivedMessage.Item1, new Proposer.eAgree(new Tuple<GodMachine.Proposal, int>(
                    new GodMachine.Proposal(this.LastSeenProposal.Round, this.LastSeenProposal.ServerId),
                    this.LastSeenProposalValue)));

                this.LastSeenProposal = receivedMessage.Item2;
                this.LastSeenProposalValue = receivedMessage.Item3;
            }
        }

        private void Accept()
        {
            var receivedMessage = (this.ReceivedEvent as eAccept).acceptProposal;

            Console.WriteLine("{0}-{1} Accepting: {2}, {3}, {4}\n",
                    this, this.Identity, receivedMessage.Item2.Round, receivedMessage.Item2.ServerId,
                    receivedMessage.Item3);

            if (!this.AreProposalsEqual(receivedMessage.Item2, this.LastSeenProposal))
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Identity, typeof(Proposer.eReject), receivedMessage.Item1);
                this.Send(receivedMessage.Item1, new Proposer.eReject(
                    new GodMachine.Proposal(this.LastSeenProposal.Round, this.LastSeenProposal.ServerId)));
            }
            else
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Identity, typeof(Proposer.eAccepted), receivedMessage.Item1);
                this.Send(receivedMessage.Item1, new Proposer.eAccepted(
                    new Tuple<GodMachine.Proposal, int>(receivedMessage.Item2, receivedMessage.Item3)));
            }
        }


        private void Stop()
        {
            Console.WriteLine("[Acceptor-{0}] Initializing ...\n", this.Identity);
            this.Raise(new Halt());
        }
        #endregion

        #region helper methods
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


