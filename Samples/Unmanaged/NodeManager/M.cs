using System;
using System.Collections.Generic;

using Mocking;

using Microsoft.PSharp;

namespace NodeManager
{
    internal class M : Monitor
    {
        int Count;

        [Start]
        [Hot]
        [OnEventDoAction(typeof(Events.NodeCreatedEvent), nameof(ProcessNewNode))]
        [OnEventGotoState(typeof(Events.UnitEvent), typeof(Satisfied))]
        class Unsatisfied : MonitorState { }

        void ProcessNewNode()
        {
            this.Count++;
            if (this.Count == 3)
            {
                this.Raise(new Events.UnitEvent());
            }
        }
        
        [Cold]
        [OnEventDoAction(typeof(Events.FailedEvent), nameof(RemoveNode))]
        [OnEventGotoState(typeof(Events.UnitEvent), typeof(Unsatisfied))]
        class Satisfied : MonitorState { }

        void RemoveNode()
        {
            this.Count--;
            this.Raise(new Events.UnitEvent());
        }
    }
}
