using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    internal class LivenessMonitor : Monitor
    {
        #region events

        /// <summary>
        /// Used to configure the liveness monitor.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public int NumberOfReplicas;

            public ConfigureEvent(int numOfReplicas)
                : base()
            {
                this.NumberOfReplicas = numOfReplicas;
            }
        }

        public class NotifyNodeCreated : Event
        {
            public int NodeId;

            public NotifyNodeCreated(int id)
                : base()
            {
                this.NodeId = id;
            }
        }

        public class NotifyNodeFail : Event
        {
            public int NodeId;

            public NotifyNodeFail(int id)
                : base()
            {
                this.NodeId = id;
            }
        }

        public class NotifyNodeUpdate : Event
        {
            public int NodeId;
            public int Data;

            public NotifyNodeUpdate(int id, int data)
                : base()
            {
                this.NodeId = id;
                this.Data = data;
            }
        }

        private class LocalEvent : Event { }

        #endregion

        #region fields

        /// <summary>
        /// Map from node ids to data.
        /// </summary>
        private Dictionary<int, int> DataMap;

        /// <summary>
        /// The number of storage replicas that must
        /// be sustained.
        /// </summary>
        private int NumberOfReplicas;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Repairing))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.DataMap = new Dictionary<int, int>();
        }

        void Configure()
        {
            this.NumberOfReplicas = (this.ReceivedEvent as ConfigureEvent).NumberOfReplicas;
            this.Raise(new LocalEvent());
        }

        [Hot]
        [OnEventDoAction(typeof(NotifyNodeCreated), nameof(ProcessNodeCreated))]
        [OnEventDoAction(typeof(NotifyNodeFail), nameof(ProcessNodeFail))]
        [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(CheckIfRepaired))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Repaired))]
        class Repairing : MonitorState { }

        void ProcessNodeCreated()
        {
            var nodeId = (this.ReceivedEvent as NotifyNodeCreated).NodeId;
            this.DataMap.Add(nodeId, 0);

            if (this.CheckRepair())
            {
                this.Raise(new LocalEvent());
            }
        }

        void ProcessNodeFail()
        {
            var nodeId = (this.ReceivedEvent as NotifyNodeFail).NodeId;
            this.DataMap.Remove(nodeId);
        }

        void CheckIfRepaired()
        {
            var nodeId = (this.ReceivedEvent as NotifyNodeUpdate).NodeId;
            var data = (this.ReceivedEvent as NotifyNodeUpdate).Data;
            this.DataMap[nodeId] = data;

            if (this.CheckRepair())
            {
                this.Raise(new LocalEvent());
            }
        }

        bool CheckRepair()
        {
            var consensus = this.DataMap.Select(kvp => kvp.Value).GroupBy(v => v).
                OrderByDescending(v => v.Count()).FirstOrDefault();

            var numOfReplicas = consensus.Count();
            if (numOfReplicas >= this.NumberOfReplicas)
            {
                return true;
            }

            return false;
        }

        [Cold]
        [OnEventDoAction(typeof(NotifyNodeCreated), nameof(CreateAndCheckRepair))]
        [OnEventDoAction(typeof(NotifyNodeFail), nameof(FailAndCheckRepair))]
        [OnEventDoAction(typeof(NotifyNodeUpdate), nameof(UpdateAndCheckRepair))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Repairing))]
        class Repaired : MonitorState { }

        void CreateAndCheckRepair()
        {
            var nodeId = (this.ReceivedEvent as NotifyNodeCreated).NodeId;

            if (!this.CheckRepair())
            {
                this.Raise(new LocalEvent());
            }
        }

        void FailAndCheckRepair()
        {
            this.ProcessNodeFail();

            if (!this.CheckRepair())
            {
                this.Raise(new LocalEvent());
            }
        }

        void UpdateAndCheckRepair()
        {
            var nodeId = (this.ReceivedEvent as NotifyNodeUpdate).NodeId;
            var data = (this.ReceivedEvent as NotifyNodeUpdate).Data;
            this.DataMap[nodeId] = data;

            if (!this.CheckRepair())
            {
                this.Raise(new LocalEvent());
            }
        }

        #endregion
    }
}