using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace CheckSendEntry
{
    class SendMachine : Machine
    {
        #region events
        #endregion

        #region fields
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            MachineId mid = CreateMachine(typeof(ReceiveMachine));
            ReceiveMachine.eEnterEvt e = new ReceiveMachine.eEnterEvt(4);
            Send(mid, e);
            e.val = 7;
        }
        #endregion
    }
}
