using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChordRacy
{
    class ChordNode : Machine
    {
        #region events
        private class eLocal : Event { }

        private class eWaitForInit : Event { }

        public class eInitialize : Event
        {
            public Tuple<MachineId, int, int> initPayload;

            public eInitialize(Tuple<MachineId, int, int> initPayload)
            {
                this.initPayload = initPayload;
            }
        }

        public class eFindSuccessor : Event
        {
            public Tuple<MachineId, int, int> fsPayload;

            public eFindSuccessor(Tuple<MachineId, int, int> fsPayload)
            {
                this.fsPayload = fsPayload;
            }
        }

        public class eFindPredecessor : Event
        {
            public MachineId sender;

            public eFindPredecessor(MachineId sender)
            {
                this.sender = sender;
            }
        }

        public class eNotifySuccessor : Event
        {
            public MachineId predecessor;

            public eNotifySuccessor(MachineId predecessor)
            {
                this.predecessor = predecessor;
            }
        }

        public class eStabilize : Event { }

        public class eStop : Event { }

        public class eConfigure : Event
        {
            public Tuple<List<int>, List<MachineId>, List<int>> conf;

            public eConfigure(Tuple<List<int>, List<MachineId>, List<int>> conf)
            {
                this.conf = conf;
            }
        }

        public class eJoin : Event
        {
            public Tuple<List<int>, List<MachineId>> join;

            public eJoin(Tuple<List<int>, List<MachineId>> join)
            {
                this.join = join;
            }
        }

        public class eQueryId : Event
        {
            public MachineId sender;

            public eQueryId(MachineId sender)
            {
                this.sender = sender;
            }
        }

        public class eFindSuccessorResp : Event
        {
            public Message msg;

            public eFindSuccessorResp(Message msg)
            {
                this.msg = msg;
            }
        }

        public class eFindPredecessorResp : Event
        {
            public MachineId successor;

            public eFindPredecessorResp(MachineId successor)
            {
                this.successor = successor;
            }
        }

        public class eAskForKeys : Event
        {
            public Tuple<MachineId, int> keyPayload;

            public eAskForKeys(Tuple<MachineId, int> keyPayload)
            {
                this.keyPayload = keyPayload;
            }
        }

        public class eAskForKeysAck : Event
        {
            public List<int> keyList;

            public eAskForKeysAck(List<int> keyList)
            {
                this.keyList = keyList;
            }
        }

        public class eFail : Event { }
        #endregion


        public class Message
        {
            public MachineId Machine;
            public int Id;

            public Message(MachineId machine, int id)
            {
                this.Machine = machine;
                this.Id = id;
            }
        }

        #region fields
        private MachineId Cluster;

        private int M;
        private int Identity;
        private int NumOfId;

        private List<int> Keys;
        private Dictionary<int, Tuple<int, int, MachineId>> FingerTable;
        private MachineId Predecessor;

        private Message message;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForInit))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [DeferEvents(typeof(eFindSuccessor),
                    typeof(eFindPredecessor),
                    typeof(eNotifySuccessor),
                    typeof(eStabilize),
                    typeof(eStop))]
        [OnEventGotoState(typeof(eConfigure), typeof(Configuring))]
        [OnEventGotoState(typeof(eJoin), typeof(Joining))]
        private class WaitingForInit : MachineState { }

        [OnEntry(nameof(OnConfiguringEntry))]
        [DeferEvents(typeof(eFindSuccessor),
                    typeof(eFindPredecessor),
                    typeof(eNotifySuccessor),
                    typeof(eStabilize),
                    typeof(eStop))]
        [OnEventGotoState(typeof(eLocal), typeof(Waiting))]
        private class Configuring : MachineState { }

        [OnEntry(nameof(OnJoiningEntry))]
        [DeferEvents(typeof(eFindSuccessor),
                    typeof(eFindPredecessor),
                    typeof(eNotifySuccessor),
                    typeof(eStabilize),
                    typeof(eStop))]
        [OnEventGotoState(typeof(eLocal), typeof(Waiting))]
        [OnEventDoAction(typeof(eQueryId), nameof(SendId))]
        private class Joining : MachineState { }

        [OnEntry(nameof(OnWaitingEntry))]
        [OnEventDoAction(typeof(eQueryId), nameof(SendId))]
        [OnEventDoAction(typeof(eStabilize), nameof(Stabilize))]
        [OnEventDoAction(typeof(eFindPredecessor), nameof(SendPredecessor))]
        [OnEventDoAction(typeof(eFindSuccessor), nameof(FindSuccessor))]
        [OnEventDoAction(typeof(eFindSuccessorResp), nameof(SuccessorFound))]
        [OnEventDoAction(typeof(eNotifySuccessor), nameof(UpdatePredecessor))]
        [OnEventDoAction(typeof(eFindPredecessorResp), nameof(UpdateSuccessor))]
        [OnEventDoAction(typeof(eAskForKeys), nameof(SendCorrespondingKeys))]
        [OnEventDoAction(typeof(eAskForKeysAck), nameof(UpdateKeys))]
        [OnEventDoAction(typeof(eFail), nameof(Failing))]
        [OnEventDoAction(typeof(eStop), nameof(Stopping))]
        private class Waiting : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Cluster = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            Identity = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            M = (this.ReceivedEvent as eInitialize).initPayload.Item3;
            message = null;
            
            Console.WriteLine("[ChordNode-{0}] Initializing ...\n", Identity);

            NumOfId = (int)Math.Pow(2, M);
            Keys = new List<int>();
            FingerTable = new Dictionary<int, Tuple<int, int, MachineId>>();
        }

        private void OnConfiguringEntry()
        {
            Console.WriteLine("[ChordNode-{0}] Configuring ...\n", Identity);

            var nodeIds = (this.ReceivedEvent as eConfigure).conf.Item1;
            var nodes = (this.ReceivedEvent as eConfigure).conf.Item2;
            var keys = (this.ReceivedEvent as eConfigure).conf.Item3;

            foreach (var key in keys)
            {
                Keys.Add(key);
            }

            for (var idx = 1; idx <= M; idx++)
            {
                var start = (Identity + (int)Math.Pow(2, (idx - 1))) % NumOfId;
                var end = (Identity + (int)Math.Pow(2, idx)) % NumOfId;

                var nodeId = GetSuccessorNodeId(start, nodeIds);
                FingerTable.Add(start, new Tuple<int, int, MachineId>(
                    start, end, nodes[nodeId]));
            }

            for (var idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == Identity)
                {
                    Predecessor = nodes[WrapSubtract(idx, 1, nodeIds.Count)];
                    break;
                }
            }

            this.Raise(new eLocal());
        }

        private void OnJoiningEntry()
        {
            Console.WriteLine("[ChordNode-{0}] Joining ...\n", Identity);

            var nodeIds = (this.ReceivedEvent as eJoin).join.Item1;
            var nodes = (this.ReceivedEvent as eJoin).join.Item2;

            for (var idx = 1; idx <= M; idx++)
            {
                var start = (Identity + (int)Math.Pow(2, (idx - 1))) % NumOfId;
                var end = (Identity + (int)Math.Pow(2, idx)) % NumOfId;
                var nodeId = GetSuccessorNodeId(start, nodeIds);
                FingerTable.Add(start, new Tuple<int, int, MachineId>(
                    start, end, nodes[nodeId]));
            }

            var successor = FingerTable[(Identity + 1) % NumOfId].Item3;

            this.Send(Cluster, new Cluster.eJoinAck());
            this.Send(successor, new eNotifySuccessor(this.Id));

            this.Raise(new eLocal());
        }

        private void OnWaitingEntry()
        {
            Console.WriteLine("[ChordNode-{0}] Waiting ...\n", Identity);
        }

        private void SendId()
        {
            Console.WriteLine("[ChordNode-{0}] Sending Id ...\n", this.Identity);

            var sender = (this.ReceivedEvent as eQueryId).sender;
            this.Send(sender, new Client.eQueryIdResp(this.Identity));
        }

        private void FindSuccessor()
        {
            var sender = (this.ReceivedEvent as eFindSuccessor).fsPayload.Item1;
            var id = (this.ReceivedEvent as eFindSuccessor).fsPayload.Item2;
            var timeout = (this.ReceivedEvent as eFindSuccessor).fsPayload.Item3;

            Console.WriteLine("[ChordNode-{0}] Finding successor of {1} ...\n", this.Identity, id);

            Message message;

            if (this.Keys.Contains(id))
            {
                message = new Message(this.Id, id);
                this.Send(sender, new eFindSuccessorResp(message));
                this.message = message;
            }
            else if (this.FingerTable.ContainsKey(id))
            {
                message = new Message(this.FingerTable[id].Item3, id);
                this.Send(sender, new eFindSuccessorResp(message));
                this.message = message;
            }
            else if (this.Identity.Equals(id))
            {
                message = new Message(this.FingerTable[(this.Identity + 1) % this.NumOfId].Item3, id);
                this.Send(sender, new eFindSuccessorResp(message));
                this.message = message;
            }
            else
            {
                int idToAsk = -1;
                foreach (var finger in this.FingerTable)
                {
                    if (((finger.Value.Item1 > finger.Value.Item2) &&
                        (finger.Value.Item1 <= id || id < finger.Value.Item2)) ||
                        ((finger.Value.Item1 < finger.Value.Item2) &&
                        (finger.Value.Item1 <= id && id < finger.Value.Item2)))
                    {
                        idToAsk = finger.Key;
                    }
                }

                if (idToAsk < 0)
                {
                    idToAsk = (this.Identity + 1) % this.NumOfId;
                }

                if (this.FingerTable[idToAsk].Item3.Equals(this))
                {
                    foreach (var finger in this.FingerTable)
                    {
                        if (finger.Value.Item2 == idToAsk ||
                            finger.Value.Item2 == idToAsk - 1)
                        {
                            idToAsk = finger.Key;
                            break;
                        }
                    }
                }

                timeout--;
                if (timeout == 0)
                {
                    return;
                }

                this.Send(this.FingerTable[idToAsk].Item3, new eFindSuccessor(
                    new Tuple<MachineId, int, int>(sender, id, timeout)));
            }
        }

        private void Stabilize()
        {
            Console.WriteLine("[ChordNode-{0}] Stabilizing ...\n", this.Identity);

            var successor = this.FingerTable[(this.Identity + 1) % this.NumOfId].Item3;
            this.Send(successor, new eFindPredecessor(this.Id));

            foreach (var finger in this.FingerTable)
            {
                if (!finger.Value.Item3.Equals(successor))
                {
                    this.message.Id = 0;
                    this.Send(successor, new eFindSuccessor(
                        new Tuple<MachineId, int, int>(this.Id, finger.Key, 100)));
                }
            }
        }

        private void UpdatePredecessor()
        {
            Console.WriteLine("[ChordNode-{0}] Updating predecessor ...\n", this.Identity);

            var predecessor = (this.ReceivedEvent as eNotifySuccessor).predecessor;
            if (predecessor.Equals(this))
            {
                return;
            }

            this.Predecessor = predecessor;
        }

        private void UpdateSuccessor()
        {
            Console.WriteLine("[ChordNode-{0}] Updating successor ...\n", this.Identity);

            var successor = (this.ReceivedEvent as eFindPredecessorResp).successor;
            if (successor.Equals(this))
            {
                return;
            }

            this.FingerTable[(this.Identity + 1) % this.NumOfId] = new Tuple<int, int, MachineId>(
                this.FingerTable[(this.Identity + 1) % this.NumOfId].Item1,
                this.FingerTable[(this.Identity + 1) % this.NumOfId].Item2,
                successor);

            this.Send(successor, new eNotifySuccessor(this.Id));
            this.Send(successor, new eAskForKeys(new Tuple<MachineId, int>(this.Id, this.Identity)));
        }

        private void SuccessorFound()
        {
            Console.WriteLine("[ChordNode-{0}] Successor found ...\n", this.Identity);

            var successor = (this.ReceivedEvent as eFindSuccessorResp).msg.Machine;
            var id = (this.ReceivedEvent as eFindSuccessorResp).msg.Id;

            this.FingerTable[id] = new Tuple<int, int, MachineId>(
                this.FingerTable[id].Item1,
                this.FingerTable[id].Item2,
                successor);
        }

        private void UpdateKeys()
        {
            Console.WriteLine("[ChordNode-{0}] Updating keys ...\n", this.Identity);

            var keys = (this.ReceivedEvent as eAskForKeysAck).keyList;
            foreach (var key in keys)
            {
                this.Keys.Add(key);
            }
        }

        private void SendPredecessor()
        {
            Console.WriteLine("[ChordNode-{0}] Sending predecessor ...\n", this.Identity);

            var sender = (this.ReceivedEvent as eFindPredecessor).sender;
            if (this.Predecessor != null)
            {
                this.Send(sender, new eFindPredecessorResp(this.Predecessor));
            }
        }

        private void SendCorrespondingKeys()
        {
            var sender = (this.ReceivedEvent as eAskForKeys).keyPayload.Item1;
            var senderId = (this.ReceivedEvent as eAskForKeys).keyPayload.Item2;
            Console.WriteLine("[ChordNode-{0}] Sending keys to predecessor {1} ...\n", this.Identity, senderId);

            List<int> keysToSend = new List<int>();
            foreach (var key in this.Keys)
            {
                if (key <= senderId)
                {
                    keysToSend.Add(key);
                }
            }

            if (keysToSend.Count == 0)
            {
                return;
            }

            this.Send(sender, new eAskForKeysAck(keysToSend));

            foreach (var key in keysToSend)
            {
                this.Keys.Remove(key);
            }
        }

        private void Failing()
        {
            Console.WriteLine("[ChordNode-{0}] Failing ...\n", this.Identity);
            Raise(new Halt());
        }

        private void Stopping()
        {
            Console.WriteLine("[ChordNode-{0}] Stopping ...\n", this.Identity);
            Raise(new Halt());
        }

        private int WrapAdd(int left, int right, int ceiling)
        {
            int result = left + right;
            if (result > ceiling)
            {
                result = ceiling - result;
            }

            return result;
        }

        private int WrapSubtract(int left, int right, int ceiling)
        {
            int result = left - right;
            if (result < 0)
            {
                result = ceiling + result;
            }

            return result;
        }

        private int GetSuccessorNodeId(int start, List<int> nodeIds)
        {
            var candidate = -1;
            foreach (var id in nodeIds.Where(v => v >= start))
            {
                if (candidate < 0 || id < candidate)
                {
                    candidate = id;
                }
            }

            if (candidate < 0)
            {
                foreach (var id in nodeIds.Where(v => v < start))
                {
                    if (candidate < 0 || id < candidate)
                    {
                        candidate = id;
                    }
                }
            }

            for (int idx = 0; idx < nodeIds.Count; idx++)
            {
                if (nodeIds[idx] == candidate)
                {
                    candidate = idx;
                    break;
                }
            }

            return candidate;
        }

        private void PrintFingerTableAndKeys()
        {
            Console.WriteLine("[ChordNode-{0}] Printing finger table:", this.Identity);
            foreach (var finger in this.FingerTable)
            {
                Console.WriteLine("  > " + finger.Key + " | [" + finger.Value.Item1 +
                    ", " + finger.Value.Item2 + ") | " + finger.Value.Item3);
            }

            Console.WriteLine("[ChordNode-{0}] Printing keys:", this.Identity);
            foreach (var key in this.Keys)
            {
                Console.WriteLine("  > Key-" + key);
            }

            Console.WriteLine();
        }
        #endregion
    }
}
