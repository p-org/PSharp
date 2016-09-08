using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderElection
{
    public enum mtype
    {
        one,
        two,
        winner
    }
    public class Node : Machine
    {
        #region events
        public class Initialize : Event
        {
            public int MyNumber;
            public MachineId LeaderCount_MachineId;

            public Initialize(MachineId leaderCount_MachineId, int myNumber)
            {
                this.MyNumber = myNumber;
                this.LeaderCount_MachineId = leaderCount_MachineId;
            }
        }
        public class SetNeighbours : Event
        {
            public MachineId InputMachineId;
            public MachineId OutputMachineId;

            public SetNeighbours(MachineId inputMachineId, MachineId outputMachineId)
            {
                this.InputMachineId = inputMachineId;
                this.OutputMachineId = outputMachineId;
            }
        }
        public class StartElection : Event { }
        public class Message : Event
        {
            public mtype MsgType;
            public int nr;

            public Message(mtype MsgType, int nr)
            {
                this.MsgType = MsgType;
                this.nr = nr;
            }
        }
        public class ContinueElection : Event { }
        #endregion

        #region fields
        private MachineId InputMachineId;
        private MachineId OutputMachineId;
        private MachineId LeaderCount_MachineId;
        private int MyNumber;

        private bool Active;
        private bool know_winner;

        private int maximum;
        private int neighbourR;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventDoAction(typeof(SetNeighbours), nameof(OnSetNeighbours))]
        [OnEventDoAction(typeof(StartElection), nameof(OnStartElection))]
        [OnEventGotoState(typeof(ContinueElection), typeof(SecondPhase))]
        [DeferEvents(typeof(Message))]
        class Init : MachineState { }

        [OnEntry(nameof(OnContinueElection))]
        class SecondPhase : MachineState { }
        #endregion

        #region actions
        void OnInit()
        {
            var e = ReceivedEvent as Initialize;
            this.MyNumber = e.MyNumber;
            this.Active = true;
            this.know_winner = false;
            this.maximum = MyNumber;
            this.LeaderCount_MachineId = e.LeaderCount_MachineId;
        }

        void OnSetNeighbours()
        {
            var e = ReceivedEvent as SetNeighbours;
            this.InputMachineId = e.InputMachineId;
            this.OutputMachineId = e.OutputMachineId;
            Raise(new StartElection());
        }

        void OnStartElection()
        {
            Console.WriteLine($"[MSG ({this.Id.Name})] My Number: " + MyNumber);
            Console.WriteLine($"[LOG ({this.Id.Name})] Sent one, {MyNumber} to {OutputMachineId.Name}");
            Send(OutputMachineId, new Message(mtype.one, MyNumber));
            Send(Id, new ContinueElection());
        }

        void OnContinueElection()
        { 
            while (true)
            {
                var receivedEvent = Receive(typeof(Message));
                var receivedMsgType = (receivedEvent as Message).MsgType;
                var receivedNr = (receivedEvent as Message).nr;

                Console.WriteLine($"[LOG ({this.Id.Name})] Received: " + receivedMsgType + "; " + receivedNr);

                if (receivedMsgType == mtype.one)
                {
                    if (Active)
                    {
                        if( receivedNr != maximum)
                        {
                            Console.WriteLine($"[LOG ({this.Id.Name})] Sent two, {receivedNr} to {OutputMachineId.Name}");
                            Send(OutputMachineId, new Message(mtype.two, receivedNr));
                            neighbourR = receivedNr;
                        }
                        else
                        {
                            this.Assert(receivedNr == 3);
                            know_winner = true;
                            Console.WriteLine($"[LOG ({this.Id.Name})] Sent winner, {receivedNr} to {OutputMachineId.Name}");
                            Send(OutputMachineId, new Message(mtype.winner, receivedNr));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[LOG ({this.Id.Name})] Sent one, {receivedNr} to {OutputMachineId.Name}");
                        Send(OutputMachineId, new Message(mtype.one, receivedNr));
                    }
                }
                else if (receivedMsgType == mtype.two)
                {
                    if (Active)
                    {
                        if (neighbourR > receivedNr && neighbourR > maximum)
                        {
                            maximum = neighbourR;
                            Console.WriteLine($"[LOG ({this.Id.Name})] Sent one, {neighbourR} to {OutputMachineId.Name}");
                            Send(OutputMachineId, new Message(mtype.one, neighbourR));
                        }
                        else
                        {
                            Active = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[LOG ({this.Id.Name})] Sent two, {receivedNr} to {OutputMachineId.Name}");
                        Send(OutputMachineId, new Message(mtype.two, receivedNr));
                    }
                }
                else if (receivedMsgType == mtype.winner)
                {
                    if (receivedNr != MyNumber)
                    {
                        Console.WriteLine($"[MSG ({this.Id.Name})] Lost");
                    }
                    else
                    {
                        Console.WriteLine($"[MSG ({this.Id.Name})] Leader");
                        Send(LeaderCount_MachineId, new LeaderCount_Machine.UpdateLeadercount());
                        Send(LeaderCount_MachineId, new LeaderCount_Machine.ValueReq(this.Id));
                        var receivedEvent1 = Receive(typeof(LeaderCount_Machine.ValueResp));
                        this.Assert((receivedEvent1 as LeaderCount_Machine.ValueResp).Value == 1);
                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyLeaderElected());
                    }
                    if (know_winner)
                    {

                    }
                    else
                    {
                        Console.WriteLine($"[LOG ({this.Id.Name})] Sent winner, {receivedNr} to {OutputMachineId.Name}");
                        Send(OutputMachineId, new Message(mtype.winner, receivedNr));
                    }
                    break;
                }
            }
            Console.WriteLine("BREAK OUT!!");
        }
        #endregion
    }
}
