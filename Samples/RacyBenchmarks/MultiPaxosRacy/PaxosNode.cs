using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MultiPaxosRacy
{
    class PaxosNode : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<int, MachineId, MachineId> initPayload;

            public eInitialize(Tuple<int, MachineId, MachineId> initPayload)
            {
                this.initPayload = initPayload;
            }
        }
        #endregion

        #region struct
        public struct Proposal
        {
            public int Round;
            public int ServerId;

            public Proposal(int round, int serverId)
            {
                this.Round = round;
                this.ServerId = serverId;
            }
        }

        public struct Leader
        {
            public int Rank;
            public Machine Server;

            public Leader(int rank, Machine server)
            {
                this.Rank = rank;
                this.Server = server;
            }
        }
        #endregion

        #region fields
        private Leader CurrentLeader;
        private MachineId LeaderElectionService;

        private MachineId PaxosMonitor;
        private MachineId ValidityMonitor;

        // Proposer fields
        private List<MachineId> Acceptors;
        private MachineId Timer;
        private Proposal NextProposal;

        private Tuple<Proposal, int> ReceivedAgree;

        private int Rank;
        private int ProposeValue;
        private int Majority;
        private int MaxRound;
        private int CountAgree;
        private int CountAccept;

        // Acceptor fields
        private Tuple<Proposal, int> LastSeenProposal;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [DeferEvents(typeof(ePing))]
        private class WaitingForInit : MachineState { }

        [IgnoreEvents(typeof(eAgree))]
        private class PerformOperation : MachineState { }

        [OnEntry(nameof(OnProposeValuePhase1Entry))]
        [IgnoreEvents(typeof(eAccepted))]
        private class ProposeValuePhase1 : MachineState { }

        [OnEntry(nameof(OnProposeValuePhase2Entry))]
        [IgnoreEvents(typeof(eAgree))]
        private class ProposeValuePhase2 : MachineState { }

        [OnEntry(nameof(OnDoneProposalEntry))]
        private class DoneProposal : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Rank = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            PaxosMonitor = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            ValidityMonitor = ((this.ReceivedEvent as eInitialize).initPayload.Item3;

            Console.WriteLine("[PaxosNode-{0}] Initializing ...\n", Rank);

            LastSeenProposal = new Tuple<Proposal, int>(new Proposal(-1, -1), -1);
            MaxRound = 0;

            Timer = CreateMachine(typeof(Timer));
            Send(Timer, new Timer.eInitialize(new Tuple<MachineId, int>(Id, 10)));
        }

        private void OnProposeValuePhase1Entry()
        {
            Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase1 ...\n", Rank);

            CountAgree = 0;
            NextProposal = GetNextProposal(MaxRound);
            ReceivedAgree = new Tuple<Proposal, int>(new Proposal(-1, -1), -1);

            BroadcastAcceptors(typeof(ePrepare), new Tuple<MachineId, Proposal>(Id, NextProposal));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Rank,
                    typeof(ValidityCheckMonitor.eMonitorProposerSent), typeof(ValidityCheckMonitor));
            this.Send(ValidityMonitor, new ValidityCheckMonitor.eMonitorProposerSent(ProposeValue));

            NextProposal.Round = NextProposal.Round + 1;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, Rank, typeof(Timer.eStartTimer), Timer);
            this.Send(Timer, new Timer.eStartTimer());
        }

        private void OnProposeValuePhase2Entry()
        {
            Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase2 (entry) ...\n", machine.Rank);

            CountAccept = 0;
            ProposeValue = GetHighestProposedValue();

            var proposal = NextProposal;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Rank,
                    typeof(PaxosInvariantMonitor.eMonitorValueProposed), typeof(PaxosInvariantMonitor));
            this.Send(PaxosMonitor, new PaxosInvariantMonitor.eMonitorValueProposed(new Tuple<Proposal, int>(
                NextProposal, ProposeValue)));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Rank,
                    typeof(ValidityCheckMonitor.eMonitorProposerSent), typeof(ValidityCheckMonitor));
            this.Send(ValidityMonitor, new ValidityCheckMonitor.eMonitorProposerSent(ProposeValue));

            BroadcastAcceptors(typeof(eAccept), new Tuple<MachineId, Proposal, int>(
                Id, NextProposal, ProposeValue));

            proposal.Round = proposal.Round + 1;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, Rank, typeof(Timer.eStartTimer), Timer);
            this.Send(Timer, new Timer.eStartTimer());
        }

        private void OnDoneProposalEntry()
        {
            Console.WriteLine("[PaxosNode-{0}] DoneProposal ...\n", Rank);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Rank,
                typeof(ValidityCheckMonitor.eMonitorProposerChosen), typeof(ValidityCheckMonitor));
            this.Send(ValidityMonitor, new ValidityCheckMonitor.eMonitorProposerChosen(ProposeValue));

            this.Raise(new eChosen(ProposeValue));
        }
        #endregion
    }
}

