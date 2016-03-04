using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace FailureDetector
{
    internal class Safety : Monitor
    {
        internal class MPing : Event
        {
            public MachineId Sender;

            public MPing(MachineId sender)
                : base()
            {
                this.Sender = sender;
            }
        }

        internal class MPong : Event
        {
            public MachineId Node;

            public MPong(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        Dictionary<MachineId, int> Pending;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(MPing), nameof(MPingAction))]
        [OnEventDoAction(typeof(MPong), nameof(MPongAction))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Pending = new Dictionary<MachineId, int>();
        }

        void MPingAction()
        {
            var sender = (this.ReceivedEvent as MPing).Sender;

            if (!this.Pending.ContainsKey(sender))
            {
                this.Pending[sender] = 0;
            }

            this.Pending[sender] = this.Pending[sender] + 1;
            this.Assert(this.Pending[sender] <= 3, "1");
        }

        void MPongAction()
        {
            var node = (this.ReceivedEvent as MPong).Node;

            this.Assert(this.Pending.ContainsKey(node), "2");
            this.Assert(this.Pending[node] > 0, "3");
            this.Pending[node] = this.Pending[node] - 1;
        }
    }
}
