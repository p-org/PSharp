using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    class Server : Machine
    {
        #region events
        public class Initialize : Event
        {
            public MachineId Lk_MachineId;
            public MachineId RLock_MachineId;
            public MachineId RWant_MachineId;
            public MachineId State_MachineId;

            public Initialize(MachineId Lk_MachineId, MachineId RLock_MachineId,
                MachineId RWant_MachineId, MachineId State_MachineId)
            {
                this.Lk_MachineId = Lk_MachineId;
                this.RLock_MachineId = RLock_MachineId;
                this.RWant_MachineId = RWant_MachineId;
                this.State_MachineId = State_MachineId;
            }
        }

        public class Wakeup : Event { }
        #endregion

        #region fields
        private MachineId Lk_MachineId;
        private MachineId RLock_MachineId;
        private MachineId RWant_MachineId;
        public MachineId State_MachineId;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitialize))]
        [OnEventDoAction(typeof(Wakeup), nameof(OnWakeup))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitialize()
        {
            var e = ReceivedEvent as Initialize;
            this.Lk_MachineId = e.Lk_MachineId;
            this.RLock_MachineId = e.RLock_MachineId;
            this.RWant_MachineId = e.RWant_MachineId;
            this.State_MachineId = e.State_MachineId;
            Raise(new Wakeup());
        }

        void OnWakeup()
        {
            Send(RLock_MachineId, new RLock_Machine.SetReq(this.Id, false));
            Receive(typeof(RLock_Machine.SetResp));
            Send(Lk_MachineId, new Lk_Machine.Waiting(this.Id, false));
            Receive(typeof(Lk_Machine.WaitResp));
            Send(RWant_MachineId, new RWant_Machine.ValueReq(this.Id));
            var receivedEvent = Receive(typeof(RWant_Machine.ValueResp));
            Console.WriteLine("seriously?? " + (receivedEvent as RWant_Machine.ValueResp).Value);
            if ((receivedEvent as RWant_Machine.ValueResp).Value == true)
            {
                Send(RWant_MachineId, new RWant_Machine.SetReq(this.Id, false));
                Receive(typeof(RWant_Machine.SetResp));

                Send(State_MachineId, new State_Machine.ValueReq(this.Id));
                var receivedEvent1 = Receive(typeof(State_Machine.ValueResp));
                if ((receivedEvent1 as State_Machine.ValueResp).Value == mtype.Wakeme)
                {
                    Send(State_MachineId, new State_Machine.SetReq(this.Id, mtype.Running));
                    Receive(typeof(State_Machine.SetResp));
                }
            }
            Send(this.Id, new Wakeup());
        }
        #endregion
    }
}
