using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChordRacy
{
    class Cluster : Machine
    {
        #region events
        private class eWaitForInit : Event { }

        private class eLocal : Event { }

        public class eInitialize : Event
        {
            public Tuple<int, List<int>, List<int>> initPayload;

            public eInitialize(Tuple<int, List<int>, List<int>> initPayload)
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

        public class eJoinAck : Event { }
        #endregion

        #region fields
        private int M;
        private int NumOfId;
        private int QueryCounter;

        private List<int> Keys;
        private List<int> NodeIds;
        private List<MachineId> Nodes;

        private MachineId Client;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(eWaitForInit), typeof(WaitingForEvent))]
        private class Init : MachineState { }

        [OnEventDoAction(typeof(eInitialize), nameof(OnInitialize))]
        [DeferEvents(typeof(eFindSuccessor))]
        [OnEventGotoState(typeof(eLocal), typeof(Querying))]
        private class WaitingForEvent : MachineState { }

        [OnEntry(nameof(OnQueryingEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Waiting))]
        private class Querying : MachineState { }

        [OnEntry(nameof(OnWaitingEntry))]
        [OnEventGotoState(typeof(eLocal), typeof(Querying))]
        [OnEventDoAction(typeof(eFindSuccessor), nameof(FindSuccessor))]
        [OnEventDoAction(typeof(eJoinAck), nameof(QueryStabilize))]
        private class Waiting : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Raise(new eWaitForInit());
        }

        private void OnInitialize()
        {
            Console.WriteLine("[Cluster] Initializing ...\n");

            M = (this.ReceivedEvent as eInitialize).initPayload.Item1;
            NodeIds = (this.ReceivedEvent as eInitialize).initPayload.Item2;
            Keys = (this.ReceivedEvent as eInitialize).initPayload.Item3;

            NumOfId = (int)Math.Pow(2, M);
            Nodes = new List<MachineId>();
            QueryCounter = 0;

            for (int idx = 0; idx < NodeIds.Count; idx++)
            {
                MachineId chNodeId = CreateMachine(typeof(ChordNode));
                Send(chNodeId, new ChordNode.eInitialize(new Tuple<MachineId, int, int>(Id, NodeIds[idx], M)));
                Nodes.Add(chNodeId);
            }

            var nodeKeys = new Dictionary<int, List<int>>();
            for (int i = Keys.Count - 1; i >= 0; i--)
            {

                bool assigned = false;
                for (int j = 0; j < NodeIds.Count; j++)
                {
                    if (Keys[i] <= NodeIds[j])
                    {
                        if (nodeKeys.ContainsKey(NodeIds[j]))
                        {
                            nodeKeys[NodeIds[j]].Add(Keys[i]);
                        }
                        else
                        {
                            nodeKeys.Add(NodeIds[j], new List<int>());
                            nodeKeys[NodeIds[j]].Add(Keys[i]);
                        }

                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    if (nodeKeys.ContainsKey(NodeIds[0]))
                    {
                        nodeKeys[NodeIds[0]].Add(Keys[i]);
                    }
                    else
                    {
                        nodeKeys.Add(NodeIds[0], new List<int>());
                        nodeKeys[NodeIds[0]].Add(Keys[i]);
                    }
                }
            }

            for (int idx = 0; idx < Nodes.Count; idx++)
            {
                this.Send(Nodes[idx], new ChordNode.eConfigure(new Tuple<List<int>, List<MachineId>, List<int>>(
                    NodeIds, Nodes, nodeKeys[NodeIds[idx]])));
            }

            Client = CreateMachine(typeof(Client));
            Send(Client, new Client.eInitialize(new Tuple<MachineId, List<int>>(Id, Keys)));

            this.Raise(new eLocal());
        }

        private void OnQueryingEntry()
        {
            if (QueryCounter < 10)
            {
                Console.WriteLine("[Cluster] Query {0} ...\n", QueryCounter);

                CreateNewNode();

                QueryCounter++;
            }
            else
            {
                Console.WriteLine("[Cluster] Notifying client ...\n");
                this.Send(Client, new Client.eNotifyClient());
            }

            this.Raise(new eLocal());
        }

        private void OnWaitingEntry()
        {
            Console.WriteLine("[Cluster] Waiting ...\n");

            if (QueryCounter == 10)
            {
                TriggerStop();
            }
        }

        private void CreateNewNode()
        {
            int newId = -1;
            Random random = new Random();
            while ((newId < 0 || this.NodeIds.Contains(newId)) &&
                this.NodeIds.Count < this.NumOfId)
            {
                newId = random.Next(this.NumOfId);
            }

            if (newId < 0)
            {
                this.TriggerStop();
                return;
            }

            var index = 0;
            for (int idx = 0; idx < this.NodeIds.Count; idx++)
            {
                if (this.NodeIds[idx] > index)
                {
                    index = idx;
                    break;
                }
            }

            Console.WriteLine("[Cluster] Creating new node with Id {0} ...\n", newId);

            var newNode = CreateMachine(typeof(ChordNode));
            Send(newNode, new ChordNode.eInitialize(new Tuple<MachineId, int, int>(Id, newId, this.M)));
            this.NodeIds.Insert(index, newId);
            this.Nodes.Insert(index, newNode);

            this.Send(newNode, new ChordNode.eJoin(new Tuple<List<int>, List<MachineId>>(this.NodeIds, this.Nodes)));
        }

        private void QueryStabilize()
        {
            foreach (var node in this.Nodes)
            {
                this.Send(node, new ChordNode.eStabilize());
            }

            this.Raise(new eLocal());
        }

        private void TriggerFailure()
        {
            Console.WriteLine("[Cluster] Triggering a failure ...\n");

            int failId = -1;
            Random random = new Random(0);
            while ((failId < 0 || !this.NodeIds.Contains(failId)) &&
                this.NodeIds.Count > 0)
            {
                failId = random.Next(this.NumOfId);
            }

            if (failId < 0)
            {
                this.TriggerStop();
                return;
            }

            var nodeToFail = this.Nodes[failId];

            this.Send(nodeToFail, new ChordNode.eFail());

            this.QueryStabilize();
        }

        private void TriggerStop()
        {
            Console.WriteLine("[Cluster] Stopping ...\n");

            this.Send(this.Client, new Client.eStop());

            foreach (var node in this.Nodes)
            {
                this.Send(node, new ChordNode.eStop());
            }

            Raise(new Halt());
        }

        private void FindSuccessor()
        {
            Console.WriteLine("[Cluster] Propagating: eFindSuccessor ...\n");
            this.Send(this.Nodes[0], new ChordNode.eFindSuccessor((this.ReceivedEvent as eFindSuccessor).fsPayload));
        }
        #endregion
    }
}

