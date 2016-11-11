using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vNextRepro
{
    class LivenessMonitor : Monitor
    {
        public class MonitorEvent : Event { }

        [Start]
        [OnEventGotoState(typeof(MonitorEvent), typeof(HotState))]
        class Init : MonitorState { }

        [Hot]
        [OnEventGotoState(typeof(MonitorEvent), typeof(HotState))]
        class HotState : MonitorState { }
    }
}
