using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    internal class Environment : Machine
    {
        #region events

        public class NotifyNode : Event
        {
            public MachineId Node;

            public NotifyNode(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        public class NotifyClientRequestHandled : Event { }
        public class FaultInject : Event { }
        private class LocalEvent : Event { }

        #endregion

        #region fields

        private MachineId NodeManager;
        private int NumberOfReplicas;

        private List<MachineId> AliveNodes;
        private int NumberOfFaults;

        private MachineId Client;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Configuring))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.NumberOfReplicas = 3;
            this.NumberOfFaults = 1;
            this.AliveNodes = new List<MachineId>();

            this.CreateMonitor(typeof(LivenessMonitor));
            this.Monitor<LivenessMonitor>(new LivenessMonitor.ConfigureEvent(this.NumberOfReplicas));

            this.NodeManager = this.CreateMachine(typeof(NodeManager));
            this.Client = this.CreateMachine(typeof(Client));

            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(ConfiguringOnInit))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        class Configuring : MachineState { }

        void ConfiguringOnInit()
        {
            this.Send(this.NodeManager, new NodeManager.ConfigureEvent(this.Id, this.NumberOfReplicas));
            this.Send(this.Client, new Client.ConfigureEvent(this.NodeManager));
            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyNode), nameof(UpdateAliveNodes))]
        [OnEventDoAction(typeof(NotifyClientRequestHandled), nameof(InjectFault))]
        class Active : MachineState { }

        void UpdateAliveNodes()
        {
            var node = (this.ReceivedEvent as NotifyNode).Node;
            this.AliveNodes.Add(node);
        }

        void InjectFault()
        {
            if (this.NumberOfFaults == 0)
            {
                return;
            }

            foreach (var node in this.AliveNodes)
            {
                if (this.Random())
                {
                    Console.WriteLine("\n [Environment] injecting fault.\n");

                    this.Send(node, new FaultInject());
                    this.Send(this.NodeManager, new NodeManager.NotifyFailure(node));
                    this.AliveNodes.Remove(node);
                    this.NumberOfFaults--;
                    break;
                }
            }
        }

        #endregion
    }
}
