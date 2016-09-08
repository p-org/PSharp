using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    class State_Machine : Machine
    {
        #region events
        public class ValueReq : Event
        {
            public MachineId Target;

            public ValueReq(MachineId target)
            {
                this.Target = target;
            }
        }
        public class ValueResp : Event
        {
            public mtype Value;

            public ValueResp(mtype value)
            {
                this.Value = value;
            }
        }
        public class SetReq : Event
        {
            public MachineId Target;
            public mtype Value;

            public SetReq(MachineId target, mtype value)
            {
                this.Target = target;
                this.Value = value;
            }
        }
        public class SetResp : Event { }
        public class Waiting : Event
        {
            public MachineId Target;
            public mtype WaitingOn;

            public Waiting(MachineId target, mtype waitingOn)
            {
                this.Target = target;
                this.WaitingOn = waitingOn;
            }
        }
        public class WaitResp : Event { }
        #endregion

        #region fields
        private mtype State;
        private Dictionary<MachineId, mtype> blockedMachines;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitialize))]
        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
        [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitialize()
        {
            this.State = mtype.Running;
            this.blockedMachines = new Dictionary<MachineId, mtype>();
        }

        void OnSetReq()
        {
            var e = ReceivedEvent as SetReq;
            this.State = e.Value;
            Unblock();
            Send(e.Target, new SetResp());
        }

        void OnValueReq()
        {
            var e = ReceivedEvent as ValueReq;
            Send(e.Target, new ValueResp(State));
        }
        
        void OnWaiting()
        {
            var e = ReceivedEvent as Waiting;
            if (this.State == e.WaitingOn)
            {
                Send(e.Target, new WaitResp());
            }
            else
            {
                blockedMachines.Add(e.Target, e.WaitingOn);
            }
        }
        #endregion

        #region private methods
        void Unblock()
        {
            List<MachineId> remove = new List<MachineId>();
            foreach (var target in this.blockedMachines.Keys)
            {
                if (this.blockedMachines[target] == this.State)
                {
                    Send(target, new WaitResp());
                    remove.Add(target);
                }
            }

            foreach (var key in remove)
            {
                blockedMachines.Remove(key);
            }
        }
        #endregion
    }
}
