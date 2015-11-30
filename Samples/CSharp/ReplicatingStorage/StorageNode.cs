using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    internal class StorageNode : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the storage node.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public MachineId Environment;
            public MachineId NodeManager;
            public int Id;

            public ConfigureEvent(MachineId env, MachineId manager, int id)
                : base()
            {
                this.Environment = env;
                this.NodeManager = manager;
                this.Id = id;
            }
        }
        
        public class SyncReport : Event
        {
            public int NodeId;
            public int Data;

            public SyncReport(int id, int data)
                : base()
            {
                this.NodeId = id;
                this.Data = data;
            }
        }

        public class SyncRequest : Event
        {
            public int Data;

            public SyncRequest(int data)
                : base()
            {
                this.Data = data;
            }
        }

        public class NotifyFailure : Event
        {
            public int NodeId;

            public NotifyFailure(int id)
                : base()
            {
                this.NodeId = id;
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
        /// The storage node manager.
        /// </summary>
        private MachineId NodeManager;

        /// <summary>
        /// The storage node id.
        /// </summary>
        private int NodeId;

        /// <summary>
        /// The data that this storage node contains.
        /// </summary>
        private int Data;

        /// <summary>
        /// The sync report timer.
        /// </summary>
        private MachineId SyncTimer;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(SyncTimer.Timeout))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.Data = 0;
            this.SyncTimer = this.CreateMachine(typeof(SyncTimer));
            this.Send(this.SyncTimer, new SyncTimer.ConfigureEvent(this.Id));
        }

        void Configure()
        {
            this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
            this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
            this.NodeId = (this.ReceivedEvent as ConfigureEvent).Id;

            Console.WriteLine("\n [StorageNode-{0}] is up and running.\n", this.NodeId);

            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeCreated(this.NodeId));
            this.Send(this.Environment, new Environment.NotifyNode(this.Id));

            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(SyncRequest), nameof(Sync))]
        [OnEventDoAction(typeof(SyncTimer.Timeout), nameof(GenerateSyncReport))]
        [OnEventDoAction(typeof(Environment.FaultInject), nameof(Terminate))]
        class Active : MachineState { }

        void Sync()
        {
            var data = (this.ReceivedEvent as SyncRequest).Data;
            Console.WriteLine("\n [StorageNode-{0}] is syncing with data {1}.\n", this.NodeId, data);
            this.Data += data;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
        }

        void GenerateSyncReport()
        {
            this.Send(this.NodeManager, new SyncReport(this.NodeId, this.Data));
        }

        void Terminate()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeFail(this.NodeId));
            this.Send(this.NodeManager, new NotifyFailure(this.NodeId));
            this.Send(this.SyncTimer, new Halt());
            this.Raise(new Halt());
        }

        #endregion
    }
}
