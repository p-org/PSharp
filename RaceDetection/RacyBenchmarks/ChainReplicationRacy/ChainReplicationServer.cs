using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainReplicationRacy
{
    class ChainReplicationServer : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<bool, bool, int> initPayload;

            public eInitialize(Tuple<bool, bool, int> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        private class eLocal : Event { }

        private class eGotoProcessUpdate : Event
        {
            public Tuple<MachineId, Tuple<int, int>> guPayload;

            public eGotoProcessUpdate(Tuple<MachineId, Tuple<int, int>> guPayload)
            {
                this.guPayload = guPayload;
            }
        }

        public class eQuery : Event
        {
            public Tuple<MachineId, int> qPayload;

            public eQuery(Tuple<MachineId, int> qPayload)
            {
                this.qPayload = qPayload;
            }
        }

        private class eWaitForRequest : Event { }

        public class eUpdate : Event
        {
            public Tuple<MachineId, Tuple<int, int>> uPayload;

            public eUpdate(Tuple<MachineId, Tuple<int, int>> uPayload)
            {
                this.uPayload = uPayload;
            }
        }

        public class eForwardUpdate : Event
        {
            public Tuple<Tuple<int, MachineId, Tuple<int, int>>, Machine> fPayload;

            public eForwardUpdate(Tuple<Tuple<int, MachineId, Tuple<int, int>>, Machine> fPayload)
            {
                this.fPayload = fPayload;
            }
        }

        public class eBackwardAck : Event
        {
            public int seqId;

            public eBackwardAck(int seqId)
            {
                this.seqId = seqId;
            }
        }

        public class eCRPing : Event
        {
            public MachineId target;

            public eCRPing(MachineId target)
            {
                this.target = target;
            }
        }

        public class eBecomeHead : Event
        {
            public MachineId target;

            public eBecomeHead(MachineId target)
            {
                this.target = target;
            }
        }

        public class eBecomeTail : Event
        {
            public MachineId target;

            public eBecomeTail(MachineId target)
            {
                this.target = target;
            }
        }

        public class eNewPredecessor : Event
        {
            public Tuple<MachineId, MachineId> predPayload;

            public eNewPredecessor(Tuple<MachineId, MachineId> predPayload)
            {
                this.predPayload = predPayload;
            }
        }

        public class eNewSuccessor : Event
        {
            public Tuple<MachineId, MachineId, int, int> succPayload;

            public eNewSuccessor(Tuple<MachineId, MachineId, int, int> succPayload)
            {
                this.succPayload = succPayload;
            }
        }

        public class eInformAboutMonitor1 : Event
        {
            public MachineId monitorId;

            public eInformAboutMonitor1(MachineId monitorId)
            {
                this.monitorId = monitorId;
            }
        }

        public class eInformAboutMonitor2 : Event
        {
            public MachineId monitorId;

            public eInformAboutMonitor2(MachineId monitorId)
            {
                this.monitorId = monitorId;
            }
        }

        public class ePredSucc : Event
        {
            public Tuple<MachineId, MachineId> iPayload;

            public ePredSucc(Tuple<MachineId, MachineId> iPayload)
            {
                this.iPayload = iPayload;
            }
        }

        public class eStop : Event { }
        #endregion

        #region fields
        private bool IsHead;
        private bool IsTail;

        private MachineId Pred;
        private MachineId Succ;

        private MachineId UpdatePropagationInvariantMonitor;
        private MachineId UpdateResponseQueryResponseSeqMonitor;

        private List<Tuple<int, MachineId, Tuple<int, int>>> Sent;

        private int NextSeqId;
        private List<int> History;
        private Dictionary<int, int> KeyValue;

        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [DeferEvents(typeof(eUpdate), typeof(eQuery), typeof(eBackwardAck),
            typeof(eForwardUpdate), typeof(eCRPing))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForRequest))]
        [OnEventDoAction(typeof(ePredSucc), nameof(InitPred))]
        [OnEventDoAction(typeof(eInformAboutMonitor1), nameof(UpdateMonitor1))]
        [OnEventDoAction(typeof(eInformAboutMonitor2), nameof(UpdateMonitor2))]
        private class WaitingForInit : MachineState { }

        [OnEventDoAction(typeof(eUpdate), nameof(OnUpdate))]
        [OnEventGotoState(typeof(eGotoProcessUpdate), typeof(ProcessUpdate))]
        [OnEventDoAction(typeof(eQuery), nameof(OnQuery))]
        [OnEventGotoState(typeof(eWaitForRequest), typeof(WaitForRequest))]
        [OnEventGotoState(typeof(eForwardUpdate), typeof(ProcessFwdUpdate))]
        [OnEventGotoState(typeof(eBackwardAck), typeof(ProcessAck))]
        [OnEventDoAction(typeof(eCRPing), nameof(SendPong))]
        [OnEventDoAction(typeof(eBecomeHead), nameof(BecomeHead))]
        [OnEventDoAction(typeof(eBecomeTail), nameof(BecomeTail))]
        [OnEventDoAction(typeof(eNewPredecessor), nameof(UpdatePredecessor))]
        [OnEventDoAction(typeof(eNewSuccessor), nameof(UpdateSuccessor))]
        [OnEventDoAction(typeof(eInformAboutMonitor1), nameof(UpdateMonitor1))]
        [OnEventDoAction(typeof(eInformAboutMonitor2), nameof(UpdateMonitor2))]
        [OnEventDoAction(typeof(eStop), nameof(Stop))]
        private class WaitForRequest : MachineState { }

        [OnEntry(nameof(OnProcessUpdateEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForRequest))]
        private class ProcessUpdate : MachineState { }

        [OnEntry(nameof(OnProcessFwdUpdateEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForRequest))]
        private class ProcessFwdUpdate : MachineState { }

        [OnEntry(nameof(OnProcessAckEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(WaitForRequest))]
        private class ProcessAck : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Server-{0}] Initializing ...\n", Id);

            Sent = new List<Tuple<int, MachineId, Tuple<int, int>>>();
            History = new List<int>();
            KeyValue = new Dictionary<int, int>();

            IsHead = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            IsTail = (this.ReceivedEvent as eInitialize).initPayload.Item2;

            NextSeqId = 0;
        }

        private void OnProcessUpdateEntry()
        {
            Console.WriteLine("[Server-{0}] ProcessUpdate ...\n", Id);

            var client = (this.ReceivedEvent as eGotoProcessUpdate).guPayload.Item1;
            var key = (this.ReceivedEvent as eGotoProcessUpdate).guPayload.Item2.Item1;
            var value = (this.ReceivedEvent as eGotoProcessUpdate).guPayload.Item2.Item2;

            if (KeyValue.ContainsKey(key))
            {
                KeyValue[key] = value;
            }
            else
            {
                KeyValue.Add(key, value);
            }

            History.Add(NextSeqId);

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Id,
                typeof(UpdatePropagationInvariantMonitor.eMonitorHistoryUpdate), typeof(UpdatePropagationInvariantMonitor));
            this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorHistoryUpdate(
                new Tuple<MachineId, List<int>>(Id, History)));

            History.Add(0);
            Sent.Add(new Tuple<int, MachineId, Tuple<int, int>>(
                NextSeqId, client, new Tuple<int, int>(key, value)));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Id,
                typeof(UpdatePropagationInvariantMonitor.eMonitorSentUpdate), typeof(UpdatePropagationInvariantMonitor));
            this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorSentUpdate(
                new Tuple<MachineId, List<Tuple<int, MachineId, Tuple<int, int>>>>(Id, Sent)));

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, Id, typeof(eForwardUpdate), Succ);
            this.Send(Succ, new eForwardUpdate(new Tuple<Tuple<int, MachineId, Tuple<int, int>>, Machine>(
                new Tuple<int, MachineId, Tuple<int, int>>(NextSeqId, client, new Tuple<int, int>(key, value)), this)));

            this.Raise(new eLocal());
        }

        private void OnProcessFwdUpdateEntry()
        {
            Console.WriteLine("[Server-{0}] ProcessFwdUpdate ...\n", Id);

            var seqId = (this.ReceivedEvent as eForwardUpdate).fPayload.Item1.Item1;
            var client = (this.ReceivedEvent as eForwardUpdate).fPayload.Item1.Item2;
            var key = (this.ReceivedEvent as eForwardUpdate).fPayload.Item1.Item3.Item1;
            var value = (this.ReceivedEvent as eForwardUpdate).fPayload.Item1.Item3.Item2;
            var pred = (this.ReceivedEvent as eForwardUpdate).fPayload.Item2;

            if (pred.Equals(Pred))
            {
                NextSeqId = seqId;

                if (KeyValue.ContainsKey(key))
                {
                    KeyValue[key] = value;
                }
                else
                {
                    KeyValue.Add(key, value);
                }

                if (!IsTail)
                {
                    History.Add(seqId);

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Id,
                        typeof(UpdatePropagationInvariantMonitor.eMonitorHistoryUpdate), typeof(UpdatePropagationInvariantMonitor));
                    this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorHistoryUpdate(
                        new Tuple<MachineId, List<int>>(Id, History)));

                    Sent.Add(new Tuple<int, MachineId, Tuple<int, int>>(
                        seqId, client, new Tuple<int, int>(key, value)));

                    Console.WriteLine("{0}-{1} sending event {2} to monitor {3}\n", this, this.Id,
                        typeof(UpdatePropagationInvariantMonitor.eMonitorSentUpdate), typeof(UpdatePropagationInvariantMonitor));
                    this.Send(UpdatePropagationInvariantMonitor, new UpdatePropagationInvariantMonitor.eMonitorSentUpdate(
                        new Tuple<MachineId, List<Tuple<int, MachineId, Tuple<int, int>>>>(Id, Sent)));

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, Id, typeof(eForwardUpdate), Succ);
                    this.Send(Succ, new eForwardUpdate(new Tuple<Tuple<int, MachineId, Tuple<int, int>>, Machine>(
                        new Tuple<int, MachineId, Tuple<int, int>>(seqId, client,
                            new Tuple<int, int>(key, value)), this)));
                }
                else
                {
                    if (!IsHead)
                    {
                        History.Add(seqId);
                    }

                    if (UpdateResponseQueryResponseSeqMonitor != null)
                    {
                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, Id,
                        typeof(UpdateResponseQueryResponseSeqMonitor.eMonitorResponseToUpdate), typeof(UpdateResponseQueryResponseSeqMonitor));
                        this.Send(UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eMonitorResponseToUpdate(
                            new Tuple<MachineId, int, int>(Id, key, value)));
                    }

                    //Console.WriteLine("{0}-{1} sending event {2} to {3}\n", machine, machine.Id,
                    //    typeof(eMonitorResponseLiveness), typeof(LivenessUpdatetoResponseMonitor));
                    //this.Send(new eMonitorResponseLiveness(seqId));

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, Id, typeof(Client.eResponseToUpdate), client);
                    this.Send(client, new Client.eResponseToUpdate());

                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, Id, typeof(eBackwardAck), Pred);
                    this.Send(Pred, new eBackwardAck(seqId));
                }
            }

            this.Raise(new eLocal());
        }

        private void OnProcessAckEntry()
        {
            Console.WriteLine("[Server-{0}] ProcessAck ...\n", Id);

            var seqId = (this.ReceivedEvent as eBackwardAck).seqId;

            RemoveItemFromSent(seqId);

            if (!IsHead)
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, Id, typeof(eBackwardAck), Pred);
                this.Send(Pred, new eBackwardAck(seqId));
            }

            this.Raise(new eLocal());
        }

        private void InitPred()
        {
            this.Pred = (this.ReceivedEvent as ePredSucc).iPayload.Item1;
            this.Succ = (this.ReceivedEvent as ePredSucc).iPayload.Item2;
            this.Raise(new eLocal());
        }

        private void SendPong()
        {
            MachineId target = (this.ReceivedEvent as eCRPing).target;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(ChainReplicationFaultDetection.eCRPong), target);
            this.Send(target, new ChainReplicationFaultDetection.eCRPong());
        }

        private void BecomeHead()
        {
            this.IsHead = true;
            this.Pred = this.Id;

            MachineId target = (this.ReceivedEvent as eBecomeHead).target;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(ChainReplicationMaster.eHeadChanged), target);
            this.Send(target, new ChainReplicationMaster.eHeadChanged());
        }

        private void BecomeTail()
        {
            this.IsTail = true;
            this.Succ = Id;

            for (int i = 0; i < this.Sent.Count; i++)
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Id,
                    typeof(UpdateResponseQueryResponseSeqMonitor.eMonitorResponseToUpdate), typeof(UpdateResponseQueryResponseSeqMonitor));
                this.Send(this.UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eMonitorResponseToUpdate(
                    new Tuple<MachineId, int, int>(Id, this.Sent[i].Item3.Item1, this.Sent[i].Item3.Item2)));

                //Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Id,
                //    typeof(eMonitorResponseLiveness), typeof(LivenessUpdatetoResponseMonitor));
                //this.Send(new eMonitorResponseLiveness(this.Sent[i].Item1));

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(Client.eResponseToUpdate), this.Sent[i].Item2);
                this.Send(this.Sent[i].Item2, new Client.eResponseToUpdate());

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(eBackwardAck), this.Pred);
                this.Send(this.Pred, new eBackwardAck(this.Sent[i].Item1));
            }

            MachineId target = (this.ReceivedEvent as eBecomeTail).target;

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(ChainReplicationMaster.eTailChanged), target);
            this.Send(target, new ChainReplicationMaster.eTailChanged());
        }

        private void UpdatePredecessor()
        {
            this.Pred = (this.ReceivedEvent as eNewPredecessor).predPayload.Item1;
            var master = (this.ReceivedEvent as eNewPredecessor).predPayload.Item2;

            if (this.History.Count > 0)
            {
                if (this.Sent.Count > 0)
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(ChainReplicationMaster.eNewSuccInfo), master);
                    this.Send(master, new ChainReplicationMaster.eNewSuccInfo(new Tuple<int, int>(
                        this.History[this.History.Count - 1], this.Sent[0].Item1)));
                }
                else
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(ChainReplicationMaster.eNewSuccInfo), master);
                    this.Send(master, new ChainReplicationMaster.eNewSuccInfo(new Tuple<int, int>(
                        this.History[this.History.Count - 1], this.History[this.History.Count - 1])));
                }
            }
        }

        private void UpdateSuccessor()
        {
            this.Pred = (this.ReceivedEvent as eNewSuccessor).succPayload.Item1;
            var master = (this.ReceivedEvent as eNewSuccessor).succPayload.Item2;
            var lastUpdateRec = (this.ReceivedEvent as eNewSuccessor).succPayload.Item3;
            var lastAckSent = (this.ReceivedEvent as eNewSuccessor).succPayload.Item4;

            if (this.Sent.Count > 0)
            {
                for (int i = 0; i < this.Sent.Count; i++)
                {
                    if (this.Sent[i].Item1 > lastUpdateRec)
                    {
                        Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                            this, this.Id, typeof(eForwardUpdate), this.Succ);
                        this.Send(this.Succ, new eForwardUpdate(new Tuple<Tuple<int, MachineId, Tuple<int, int>>, Machine>(
                            this.Sent[i], this)));
                    }
                }

                int tempIndex = -1;
                for (int i = this.Sent.Count - 1; i >= 0; i--)
                {
                    if (this.Sent[i].Item1 == lastAckSent)
                    {
                        tempIndex = i;
                    }
                }

                for (int i = 0; i < tempIndex; i++)
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                        this, this.Id, typeof(eBackwardAck), this.Pred);
                    this.Send(this.Pred, new eBackwardAck(this.Sent[0].Item1));
                    this.Sent.RemoveAt(0);
                }
            }

            Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                this, this.Id, typeof(ChainReplicationMaster.eSuccess), master);
            this.Send(master, new ChainReplicationMaster.eSuccess());
        }

        private void UpdateMonitor1()
        {
            this.UpdatePropagationInvariantMonitor = (this.ReceivedEvent as eInformAboutMonitor1).monitorId;
        }

        private void UpdateMonitor2()
        {
            this.UpdateResponseQueryResponseSeqMonitor = (this.ReceivedEvent as eInformAboutMonitor2).monitorId;
        }
        private void RemoveItemFromSent(int req)
        {
            int removeIdx = -1;

            for (int i = this.Sent.Count - 1; i >= 0; i--)
            {
                if (req == this.Sent[i].Item1)
                {
                    removeIdx = i;
                }
            }

            if (removeIdx != -1)
            {
                this.Sent.RemoveAt(removeIdx);
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Server-{0}] Stopping ...\n", this.Id);

            Raise(new Halt());
        }

        private void OnUpdate()
        {
            Console.WriteLine("[Server-{0}] Request: eUpdate ...\n", this.Id);
            this.NextSeqId++;
            Assert(this.IsHead, "Server {0} is not head", this.Id);
            Raise(new eGotoProcessUpdate((this.ReceivedEvent as eUpdate).uPayload));
        }

        private void OnQuery()
        {
            var client = (this.ReceivedEvent as eQuery).qPayload.Item1;
            var key = (this.ReceivedEvent as eQuery).qPayload.Item2;

            Console.WriteLine("[Server-{0}] Request: eQuery ...\n", this.Id);
            Assert(this.IsTail, "Server {0} is not tail", this.Id);

            if (this.KeyValue.ContainsKey(key))
            {
                if (this.UpdateResponseQueryResponseSeqMonitor != null)
                {
                    Console.WriteLine("{0}-{1} sending event {2} to {3}\n", this, this.Id,
                    typeof(UpdateResponseQueryResponseSeqMonitor.eMonitorResponseToQuery), typeof(UpdateResponseQueryResponseSeqMonitor));
                    this.Send(this.UpdateResponseQueryResponseSeqMonitor, new UpdateResponseQueryResponseSeqMonitor.eMonitorResponseToQuery(
                        new Tuple<MachineId, int, int>(Id, key, this.KeyValue[key])));
                }

                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(Client.eResponseToQuery), client);
                this.Send(client, new Client.eResponseToQuery(new Tuple<MachineId, int>(client, this.KeyValue[key])));
            }
            else
            {
                Console.WriteLine("{0}-{1} sending event {2} to {3}\n",
                    this, this.Id, typeof(Client.eResponseToQuery), client);
                this.Send(client, new Client.eResponseToQuery(new Tuple<MachineId, int>(client, -1)));
            }
            Raise(new eWaitForRequest());
        }
        #endregion
    }
}

