using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BasicPaxosRacy
{
    class GodMachine : Machine
    {
        #region events
        private class eLocal : Event { }
        #endregion


        #region C# Classes and Structs

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
        #endregion

        #region fields
        private List<MachineId> Proposers;
        private List<MachineId> Acceptors;
        private MachineId PaxosInvariantMonitor;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eLocal), typeof(End))]
        private class Init : MachineState { }

        [OnEntry(nameof(OnEnd))]
        private class End : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[GodMachine] Initializing ...\n");

            this.PaxosInvariantMonitor = CreateMachine(typeof(PaxosInvariantMonitor));

            this.Proposers = new List<MachineId>();
            this.Acceptors = new List<MachineId>();

            for (int i = 0; i < 3; i++)
            {
                MachineId acId = CreateMachine(typeof(Acceptor));
                Send(acId, new Acceptor.eInitialize(i + 1));
                this.Acceptors.Insert(i, acId);
            }

            MachineId pr1 = CreateMachine(typeof(Proposer));
            Send(pr1, new Proposer.eInitialize(new Tuple<MachineId, List<MachineId>, List<MachineId>, int, int>(
                        this.PaxosInvariantMonitor, this.Proposers, this.Acceptors, 1, 1)));
            this.Proposers.Insert(0, pr1);

            MachineId pr2 = CreateMachine(typeof(Proposer));
            Send(pr2, new Proposer.eInitialize(new Tuple<MachineId, List<MachineId>, List<MachineId>, int, int>(
                        this.PaxosInvariantMonitor, this.Proposers, this.Acceptors, 2, 100)));
            this.Proposers.Insert(1, pr2);

            this.Raise(new eLocal());
        }

        private void OnEnd()
        {
            Console.WriteLine("[GodMachine] Stopping ...\n");
            Console.ReadLine();
            this.Raise(new Halt());
        }
        #endregion
    }
}
