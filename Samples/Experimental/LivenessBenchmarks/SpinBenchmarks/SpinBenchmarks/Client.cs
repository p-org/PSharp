using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    public enum mtype
    {
        Wakeme,
        Running
    }

    class Client : Machine
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

        public class Sleep : Event { }
        public class Progress : Event { }
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
        [OnEventDoAction(typeof(Sleep), nameof(OnSleep))]
        [OnEventDoAction(typeof(Progress), nameof(OnProgress))]
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
            Raise(new Progress());
        }

        void OnSleep()
        {
            Send(Lk_MachineId, new Lk_Machine.AtomicTestSet(this.Id));
            Receive(typeof(Lk_Machine.AtomicTestSet_Resp));
            while (true)
            {
                Send(RLock_MachineId, new RLock_Machine.ValueReq(this.Id));
                var receivedEvent = Receive(typeof(RLock_Machine.ValueResp));
                if((receivedEvent as RLock_Machine.ValueResp).Value == true)
                {
                    Send(RWant_MachineId, new RWant_Machine.SetReq(this.Id, true));
                    Receive(typeof(RWant_Machine.SetResp));
                    Send(State_MachineId, new State_Machine.SetReq(this.Id, mtype.Wakeme));
                    Receive(typeof(State_Machine.SetResp));
                    Send(Lk_MachineId, new Lk_Machine.SetReq(this.Id, false));
                    Receive(typeof(Lk_Machine.SetResp));

                    this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientSleep());

                    Send(State_MachineId, new State_Machine.Waiting(this.Id, mtype.Running));
                    Receive(typeof(State_Machine.WaitResp));

                    this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientProgress());
                }
                else
                {
                    break;
                }
            }

            //TODO: check if this is required (not there in the paper)
            Send(Id, new Progress());
        }

        void OnProgress()
        {
            Send(RLock_MachineId, new RLock_Machine.ValueReq(Id));
            var receivedEvent = Receive(typeof(RLock_Machine.ValueResp));
            this.Assert((receivedEvent as RLock_Machine.ValueResp).Value == false);
            Send(RLock_MachineId, new RLock_Machine.SetReq(this.Id, true));
            Receive(typeof(RLock_Machine.SetResp));
            Send(Lk_MachineId, new Lk_Machine.SetReq(this.Id, false));
            Receive(typeof(Lk_Machine.SetResp));
            Send(Id, new Sleep());
        }
        #endregion
    }

}
