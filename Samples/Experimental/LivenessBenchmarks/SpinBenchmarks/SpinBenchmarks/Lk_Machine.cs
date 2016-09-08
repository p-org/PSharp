using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    class Lk_Machine : Machine
    {
        #region events
        public class AtomicTestSet : Event
        {
            public MachineId Target;

            public AtomicTestSet(MachineId target)
            {
                this.Target = target;
            }
        }
        public class AtomicTestSet_Resp : Event { }
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
        public class Waiting : Event
        {
            public MachineId Target;
            public bool WaitingOn;

            public Waiting(MachineId target, bool waitingOn)
            {
                this.Target = target;
                this.WaitingOn = waitingOn;
            }
        }
        public class WaitResp : Event { }
        #endregion

        #region fields
        private bool lk;
        private Dictionary<MachineId, bool> blockedMachines;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitialize))]
        [OnEventDoAction(typeof(AtomicTestSet), nameof(OnAtomicTestSet))]
        [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
        [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitialize()
        {
            this.lk = false;
            this.blockedMachines = new Dictionary<MachineId, bool>();
        }

        void OnAtomicTestSet()
        {
            var e = ReceivedEvent as AtomicTestSet;
            if(this.lk == false)
            {
                this.lk = true;
                Unblock();                
            }
            Send(e.Target, new AtomicTestSet_Resp());
        }

        void OnSetReq()
        {
            var e = ReceivedEvent as SetReq;
            this.lk = e.Value;
            Unblock();
            Send(e.Target, new SetResp());
        }

        void OnWaiting()
        {
            var e = ReceivedEvent as Waiting;
            if(this.lk == e.WaitingOn)
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
                if (this.blockedMachines[target] == this.lk)
                {
                    Send(target, new WaitResp());
                    remove.Add(target);
                }
            }

            foreach(var key in remove)
            {
                blockedMachines.Remove(key);
            }
        }
        #endregion
    }
}
