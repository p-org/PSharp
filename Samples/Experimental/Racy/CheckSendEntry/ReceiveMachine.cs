using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace CheckSendEntry
{
    class ReceiveMachine : Machine
    {
        #region events
        public class eEnterEvt : Event
        {
            public int val;

            public eEnterEvt(int val)
            {
                this.val = val;
            }
        }
        #endregion

        #region fields
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventDoAction(typeof(eEnterEvt), nameof(OnEnterEvt))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            Console.WriteLine("Init done");
        }

        private void OnEnterEvt()
        {
            int a = (this.ReceivedEvent as eEnterEvt).val;
            Console.WriteLine(a);
        }
        #endregion
    }
}
