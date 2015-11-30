using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    internal class LivenessMonitor : Monitor
    {
        #region events

        private class LocalEvent : Event { }

        #endregion

        #region fields

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Monitoring))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Raise(new LocalEvent());
        }

        class Monitoring : MonitorState { }

        #endregion
    }
}