internal class PaxosNode : Machine
{
    private class RunLearner : State
    {
        protected override void OnEntry()
        {
            var machine = this.Machine as PaxosNode;

            Console.WriteLine("[PaxosNode-{0}] RunLearner ...\n", machine.Rank);

            machine.DoGlobalStop();
        }

        protected override HashSet<Type> DefineIgnoredEvents()
        {
            return new HashSet<Type>
                {
                    typeof(eAgree),
                    typeof(eAccepted),
                    typeof(eTimeout),
                    typeof(ePrepare),
                    typeof(eReject),
                    typeof(eAccept)
                };
        }

        protected override HashSet<Type> DefineDeferredEvents()
        {
            return new HashSet<Type>
                {
                    typeof(eNewLeader)
                };
        }
    }

    private void UpdateAcceptors()
    {
        this.Acceptors = (List<Machine>)this.Payload;

        this.Majority = (this.Acceptors.Count / 2) + 1;
        Runtime.Assert(this.Majority == 2, "Machine majority {0} " +
            "is not equal to 2.\n", this.Majority);

        this.LeaderElectionService = Machine.Factory.CreateMachine<LeaderElection>(
            new Tuple<List<Machine>, Machine, int>(this.Acceptors, this, this.Rank));

        this.Raise(new eLocal());
    }

