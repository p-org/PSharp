using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Variant1
{
    class DoneMachine : Machine
    {
        #region events
        public class SetValue : Event
        {
            public int done;
            public SetValue(int done)
            {
                this.done = done;
            }
        }

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
            public int done;
            public GotValue(int done)
            {
                this.done = done;
            }
        }
        public class Increment : Event { }
        #endregion

        #region fields
        int done;
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(SetValue), nameof(OnSetValue))]
        [OnEventDoAction(typeof(GetValue), nameof(OnGetValue))]
        [OnEventDoAction(typeof(Increment), nameof(OnIncrement))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnSetValue()
        {
            done = (ReceivedEvent as SetValue).done;
        }

        void OnGetValue()
        {
            var target = (ReceivedEvent as GetValue).Target;
            Send(target, new GotValue(done));
        }

        void OnIncrement()
        {
            done++;
        }
        #endregion
    }
}
