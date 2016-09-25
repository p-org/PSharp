using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    class LivenessMonitor : Monitor
    {
        #region events
        public class NotifyClientSleep : Event { }
        public class NotifyClientProgress : Event { }
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(InitOnEntry))]

        class init : MonitorState { }
        [Hot]
        [OnEventGotoState(typeof(NotifyClientProgress), typeof(Progressing))]
        class Suspended : MonitorState { }

        [Cold]
        [OnEventGotoState(typeof(NotifyClientSleep), typeof(Suspended))]
        class Progressing : MonitorState { }
        #endregion

        #region actions
        void InitOnEntry()
        {
            this.Goto(typeof(Progressing));
        }
        #endregion
    }
}
