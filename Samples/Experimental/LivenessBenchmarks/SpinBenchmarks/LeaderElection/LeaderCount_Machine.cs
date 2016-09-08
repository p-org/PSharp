using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderElection
{
    class LeaderCount_Machine : Machine
    {
        #region events
        public class UpdateLeadercount : Event { }
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
            public int Value;

            public ValueResp(int value)
            {
                this.Value = value;
            }
        }
        #endregion

        #region fields
        int nr_leaders;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(UpdateLeadercount), nameof(OnUpdateLeaderCount))]
        [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            this.nr_leaders = 0;
        }

        void OnUpdateLeaderCount()
        {
            this.nr_leaders++;
        }

        void OnValueReq()
        {
            var e = ReceivedEvent as ValueReq;
            Send(e.Target, new ValueResp(nr_leaders));
        }
        #endregion
    }
}
