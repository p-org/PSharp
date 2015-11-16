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

            public NotifyLeaderElected(MachineId leader)
                : base()
            {
                this.Leader = leader;
            }
        }

        internal class NotifyNewFollower : Event
        {
            public MachineId Follower;

            public NotifyNewFollower(MachineId follower)
                : base()
            {
                this.Follower = follower;
            }
        }

        private class LocalEvent : Event { }

        HashSet<MachineId> Leaders;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Monitoring))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Leaders = new HashSet<MachineId>();
            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(NotifyLeaderElected), nameof(ProcessLeaderElected))]
        [OnEventDoAction(typeof(NotifyNewFollower), nameof(ProcessNewFollower))]
        class Monitoring : MonitorState { }

        void ProcessLeaderElected()
        {
            var leader = (this.ReceivedEvent as NotifyLeaderElected).Leader;
            this.Leaders.Add(leader);

            this.Assert(this.Leaders.Count == 1, "Detected " + this.Leaders.Count + " leaders.");
        }

        void ProcessNewFollower()
        {
            var follower = (this.ReceivedEvent as NotifyNewFollower).Follower;

            if (this.Leaders.Contains(follower))
            {
                this.Leaders.Remove(follower);
            }
        }
    }
}