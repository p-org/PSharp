using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Variant1
{
    public class Leader : Machine
    {
        #region events
        public class SetValue : Event
        {
            public int leader;
            public SetValue(int leader)
            {
                this.leader = leader;
            }
        }
        #endregion

        #region fields
        int leader;
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(SetValue), nameof(OnSetValue))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnSetValue()
        {
            leader = (ReceivedEvent as SetValue).leader;
        }
        #endregion
    }
}
