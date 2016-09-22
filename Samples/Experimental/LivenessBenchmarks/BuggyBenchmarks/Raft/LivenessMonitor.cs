using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    class LivenessMonitor : Monitor
    {
        #region events
        public class NotifyLeaderElected : Event { }
        #endregion

        #region states
        [Start]
        [Hot]
        [OnEventGotoState(typeof(NotifyLeaderElected), typeof(LeaderElected))]
        class LeaderNotElected : MonitorState { }

        [Cold]
        [OnEventGotoState(typeof(NotifyLeaderElected), typeof(LeaderElected))]
        class LeaderElected : MonitorState { }
        #endregion
    }
}
