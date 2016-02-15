using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class ChainReplicationMaster : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<List<MachineId>, List<MachineId>, MachineId, MachineId> initPayload;

            public eInitialize(Tuple<List<MachineId>, List<MachineId>, MachineId, MachineId> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        private class eLocal : Event { }

        private class eDone : Event { }

        private class eHeadFailed : Event { }

        private class eTailFailed : Event { }

        private class eServerFailed : Event { }

        private class eGotoWaitforFault : Event { }

        public class eFaultDetected : Event
        {
            public MachineId mPayload;

            public eFaultDetected(MachineId mPayload)
            {
                this.mPayload = mPayload;
            }
        }

        public class eStop : Event { }

        public class eHeadChanged : Event { }

        public class eTailChanged : Event { }

        public class eFixSuccessor : Event { }

        public class eNewSuccInfo : Event
        {
            public Tuple<int, int> tPayload;

            public eNewSuccInfo(Tuple<int, int> tPayload)
            {
                this.tPayload = tPayload;
            }
        }

        public class eFixPredecessor : Event { }

        public class eSuccess : Event { }
        #endregion

        #region fields
        private List<MachineId> Servers;
        private List<MachineId> Clients;

        private MachineId FaultDetector;

        private MachineId UpdatePropagationInvariantMonitor;
        private MachineId UpdateResponseQueryResponseSeqMonitor;

        private MachineId Head;
        private MachineId Tail;

        private int FaultyNodeIndex;
        private int LastUpdateReceivedSucc;
        private int LastAckSent;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitforFault))]
        private class WaitingForInit : MachineState { }

        [OnEventGotoState(typeof(eHeadFailed), typeof(CorrectHeadFailure))]
        [OnEventGotoState(typeof(eTailFailed), typeof(CorrectTailFailure))]
        [OnEventGotoState(typeof(eServerFailed), typeof(CorrectServerFailure))]
        [OnEventDoAction(typeof(eFaultDetected), nameof(CheckWhichNodeFailed))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class WaitforFault : MachineState { }

        [OnEntry(nameof(OnCorrectHeadFailureEntry))]
        [OnEventDoAction(typeof(eDone), nameof(OnDone))]
        [OnEventGotoState(typeof(eGotoWaitforFault), typeof(WaitforFault))]
        [OnEventDoAction(typeof(eHeadChanged), nameof(UpdateClients))]
        private class CorrectHeadFailure : MachineState { }

        [OnEntry(nameof(OnCorrectTailFailureEntry))]
        [OnEventDoAction(typeof(eDone), nameof(OnDone))]
        [OnEventGotoState(typeof(eGotoWaitforFault), typeof(WaitforFault))]
        [OnEventDoAction(typeof(eTailChanged), nameof(UpdateClients))]
        private class CorrectTailFailure : MachineState { }

        [OnEntry(nameof(OnCorrectServerFailureEntry))]
        [OnEventDoAction(typeof(eDone), nameof(OnDone))]
        [OnEventGotoState(typeof(eGotoWaitforFault), typeof(WaitforFault))]
        [OnEventDoAction(typeof(eFixSuccessor), nameof(FixSuccessor))]
        [OnEventDoAction(typeof(eNewSuccInfo), nameof(SetLastUpdate))]
        [OnEventDoAction(typeof(eFixPredecessor), nameof(FixPredecessor))]
        [OnEventDoAction(typeof(eSuccess), nameof(ProcessSuccess))]
        private class CorrectServerFailure : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Master] Initializing ...\n");

            Servers = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Clients = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            UpdatePropagationInvariantMonitor = (this.ReceivedEvent as eInitialize).initPayload.Item3;
            UpdateResponseQueryResponseSeqMonitor = (this.ReceivedEvent as eInitialize).initPayload.Item4;

            FaultDetector = CreateMachine(typeof(ChainReplicationFaultDetection));
            Send(FaultDetector, new ChainReplicationFaultDetection.eInitialize(
                new Tuple<MachineId, List<MachineId>>(Id, Servers)));

            Head = Servers[0];
            Tail = Servers[Servers.Count - 1];

            this.Raise(new eLocal());
        }

        private void OnCorrectHeadFailureEntry()
        {
            Console.WriteLine("[Master] CorrectHeadFailure ...\n");

            Servers.RemoveAt(0);

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                typeof(UpdatePropagationInvariantMonitor.eMonitorUpdateServers), typeof(UpdatePropagationInvariantMonitor));
            this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorUpdateServers(Servers));

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                typeof(UpdateResponseQueryResponseSeqMonitor.eMonitorUpdateServers), typeof(UpdateResponseQueryResponseSeqMonitor));
            this.Send(UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eMonitorUpdateServers(Servers));

            Head = Servers[0];

            Console.WriteLine("{0} sending event {1} to {2}\n",
                this, typeof(ChainReplicationServer.eBecomeHead), Head);
            this.Send(Head, new ChainReplicationServer.eBecomeHead(this.Id));
        }

        private void OnCorrectTailFailureEntry()
        {
            Console.WriteLine("[Master] CorrectTailFailure ...\n");

            Servers.RemoveAt(Servers.Count - 1);

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                typeof(UpdatePropagationInvariantMonitor.eMonitorUpdateServers), typeof(UpdatePropagationInvariantMonitor));
            this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorUpdateServers(Servers));

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                typeof(UpdateResponseQueryResponseSeqMonitor.eMonitorUpdateServers), typeof(UpdateResponseQueryResponseSeqMonitor));
            this.Send(UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eMonitorUpdateServers(Servers));

            Tail = Servers[Servers.Count - 1];

            Console.WriteLine("{0} sending event {1} to {2}\n", this, typeof(ChainReplicationServer.eBecomeTail), Tail);
            this.Send(Tail, new ChainReplicationServer.eBecomeTail(this.Id));
        }

        private void OnCorrectServerFailureEntry()
        {
            Console.WriteLine("[Master] CorrectServerFailure ...\n");

            Servers.RemoveAt(FaultyNodeIndex);

            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                typeof(UpdatePropagationInvariantMonitor.eMonitorUpdateServers), typeof(UpdatePropagationInvariantMonitor));
            this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorUpdateServers(Servers));
            
            Console.WriteLine("{0} sending event {1} to {2}\n", this,
                typeof(UpdateResponseQueryResponseSeqMonitor.eMonitorUpdateServers), typeof(UpdateResponseQueryResponseSeqMonitor));
            this.Send(UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eMonitorUpdateServers(Servers));

            this.Raise(new eFixSuccessor());
        }

        private void FixSuccessor()
        {
            Console.WriteLine("{0} sending event {1} to {2}\n",
                this, typeof(ChainReplicationServer.eNewPredecessor), this.Servers[this.FaultyNodeIndex]);
            this.Send(this.Servers[this.FaultyNodeIndex], new ChainReplicationServer.eNewPredecessor(
                new Tuple<MachineId, MachineId>(this.Servers[this.FaultyNodeIndex - 1], this.Id)));
        }

        private void FixPredecessor()
        {
            Console.WriteLine("{0} sending event {1} to {2}\n",
                this, typeof(ChainReplicationServer.eNewSuccessor), this.Servers[this.FaultyNodeIndex - 1]);
            this.Send(this.Servers[this.FaultyNodeIndex - 1], new ChainReplicationServer.eNewSuccessor(
                new Tuple<MachineId, MachineId, int, int>(this.Servers[this.FaultyNodeIndex],
                    this.Id, this.LastAckSent, this.LastUpdateReceivedSucc)));
        }

        private void CheckWhichNodeFailed()
        {
            if (this.Servers.Count == 1)
            {
                Assert(false, "All nodes have failed.");
            }
            else
            {
                if (this.Head.Equals((this.ReceivedEvent as eFaultDetected).mPayload))
                {
                    Console.WriteLine("[Master] Head failed ...\n");
                    this.Raise(new eHeadFailed());
                }
                else if (this.Tail.Equals((this.ReceivedEvent as eFaultDetected).mPayload))
                {
                    Console.WriteLine("[Master] Tail failed ...\n");
                    this.Raise(new eTailFailed());
                }
                else
                {
                    Console.WriteLine("[Master] Server failed ...\n");

                    for (int i = 0; i < this.Servers.Count - 1; i++)
                    {
                        if (this.Servers[i].Equals((this.ReceivedEvent as eFaultDetected).mPayload))
                        {
                            this.FaultyNodeIndex = i;
                        }
                    }

                    this.Raise(new eServerFailed());
                }
            }
        }

        private void UpdateClients()
        {
            for (int i = 0; i < this.Clients.Count; i++)
            {
                Console.WriteLine("{0} sending event {1} to {2}\n",
                    this, typeof(Client.eUpdateHeadTail), this.Clients[i]);
                this.Send(this.Clients[i], new Client.eUpdateHeadTail(new Tuple<MachineId, MachineId>(
                    this.Head, this.Tail)));
            }

            this.Raise(new eDone());
        }

        private void SetLastUpdate()
        {
            this.LastUpdateReceivedSucc = (this.ReceivedEvent as eNewSuccInfo).tPayload.Item1;
            this.LastAckSent = (this.ReceivedEvent as eNewSuccInfo).tPayload.Item2;
            this.Raise(new eFixPredecessor());
        }

        private void ProcessSuccess()
        {
            this.Raise(new eDone());
        }

        private void Stop()
        {
            Console.WriteLine("[Master] Stopping ...\n");

            this.Send(this.UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eStop());
            this.Send(this.UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eStop());

            foreach (var client in this.Clients)
            {
                this.Send(client, new Client.eStop());
            }

            foreach (var server in this.Servers)
            {
                this.Send(server, new ChainReplicationServer.eStop());
            }

            this.Raise(new Halt());
        }

        private void OnDone()
        {
            Console.WriteLine("{0} sending event {1} to {2}\n",this, typeof(ChainReplicationFaultDetection.eFaultCorrected), this.FaultDetector);
            this.Send(this.FaultDetector, new ChainReplicationFaultDetection.eFaultCorrected(this.Servers));
            Raise(new eGotoWaitforFault());
        }
        #endregion
    }
}
