using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderElection
{
    class LivenessMonitor : Monitor
    {
        #region events
        public class NotifyLeaderElected : Event { }
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class init : MonitorState { }
        [Hot]
        [OnEventGotoState(typeof(NotifyLeaderElected), typeof(LeaderElected))]
        class NoLeaderElected : MonitorState { }

        [Cold]
        class LeaderElected : MonitorState { }
        #endregion

        #region actions
        void InitOnEntry()
        {
            this.Goto(typeof(NoLeaderElected));
        }
        #endregion
    }
}
