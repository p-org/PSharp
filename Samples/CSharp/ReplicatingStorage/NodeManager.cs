using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    internal class NodeManager : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the node manager.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public MachineId Environment;
            public int NumberOfReplicas;

            public ConfigureEvent(MachineId env, int numOfReplicas)
                : base()
            {
                this.Environment = env;
                this.NumberOfReplicas = numOfReplicas;
            }
        }

        internal class ShutDown : Event { }
        private class LocalEvent : Event { }

        #endregion

        #region fields

        /// <summary>
        /// The environment.
        /// </summary>
        private MachineId Environment;

        /// <summary>
        /// The nodes.
        /// </summary>
        private List<MachineId> Nodes;

        /// <summary>
        /// The number of replicas that must
        /// be sustained.
        /// </summary>
        private int NumberOfReplicas;

        /// <summary>
        /// The client who sent the latest request.
        /// </summary>
        private MachineId Client;

        /// <summary>
        /// Map from node ids to a boolean value that
        /// denotes if the node is alive or not.
        /// </summary>
        private Dictionary<int, bool> NodeMap;

        /// <summary>
        /// Map from node ids to data they contain.
        /// </summary>
        private Dictionary<int, int> DataMap;

        /// <summary>
        /// The repair timer.
        /// </summary>
        private MachineId RepairTimer;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(Client.Request), typeof(RepairTimer.Timeout))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.Nodes = new List<MachineId>();
            this.NodeMap = new Dictionary<int, bool>();
            this.DataMap = new Dictionary<int, int>();

            this.RepairTimer = this.CreateMachine(typeof(RepairTimer));
            this.Send(this.RepairTimer, new RepairTimer.ConfigureEvent(this.Id));
        }

        void Configure()
        {
            this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
            this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;

            for (int idx = 0; idx < this.NumberOfReplicas; idx++)
            {
                this.CreateNewNode();
            }

            this.Raise(new LocalEvent());
        }

        void CreateNewNode()
        {
            var idx = this.Nodes.Count;
            var node = this.CreateMachine(typeof(Node));
            this.Nodes.Add(node);
            this.NodeMap.Add(idx, true);
            this.Send(node, new Node.ConfigureEvent(this.Environment, this.Id, idx));
        }

        [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
        [OnEventDoAction(typeof(RepairTimer.Timeout), nameof(RepairNodes))]
        [OnEventDoAction(typeof(Node.SyncReport), nameof(ProcessSyncReport))]
        [OnEventDoAction(typeof(Node.NotifyFailure), nameof(ProcessFailure))]
        class Active : MachineState { }

        void ProcessClientRequest()
        {
            this.Client = (this.ReceivedEvent as Client.Request).Client;
            var command = (this.ReceivedEvent as Client.Request).Command;

            var aliveNodeIds = this.NodeMap.Where(n => n.Value).Select(n => n.Key);
            foreach (var nodeId in aliveNodeIds)
            {
                this.Send(this.Nodes[nodeId], new Node.StoreRequest(command));
            }

            this.Send(this.Environment, new Environment.NotifyClientRequestHandled());
        }

        void RepairNodes()
        {
            var consensus = this.DataMap.Select(kvp => kvp.Value).GroupBy(v => v).
                OrderByDescending(v => v.Count()).FirstOrDefault();
            if (consensus == null)
            {
                return;
            }

            Console.WriteLine("\n [NodeManager] consensus {0} - {1}.\n",
                consensus.Count(), consensus.Key);

            var numOfReplicas = consensus.Count();
            if (numOfReplicas >= this.NumberOfReplicas)
            {
                return;
            }

            foreach (var node in this.DataMap)
            {
                if (node.Value != consensus.Key)
                {
                    Console.WriteLine("\n [NodeManager] repairing node {0}.\n", node.Key);

                    this.Send(this.Nodes[node.Key], new Node.StoreRequest(consensus.Key));
                    numOfReplicas++;
                }

                if (numOfReplicas == this.NumberOfReplicas)
                {
                    break;
                }
            }
        }

        void ProcessSyncReport()
        {
            var nodeId = (this.ReceivedEvent as Node.SyncReport).NodeId;
            var data = (this.ReceivedEvent as Node.SyncReport).Data;

            if (!this.DataMap.ContainsKey(nodeId))
            {
                this.DataMap.Add(nodeId, 0);
            }

            this.DataMap[nodeId] = data;
        }

        void ProcessFailure()
        {
            var nodeId = (this.ReceivedEvent as Node.NotifyFailure).NodeId;
            this.NodeMap.Remove(nodeId);
            this.DataMap.Remove(nodeId);

            Console.WriteLine("\n [NodeManager] node {0} failed.\n", nodeId);

            this.CreateNewNode();
        }

        #endregion
    }
}
