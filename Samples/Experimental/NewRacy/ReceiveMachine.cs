using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewRacy
{
    class ReceiveMachine : Machine
    {
        #region events
        public class eReceive : Event
        {
            public Data mData;
            public eReceive(Data mData)
            {
                this.mData = mData;
            }
        }
        #endregion

        #region fields
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(eReceive), nameof(OnReceive))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnReceive()
        {
            Data received = (this.ReceivedEvent as eReceive).mData;
            Console.WriteLine(received.val);
        }
        #endregion
    }
}
