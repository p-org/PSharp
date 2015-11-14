using static OperationsExample.Events.SlaveEvents;
using OperationsExample.Events;
using Microsoft.PSharp;
using System.Collections.Generic;

namespace OperationsExample
{
    internal class Slave : Node
    {
        Dictionary<string, string> map = new Dictionary<string, string>();

        [OnEventDoAction(typeof(PerformRead), nameof(ReadOp))]
        [OnEventDoAction(typeof(PerformWrite), nameof(WriteOp))]
        internal class PerformSlaveOperation : MachineState { }

        // TODO: on read I want to get the other's value too and vote. Do I need to change state? Or create other type of event?
        void ReadOp()
        {
            string k = (this.ReceivedEvent as PerformRead).K; // TODO: is there a way to get sender's id easy?

            string v = this.map[k];

            this.Send(this.Master, new MasterEvents.Response(v));
        }

        void WriteOp()
        {
            var e = (this.ReceivedEvent as PerformWrite);
            string k = e.K;
            string v = e.V;

            this.map[k] = v;

            // TODO: broadcast the write. Create a new event type or else inf loop. Can we detect this??
            //foreach (var s in this.Slaves)
            //{
            //    this.Send(s, e);
            //}
        }
    }
}