    private void CheckIfLeader()
    {
        if (this.CurrentLeader.Rank == this.Rank)
        {
            this.ProposeValue = ((Tuple<int, int>)this.Payload).Item2;
            this.Raise(new eGoPropose());
        }
        else
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(eUpdate), this.CurrentLeader.Server);
            this.Send(this.CurrentLeader.Server, new eUpdate(this.Payload));
        }
    }

    private void ForwardToLE()
    {
        Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
            typeof(ePing), this.LeaderElectionService);
        this.Send(this.LeaderElectionService, new ePing(this.Payload));
    }

    private void UpdateLeader()
    {
        this.CurrentLeader = (Leader)this.Payload;
    }

    private void Prepare()
    {
        var receivedProposer = ((Tuple<Machine, Proposal>)this.Payload).Item1;
        var receivedProposal = ((Tuple<Machine, Proposal>)this.Payload).Item2;

        Console.WriteLine("{0}-{1} Preparing: round {2}, serverId {3}\n", this,
            this.Rank, receivedProposal.Round, receivedProposal.ServerId);

        if (this.LastSeenProposal.Item2 == -1)
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(eAgree), receivedProposer);
            this.Send(receivedProposer, new eAgree(new Tuple<Proposal, int>(
                new Proposal(-1, -1), -1)));
            this.LastSeenProposal = new Tuple<Proposal, int>(receivedProposal,
                this.LastSeenProposal.Item2);
        }
        else if (this.IsProposalLessThan(receivedProposal, this.LastSeenProposal.Item1))
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(eReject), receivedProposer);
            this.Send(receivedProposer, new eReject(this.LastSeenProposal.Item1));
        }
        else
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(eAgree), receivedProposer);
            this.Send(receivedProposer, new eAgree(this.LastSeenProposal));
            this.LastSeenProposal = new Tuple<Proposal, int>(receivedProposal,
                this.LastSeenProposal.Item2);
        }
    }

    private void Accept()
    {
        var receivedProposer = ((Tuple<Machine, Proposal, int>)this.Payload).Item1;
        var receivedProposal = ((Tuple<Machine, Proposal, int>)this.Payload).Item2;
        var receivedValue = ((Tuple<Machine, Proposal, int>)this.Payload).Item3;

        Console.WriteLine("{0}-{1} Accepting: round {2}, serverId {3}, value {4}\n", this,
            this.Rank, receivedProposal.Round, receivedProposal.ServerId, receivedValue);

        if (!this.AreProposalsEqual(receivedProposal, this.LastSeenProposal.Item1))
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                typeof(eReject), receivedProposer);
            this.Send(receivedProposer, new eReject(this.LastSeenProposal.Item1));
        }
        else
        {
            this.LastSeenProposal = new Tuple<Proposal, int>(receivedProposal, receivedValue);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
            typeof(eAccepted), receivedProposer);
            this.Send(receivedProposer, new eAccepted(new Tuple<Proposal, int>(
                receivedProposal, receivedValue)));
        }
    }

    private void CheckCountAgree()
    {
        Console.WriteLine("[PaxosNode-{0}] CheckCountAgree ...\n", this.Rank);

        var receivedProposal = ((Tuple<Proposal, int>)this.Payload).Item1;
        var receivedValue = ((Tuple<Proposal, int>)this.Payload).Item2;

        this.CountAgree++;

        if (this.IsProposalLessThan(this.ReceivedAgree.Item1, receivedProposal))
        {
            this.ReceivedAgree = new Tuple<Proposal, int>(receivedProposal, receivedValue);
        }

        if (this.CountAgree == this.Majority)
        {
            this.Raise(new eSuccess());
        }
    }

    private void CheckCountAccepted()
    {
        Console.WriteLine("[PaxosNode-{0}] CheckCountAccepted ...\n", this.Rank);

        var receivedProposal = ((Tuple<Proposal, int>)this.Payload).Item1;
        var receivedValue = ((Tuple<Proposal, int>)this.Payload).Item2;

        if (this.AreProposalsEqual(receivedProposal, this.NextProposal))
        {
            this.CountAccept++;
        }

        if (this.CountAccept == this.Majority)
        {
            this.Raise(new eSuccess());
        }
    }

    private void BroadcastAcceptors(Type e, Object pay)
    {
        for (int i = 0; i < this.Acceptors.Count; i++)
        {
            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Rank, e, this.Acceptors[i]);
            this.Send(this.Acceptors[i], Activator.CreateInstance(e, pay) as Event);
        }
    }

    private int GetHighestProposedValue()
    {
        if (this.ReceivedAgree.Item2 != -1)
        {
            return this.ReceivedAgree.Item2;
        }
        else
        {
            return this.ProposeValue;
        }
    }

    private Proposal GetNextProposal(int maxRound)
    {
        return new Proposal(maxRound + 1, this.Rank);
    }

    private bool AreProposalsEqual(Proposal p1, Proposal p2)
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

    private bool IsProposalLessThan(Proposal p1, Proposal p2)
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

    private void DoGlobalStop()
    {
        foreach (var acceptor in this.Acceptors)
        {
            this.Send(acceptor, new eStop());
        }

        this.Send(this.PaxosMonitor, new eStop());
        this.Send(this.ValidityMonitor, new eStop());

        this.Stop();
    }

    private void Stop()
    {
        Console.WriteLine("[PaxosNode-{0}] Stopping ...\n", this.Rank);

        this.Send(this.Timer, new eStop());
        this.Send(this.LeaderElectionService, new eStop());

        this.Delete();
    }

    protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
    {
        Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

        StepStateTransitions initDict = new StepStateTransitions();
        initDict.Add(typeof(eLocal), typeof(PerformOperation));

        StepStateTransitions performOperationDict = new StepStateTransitions();
        performOperationDict.Add(typeof(eGoPropose), typeof(ProposeValuePhase1));

        // Step transitions for ProposeValuePhase1
        StepStateTransitions proposeValuePhase1Dict = new StepStateTransitions();
        proposeValuePhase1Dict.Add(typeof(eReject), typeof(ProposeValuePhase1), () =>
        {
            Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase1 (REJECT) ...\n", this.Rank);

            var round = ((Proposal)this.Payload).Round;

            if (this.NextProposal.Round <= round)
            {
                this.MaxRound = round;
            }

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Rank, typeof(eStartTimer), this.Timer);
            this.Send(this.Timer, new eCancelTimer());
        });

        proposeValuePhase1Dict.Add(typeof(eSuccess), typeof(ProposeValuePhase2), () =>
        {
            Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase1 (SUCCESS) ...\n", this.Rank);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Rank, typeof(eStartTimer), this.Timer);
            this.Send(this.Timer, new eCancelTimer());
        });

        proposeValuePhase1Dict.Add(typeof(eTimeout), typeof(ProposeValuePhase1));

        // Step transitions for ProposeValuePhase2
        StepStateTransitions proposeValuePhase2Dict = new StepStateTransitions();
        proposeValuePhase2Dict.Add(typeof(eReject), typeof(ProposeValuePhase1), () =>
        {
            Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase2 (REJECT) ...\n", this.Rank);

            var round = ((Proposal)this.Payload).Round;

            if (this.NextProposal.Round <= round)
            {
                this.MaxRound = round;
            }

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Rank, typeof(eStartTimer), this.Timer);
            this.Send(this.Timer, new eCancelTimer());
        });

        proposeValuePhase2Dict.Add(typeof(eSuccess), typeof(DoneProposal), () =>
        {
            Console.WriteLine("[PaxosNode-{0}] ProposeValuePhase2 (SUCCESS) ...\n", this.Rank);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Rank,
                    typeof(eMonitorValueChosen), typeof(PaxosInvariantMonitor));
            this.Send(this.PaxosMonitor, new eMonitorValueChosen(new Tuple<Proposal, int>(
                this.NextProposal, this.ProposeValue)));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Rank, typeof(eStartTimer), this.Timer);
            this.Send(this.Timer, new eCancelTimer());
        });

        proposeValuePhase2Dict.Add(typeof(eTimeout), typeof(ProposeValuePhase1));

        StepStateTransitions doneProposalDict = new StepStateTransitions();
        doneProposalDict.Add(typeof(eChosen), typeof(RunLearner));

        dict.Add(typeof(Init), initDict);
        dict.Add(typeof(PerformOperation), performOperationDict);
        dict.Add(typeof(ProposeValuePhase1), proposeValuePhase1Dict);
        dict.Add(typeof(ProposeValuePhase2), proposeValuePhase2Dict);
        dict.Add(typeof(DoneProposal), doneProposalDict);

        return dict;
    }

    protected override Dictionary<Type, ActionBindings> DefineActionBindings()
    {
        Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

        ActionBindings initDict = new ActionBindings();
        initDict.Add(typeof(eAllNodes), new Action(UpdateAcceptors));

        // Action bindings for PerformOperation
        ActionBindings performOperationDict = new ActionBindings();
        // Proposer
        performOperationDict.Add(typeof(eUpdate), new Action(CheckIfLeader));
        // Acceptor
        performOperationDict.Add(typeof(ePrepare), new Action(Prepare));
        performOperationDict.Add(typeof(eAccept), new Action(Accept));
        // Leader Election
        performOperationDict.Add(typeof(ePing), new Action(ForwardToLE));
        performOperationDict.Add(typeof(eNewLeader), new Action(UpdateLeader));
        performOperationDict.Add(typeof(eStop), new Action(Stop));

        // Action bindings for ProposeValuePhase1
        ActionBindings proposeValuePhase1Dict = new ActionBindings();
        proposeValuePhase1Dict.Add(typeof(eAgree), new Action(CheckCountAgree));
        proposeValuePhase1Dict.Add(typeof(ePrepare), new Action(Prepare));

        // Action bindings for ProposeValuePhase2
        ActionBindings proposeValuePhase2Dict = new ActionBindings();
        proposeValuePhase2Dict.Add(typeof(eAccepted), new Action(CheckCountAccepted));
        proposeValuePhase2Dict.Add(typeof(eAccept), new Action(Accept));

        dict.Add(typeof(Init), initDict);
        dict.Add(typeof(PerformOperation), performOperationDict);
        dict.Add(typeof(ProposeValuePhase1), proposeValuePhase1Dict);
        dict.Add(typeof(ProposeValuePhase2), proposeValuePhase2Dict);

        return dict;
    }
}

