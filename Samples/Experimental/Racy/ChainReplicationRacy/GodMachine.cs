using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class GodMachine : Machine
    {
        #region events
        #endregion

        #region fields
        private List<MachineId> Servers;
        private List<MachineId> Clients;

        private MachineId ChainReplicationMaster;

        private MachineId UpdatePropagationInvariantMonitor;
        private MachineId UpdateResponseQueryResponseSeqMonitor;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("[GodMachine] Initializing ...\n");

            Servers = new List<MachineId>();
            Clients = new List<MachineId>();

            MachineId chRId = CreateMachine(typeof(ChainReplicationServer));
            Servers.Insert(0, chRId);
            Send(chRId, new ChainReplicationServer.eInitialize(new Tuple<bool, bool, int>(false, true, 3)));

            MachineId chRId1 = CreateMachine(typeof(ChainReplicationServer));
            Servers.Insert(0, chRId1);
            Send(chRId1, new ChainReplicationServer.eInitialize(new Tuple<bool, bool, int>(false, false, 2)));

            MachineId chRId2 = CreateMachine(typeof(ChainReplicationServer));
            Servers.Insert(0, chRId2);
            Send(chRId2, new ChainReplicationServer.eInitialize(new Tuple<bool, bool, int>(true, false, 1)));

            UpdatePropagationInvariantMonitor = CreateMachine(typeof(UpdatePropagationInvariantMonitor));
            Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eInitialize(Servers));
            UpdateResponseQueryResponseSeqMonitor = CreateMachine(typeof(UpdateResponseQueryResponseSeqMonitor));
            //Send(UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eInitialize(Servers));

            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationServer.ePredSucc), Servers[2]);
            this.Send(Servers[2], new ChainReplicationServer.ePredSucc(new Tuple<MachineId, MachineId>(Servers[1], Servers[2])));

            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationServer.ePredSucc), this.Servers[1]);
            this.Send(Servers[1], new ChainReplicationServer.ePredSucc(new Tuple<MachineId, MachineId>(Servers[0], Servers[2])));

            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationServer.ePredSucc), Servers[0]);
            this.Send(Servers[0], new ChainReplicationServer.ePredSucc(new Tuple<MachineId, MachineId>(Servers[0], Servers[1])));

            MachineId clId = CreateMachine(typeof(Client));
            Send(clId, new Client.eInitialize(new Tuple<int, MachineId, MachineId, int>(1, Servers[0], Servers[2], 1)));
            Clients.Insert(0, clId);

            MachineId clId2 = CreateMachine(typeof(Client));
            Send(clId2, new Client.eInitialize(new Tuple<int, MachineId, MachineId, int>(0, Servers[0], Servers[2], 100)));
            Clients.Insert(0, clId2);

            ChainReplicationMaster = CreateMachine(typeof(ChainReplicationMaster));
            Send(ChainReplicationMaster, new ChainReplicationMaster.eInitialize(
                new Tuple<List<MachineId>, List<MachineId>, MachineId, MachineId>(
                    Servers, Clients,
                    UpdatePropagationInvariantMonitor,
                    UpdateResponseQueryResponseSeqMonitor)));

            Raise(new Halt());
        }
        #endregion
    }
}
