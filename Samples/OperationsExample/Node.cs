using Microsoft.PSharp;
using OperationsExample.Events;
using System.Collections.Generic;

namespace OperationsExample
{
    internal abstract class Node : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> ClusterNodes;
            public int id;

            public Config(int id, List<MachineId> nodes)
                : base(-1, -1)
            {
                this.ClusterNodes = nodes;
                this.id = id;
            }
        }

        protected MachineId Master;
        protected List<MachineId> Slaves;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(MasterEvents.masterlocal), typeof(Master.Registration))]
        [OnEventGotoState(typeof(SlaveEvents.slavelocal), typeof(Slave.PerformSlaveOperation))]
        protected class Init : MachineState { }

        protected void Configure()
        {
            Config c = (this.ReceivedEvent as Config);
            c.ClusterNodes.Remove(this.Id);
            
            if (c.id == 0)
            {
                this.Slaves = c.ClusterNodes;
                this.Master = this.Id;
                this.Raise(new MasterEvents.masterlocal());
            }
            else
            {
                this.Master = c.ClusterNodes[0];
                c.ClusterNodes.RemoveAt(0);
                this.Slaves = c.ClusterNodes;
                this.Raise(new SlaveEvents.slavelocal());
            }
        }
    }
}
