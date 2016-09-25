using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler

{
    class RLock_Machine : Machine
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
            public bool Value;

            public ValueResp(bool value)
            {
                this.Value = value;
            }
        }
        public class SetReq : Event
        {
            public MachineId Target;
            public bool Value;

            public SetReq(MachineId target, bool value)
            {
                this.Target = target;
                this.Value = value;
            }
        }
        public class SetResp : Event { }
        #endregion

        #region fields
        private bool r_lock;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitialize))]
        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitialize()
        {
            this.r_lock = false;
        }

        void OnSetReq()
        {
            var e = ReceivedEvent as SetReq;
            this.r_lock = e.Value;
            Send(e.Target, new SetResp());
        }

        void OnValueReq()
        {
            var e = ReceivedEvent as ValueReq;
            Send(e.Target, new ValueResp(r_lock));
        }
        #endregion
    }
}
