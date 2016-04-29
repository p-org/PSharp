using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorExample
{
    public class MyMonitor : Machine
    {
        #region fields
        #endregion

        #region events
        internal class MonitorEvent : Event
        {
            public int data;

            public MonitorEvent(int data)
            {
                this.data = data;
            }
        }
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(MyMonitor), nameof(OnMonitorEvent))]
        private class MonitorState : MachineState { }
        #endregion

        #region actions
        private void OnMonitorEvent()
        {
            var e = (ReceivedEvent as MonitorEvent).data;
            Assert(e == 5);
        }
        #endregion
    }
}
