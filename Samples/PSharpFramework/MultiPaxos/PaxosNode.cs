using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class PaxosNode : Machine
    {
        Tuple<int, Id> CurrentLeader;
        Id LeaderElectionService;

        List<Id> Acceptors;
        int CommitValue;
        int ProposeVal;
        int Majority;
        int MyRank;
        Tuple<int, int> NextProposal;
        Tuple<int, int, int> ReceivedAgree;
        int MaxRound;
        int AcceptCount;
        int AgreeCount;
        Id Timer;
        int NextSlotForProposer;

        Dictionary<int, Tuple<int, int, int>> AcceptorSlots;

        Dictionary<int, Tuple<int, int, int>> LearnerSlots;
        int LastExecutedSlot;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(PerformOperation))]
        [OnEventDoAction(typeof(allNodes), nameof(UpdateAcceptors))]
        [DeferEvents(typeof(Ping))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Acceptors = new List<Id>();
            this.AcceptorSlots = new Dictionary<int, Tuple<int, int, int>>();
            this.LearnerSlots = new Dictionary<int, Tuple<int, int, int>>();

            this.MyRank = (int)this.Payload;

            this.CurrentLeader = Tuple.Create(this.MyRank, this.Id);
            this.MaxRound = 0;

            this.Timer = this.CreateMachine(typeof(Timer), this.Id, 10);

            this.LastExecutedSlot = -1;
            this.NextSlotForProposer = 0;
        }

        [OnEventPushState(typeof(goPropose), typeof(ProposeValuePhase1))]
        [OnEventPushState(typeof(chosen), typeof(RunLearner))]
        [OnEventDoAction(typeof(update), nameof(CheckIfLeader))]
        [OnEventDoAction(typeof(prepare), nameof(PrepareAction))]
        [OnEventDoAction(typeof(accept), nameof(AcceptAction))]
        [OnEventDoAction(typeof(Ping), nameof(ForwardToLE))]
        [OnEventDoAction(typeof(newLeader), nameof(UpdateLeader))]
        [IgnoreEvents(typeof(agree), typeof(accepted), typeof(timeout), typeof(reject))]
        class PerformOperation : MachineState { }

        [OnEntry(nameof(ProposeValuePhase1OnEntry))]
        [OnEventGotoState(typeof(reject), typeof(ProposeValuePhase1), nameof(ProposeValuePhase1RejectAction))]
        [OnEventGotoState(typeof(success), typeof(ProposeValuePhase2), nameof(ProposeValuePhase1SuccessAction))]
        [OnEventGotoState(typeof(timeout), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(agree), nameof(CountAgree))]
        [IgnoreEvents(typeof(accepted))]
        class ProposeValuePhase1 : MachineState { }

        void ProposeValuePhase1OnEntry()
        {
            this.AgreeCount = 0;
            this.NextProposal = this.GetNextProposal(this.MaxRound);
            this.ReceivedAgree = Tuple.Create(-1, -1, -1);

            foreach (var acceptor in this.Acceptors)
            {
                this.Send(acceptor, new prepare(), this.Id, this.NextSlotForProposer, this.NextProposal.Item1, this.NextProposal.Item2, this.MyRank);
            }

            this.Monitor<ValidityCheck>(new monitor_proposer_sent(), this.ProposeVal);
            this.Send(this.Timer, new startTimer());
        }

        void ProposeValuePhase1RejectAction()
        {
            var round = (int)(this.Payload as object[])[0];

            if (this.NextProposal.Item1 <= round)
            {
                this.MaxRound = round;
            }

            this.Send(this.Timer, new cancelTimer());
        }

        void ProposeValuePhase1SuccessAction()
        {
            this.Send(this.Timer, new cancelTimer());
        }

        [OnEntry(nameof(ProposeValuePhase2OnEntry))]
        [OnExit(nameof(ProposeValuePhase2OnExit))]
        [OnEventGotoState(typeof(reject), typeof(ProposeValuePhase1), nameof(ProposeValuePhase2RejectAction))]
        [OnEventGotoState(typeof(timeout), typeof(ProposeValuePhase1))]
        [OnEventDoAction(typeof(accepted), nameof(CountAccepted))]
        [IgnoreEvents(typeof(agree))]
        class ProposeValuePhase2 : MachineState { }

        void ProposeValuePhase2OnEntry()
        {
            this.AcceptCount = 0;
            this.ProposeVal = this.GetHighestProposedValue();

            this.Monitor<BasicPaxosInvariant_P2b>(new monitor_valueProposed(), this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal);
            this.Monitor<ValidityCheck>(new monitor_proposer_sent(), this.ProposeVal);

            foreach (var acceptor in this.Acceptors)
            {
                this.Send(acceptor, new accept(), this.Id, this.NextSlotForProposer, this.NextProposal.Item1, this.NextProposal.Item2, this.ProposeVal);
            }

            this.Send(this.Timer, new startTimer());
        }

        void ProposeValuePhase2OnExit()
        {
            if (this.Trigger == typeof(chosen))
            {
                this.Monitor<BasicPaxosInvariant_P2b>(new monitor_valueChosen(), this.Id, this.NextSlotForProposer, this.NextProposal, this.ProposeVal);

                this.Send(this.Timer, new cancelTimer());

                this.Monitor<ValidityCheck>(new monitor_proposer_chosen(), this.ProposeVal);

                this.NextSlotForProposer++;
            }
        }

        void ProposeValuePhase2RejectAction()
        {
            var round = (int)(this.Payload as object[])[0];

            if (this.NextProposal.Item1 <= round)
            {
                this.MaxRound = round;
            }

            this.Send(this.Timer, new cancelTimer());
        }

        [OnEntry(nameof(RunLearnerOnEntry))]
        [IgnoreEvents(typeof(agree), typeof(accepted), typeof(timeout), typeof(prepare), typeof(reject), typeof(accept))]
        [DeferEvents(typeof(newLeader))]
        class RunLearner : MachineState { }

        void RunLearnerOnEntry()
        {
            var slot = (int)(this.Payload as object[])[0];
            var round = (int)(this.Payload as object[])[1];
            var server = (int)(this.Payload as object[])[2];
            var value = (int)(this.Payload as object[])[3];

            this.LearnerSlots[slot] = Tuple.Create(round, server, value);

            if (this.CommitValue == value)
            {
                this.Pop();
            }
            else
            {
                this.ProposeVal = this.CommitValue;
                this.Raise(new goPropose());
            }
        }

        void UpdateAcceptors()
        {
            var acceptors = this.Payload as List<Id>;

            this.Acceptors = acceptors;

            this.Majority = this.Acceptors.Count / 2 + 1;
            this.Assert(this.Majority == 2, "Majority is not 2");

            this.LeaderElectionService = this.CreateMachine(typeof(LeaderElection), this.Acceptors, this.Id, this.MyRank);

            this.Raise(new local());
        }
        
        void CheckIfLeader()
        {
            if (this.CurrentLeader.Item1 == this.MyRank)
            {
                this.CommitValue = (int)(this.Payload as object[])[1];
                this.ProposeVal = this.CommitValue;
                this.Raise(new goPropose());
            }
            else
            {
                this.Send(this.CurrentLeader.Item2, new update(), this.Payload);
            }
        }
        
        void PrepareAction()
        {
            var proposer = (this.Payload as object[])[0] as Id;
            var slot = (int)(this.Payload as object[])[1];
            var round = (int)(this.Payload as object[])[2];
            var server = (int)(this.Payload as object[])[3];

            if (!this.AcceptorSlots.ContainsKey(slot))
            {
                this.Send(proposer, new agree(), slot, -1, -1, -1);
                return;
            }

            if (this.LessThan(round, server, this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2))
            {
                this.Send(proposer, new reject(), Tuple.Create(slot, this.AcceptorSlots[slot].Item1,
                    this.AcceptorSlots[slot].Item2));
            }
            else
            {
                this.Send(proposer, new agree(), slot, this.AcceptorSlots[slot].Item1,
                    this.AcceptorSlots[slot].Item2, this.AcceptorSlots[slot].Item3);
                this.AcceptorSlots[slot] = Tuple.Create(this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2, -1);
            }
        }

        void AcceptAction()
        {
            var proposer = (this.Payload as object[])[0] as Id;
            var slot = (int)(this.Payload as object[])[1];
            var round = (int)(this.Payload as object[])[2];
            var server = (int)(this.Payload as object[])[3];
            var value = (int)(this.Payload as object[])[4];

            if (this.AcceptorSlots.ContainsKey(slot))
            {
                if (!this.IsEqual(round, server, this.AcceptorSlots[slot].Item1, this.AcceptorSlots[slot].Item2))
                {
                    this.Send(proposer, new reject(), Tuple.Create(slot, this.AcceptorSlots[slot].Item1,
                        this.AcceptorSlots[slot].Item2));
                }
                else
                {
                    this.AcceptorSlots[slot] = Tuple.Create(round, server, value);
                    this.Send(proposer, new accepted(), slot, round, server, value);
                }
            }
        }

        void ForwardToLE()
        {
            this.Send(this.LeaderElectionService, new Ping(), this.Payload);
        }

        void UpdateLeader()
        {
            this.CurrentLeader = this.Payload as Tuple<int, Id>;
        }

        void CountAgree()
        {
            var slot = (int)(this.Payload as object[])[0];
            var round = (int)(this.Payload as object[])[1];
            var server = (int)(this.Payload as object[])[2];
            var value = (int)(this.Payload as object[])[3];

            if (slot == this.NextSlotForProposer)
            {
                this.AgreeCount++;
                if (this.LessThan(this.ReceivedAgree.Item1, this.ReceivedAgree.Item2, round, server))
                {
                    this.ReceivedAgree = Tuple.Create(round, server, value);
                }

                if (this.AgreeCount == this.Majority)
                {
                    this.Raise(new success());
                }
            }
        }

        void CountAccepted()
        {
            var slot = (int)(this.Payload as object[])[0];
            var round = (int)(this.Payload as object[])[1];
            var server = (int)(this.Payload as object[])[2];

            if (slot == this.NextSlotForProposer)
            {
                if (this.IsEqual(round, server, this.NextProposal.Item1, this.NextProposal.Item2))
                {
                    this.AcceptCount++;
                }

                if (this.AcceptCount == this.Majority)
                {
                    this.Raise(new chosen(), this.Payload);
                }
            }
        }

        void RunReplicatedMachine()
        {
            while (true)
            {
                if (this.LearnerSlots.ContainsKey(this.LastExecutedSlot + 1))
                {
                    this.LastExecutedSlot++;
                }
                else
                {
                    return;
                }
            }
        }

        int GetHighestProposedValue()
        {
            if (this.ReceivedAgree.Item2 != -1)
            {
                return this.ReceivedAgree.Item2;
            }
            else
            {
                return this.CommitValue;
            }
        }

        Tuple<int, int> GetNextProposal(int maxRound)
        {
            return Tuple.Create(maxRound + 1, this.MyRank);
        }

        bool IsEqual(int round1, int server1, int round2, int server2)
        {
            if (round1 == round2 && server1 == server2)
            {
                return true;
            }
            else
            {
                return false;
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
