using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Variant1
{
    class NrLeadersMachine : Machine
    {
        #region events
        public class SetValue : Event
        {
            public int nr_leaders;
            public SetValue(int nr_leaders)
            {
                this.nr_leaders = nr_leaders;
            }
        }
        public class Increment : Event { }
        public class GetValue : Event
        {
            public MachineId Target;
            public GetValue(MachineId target)
            {
                this.Target = target;
            }
        }
        public class GotValue : Event
        {
            public int Value;
            public GotValue(int value)
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
        [OnEventDoAction(typeof(SetValue), nameof(OnSetValue))]
        [OnEventDoAction(typeof(Increment), nameof(OnIncrement))]
        [OnEventDoAction(typeof(GetValue), nameof(OnGetValue))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnSetValue()
        {
            nr_leaders = (ReceivedEvent as SetValue).nr_leaders;
        }

        void OnIncrement()
        {
            nr_leaders++;
        }

        void OnGetValue()
        {
            var e = ReceivedEvent as GetValue;
            Send(e.Target, new GotValue(nr_leaders));
        }
        #endregion
    }
}
