using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewRacy
{
    class GodMachine : Machine
    {
        #region events
        #endregion

        #region fields
        private Data myData;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            for(int i = 0; i < 1000; i++)
            {
                MachineId snd = CreateMachine(typeof(ReceiveMachine));
                myData = new Data(3);
                Send(snd, new ReceiveMachine.eReceive(myData));
                myData = new Data(8);
            }
        }
        #endregion
    }
}
