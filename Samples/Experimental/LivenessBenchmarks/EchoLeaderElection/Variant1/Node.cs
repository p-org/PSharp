/* Echo Election Algorithm with Extinction in an Arbitrary Network. */
/* Variation 1: Node 0 wins every time.                             */

using Microsoft.PSharp;

namespace Variant1
{
    class Node : Machine
    {
        #region events
        public class Config : Event
        {
            public MachineId Neighbour1;
            public int Neighbour1Id;
            public MachineId Neighbour2;
            public int Neighbour2Id;
            public MachineId DoneMachine;
            public MachineId NrLeadersMachine;
            public MachineId LeaderMachine;

            public Config(MachineId neighbour1, int neighbour1Id, MachineId neighbour2, int neighbour2Id,
                MachineId doneMachine, MachineId nrLeadersMachine, MachineId leaderMachine)
            {
                this.Neighbour1 = neighbour1;
                this.Neighbour1Id = neighbour1Id;
                this.Neighbour2 = neighbour2;
                this.Neighbour2Id = neighbour2Id;
                this.DoneMachine = doneMachine;
                this.NrLeadersMachine = nrLeadersMachine;
                this.LeaderMachine = leaderMachine;
            }
        }

        public class Initialize : Event
        {
            public int MyId;
            public Initialize(int myId)
            {
                this.MyId = myId;
            }
        }
        public class Tok : Event
        {
            public int Token;
            public int SenderId;
            public Tok() { }
            public Tok(int token, int senderId)
            {
                this.Token = token;
                this.SenderId = senderId;
            }
        }
        public class Ldr : Event
        {
            public int Value;
            public Ldr(int value)
            {
                this.Value = value;
            }
        }
        public class Local : Event { }
        #endregion

        #region fields
        MachineId Neighbour1;
        MachineId Neighbour2;
        MachineId DoneMachine;
        MachineId NrLeadersMachine;
        MachineId LeaderMachine;

        int MyId;
        int Neighbour1Id;
        int Neighbour2Id;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(Config), nameof(OnConfig))]
        [OnEventGotoState(typeof(Local), typeof(ProcessingElection))]
        class Init : MachineState { }

        [OnEntry(nameof(ElectLeader))]
        class ProcessingElection : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var e = ReceivedEvent as Initialize;
            MyId = e.MyId;
        }
        void OnConfig()
        {
            var e = ReceivedEvent as Config;
            Neighbour1 = e.Neighbour1;
            Neighbour2 = e.Neighbour2;
            DoneMachine = e.DoneMachine;
            NrLeadersMachine = e.NrLeadersMachine;
            LeaderMachine = e.LeaderMachine;

            Raise(new Local());
        }

        void ElectLeader()
        {
            int myid = 3 - Neighbour1Id - Neighbour2Id;
            int caw, rec, father, lrec, win, r;

            restart:
            Receive(typeof(Tok));
            caw = myid; rec = 0; lrec = 0;
            father = 3; win = 3; r = 3;

            Send(Neighbour1, new Tok(myid, MyId));
            Send(Neighbour2, new Tok(myid, MyId));

            while (true)
            {
                if (lrec == 2)
                {
                    break;
                }
                var receivedEvt = Receive(typeof(Tok), typeof(Ldr));
                if (receivedEvt is Ldr)
                {
                    if (lrec == 0 && r != myid)
                    {
                        Send(Neighbour1, new Ldr(r));
                        Send(Neighbour2, new Ldr(r));
                    }
                    lrec++;
                    win = r;

                }
                if (receivedEvt is Tok)
                {
                    recvtok((receivedEvt as Tok).SenderId, Neighbour2, ref r, ref caw, ref rec, ref father, ref myid);
                }
            }

            if (win == myid)
            {
                Send(LeaderMachine, new Leader.SetValue(myid));
                Send(NrLeadersMachine, new NrLeadersMachine.Increment());

                Send(NrLeadersMachine, new NrLeadersMachine.GetValue(this.Id));
                var receivedEvt = Receive(typeof(NrLeadersMachine.GotValue));
                this.Assert((receivedEvt as NrLeadersMachine.GotValue).Value == 1, "More than one leader");
            }
            Send(DoneMachine, new DoneMachine.Increment());
            goto restart;
        }
        #endregion

        #region private methods
        void recvtok(int q, MachineId c, ref int r, ref int caw, ref int rec, ref int father, ref int myid)
        {
            if (r < caw)
            {
                caw = r;
                rec = 0;
                father = q;
                Send(c, new Tok(r, MyId));
            }

            if (r == caw)
            {
                rec++;
                if (rec == 2 && caw == myid)
                {
                    Send(Neighbour1, new Ldr(myid));
                    Send(Neighbour2, new Ldr(myid));
                }
                if (rec == 2 && caw != myid && father == Neighbour1Id)
                {
                    Send(Neighbour1, new Tok(caw, MyId));
                }
                if (rec == 2 && caw != myid && father == Neighbour2Id)
                {
                    Send(Neighbour2, new Tok(caw, MyId));
                }
            }
        }
        #endregion
    }
}



