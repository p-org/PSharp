using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorExample
{
    internal class MyMachine : Machine
    {
        #region fields
        #endregion

        #region events
        internal class MachineEvent : Event { }
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(MachineEvent), nameof(OnMachineEvent))]
        private class Init : MachineState { }
        #endregion

        #region events
        private void OnMachineEvent()
        {
            this.Monitor<MyMonitor>(new MyMonitor.MonitorEvent(5));
        }
        #endregion
    }
}
