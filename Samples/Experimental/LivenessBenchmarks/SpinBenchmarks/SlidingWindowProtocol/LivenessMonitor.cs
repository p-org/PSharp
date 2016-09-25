using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlidingWindowProtocol
{
    class LivenessMonitor : Monitor
    {
        #region events
        public class NotifyRedMessageSent : Event { }
        public class NotifyRedMessageReceived : Event { }
        #endregion

        #region states
        [Start]
        [Cold]
        [OnEventGotoState(typeof(NotifyRedMessageSent), typeof(RedMessageSent))]
        class RedMessageReceived : MonitorState { }

        [Hot]
        [OnEventGotoState(typeof(NotifyRedMessageReceived), typeof(RedMessageReceived))]
        class RedMessageSent : MonitorState { }
        #endregion
    }
}
