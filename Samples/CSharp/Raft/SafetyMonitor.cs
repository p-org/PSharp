using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class SafetyMonitor : Monitor
    {
        internal class NotifyLeaderElected : Event
        {
            public MachineId Leader;
            public int Term;

            public NotifyLeaderElected(MachineId leader, int term)
                : base()
            {
                this.Leader = leader;
                this.Term = term;
            }
        }

        private class LocalEvent : Event { }

        private int CurrentTerm;
        private HashSet<MachineId> Leaders;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Monitoring))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.CurrentTerm = -1;
            this.Leaders = new HashSet<MachineId>();
            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyLeaderElected), nameof(ProcessLeaderElected))]
        class Monitoring : MonitorState { }

        void ProcessLeaderElected()
        {
            var leader = (this.ReceivedEvent as NotifyLeaderElected).Leader;
            var term = (this.ReceivedEvent as NotifyLeaderElected).Term;

            if (term > this.CurrentTerm)
            {
                this.CurrentTerm = term;
                this.Leaders.Clear();
            }

            this.Leaders.Add(leader);
            this.Assert(this.Leaders.Count == 1, "Detected " + this.Leaders.Count + " leaders.");
        }
    }
}