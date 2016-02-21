using static OperationsExample.Events.MasterEvents;
using OperationsExample.Events;
using Microsoft.PSharp;

namespace OperationsExample
{
    internal class Master : Node
    {
        private MachineId Client;
        private int rr = 0; // round robin

        [OnEventDoAction(typeof(RegisterClient), nameof(RegistrationOp))]
        [OnEventGotoState(typeof(masterlocal), typeof(PerformMasterOperation))]
        internal class Registration : MachineState { }

        void RegistrationOp()
        {
            this.Client = (this.ReceivedEvent as RegisterClient).client;
            this.Raise(new masterlocal());
        }
 
        [OnEventDoAction(typeof(Read), nameof(ReadOp))]
        [OnEventDoAction(typeof(Write), nameof(WriteOp))]
        [OnEventDoAction(typeof(Response), nameof(ResponseOp))]
        internal class PerformMasterOperation : MachineState { }

        void ReadOp()
        {
            string k = (this.ReceivedEvent as Read).K;

            rr = (rr + 1) % this.Slaves.Count;
            MachineId m = this.Slaves[rr];

            this.Send(m, new SlaveEvents.PerformRead(k));
        }

        void WriteOp()
        {
            var e = (this.ReceivedEvent as Write);
            string k = e.K;
            string v = e.V;

            rr = (rr + 1) % this.Slaves.Count;
            MachineId m = this.Slaves[rr];

            this.Send(m, new SlaveEvents.PerformWrite(k, v));
        }

        void ResponseOp()
        {
            var e = (this.ReceivedEvent as Response);
            this.Send(this.Client, e);
        }
    }
}
