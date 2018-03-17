using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace Microsoft.PSharp.ReliableServices.Timers
{
    /// <summary>
    /// A mock timer machine
    /// </summary>
    class MockTimerMachine : Machine
    {
        /// <summary>
        /// Client requesting the timeout
        /// </summary>
        MachineId client;

        /// <summary>
        /// Name of the timer
        /// </summary>
        string Name;

        /// <summary>
        /// Has timer fired already
        /// </summary>
        bool TimeoutSent = false;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Unit), nameof(SendTimeout))]
        [OnEventDoAction(typeof(CancelTimer), nameof(OnCancel))]
        class Init : MachineState { }

        class Unit : Event { }
        public class CancelTimer : Event { }
        public class InitTimer : Event
        {
            public MachineId client;
            public string Name;

            public InitTimer(MachineId client, string name)
            {
                this.client = client;
                this.Name = name;
            }
        }

        void InitOnEntry()
        {
            var ev = (this.ReceivedEvent as InitTimer);
            this.client = ev.client;
            this.Name = ev.Name;
            this.TimeoutSent = false;
            SendTimeout();
        }

        void SendTimeout()
        {
            if(TimeoutSent)
            {
                return;
            }

            if (this.Random() && this.FairRandom())
            {
                this.Send(client, new TimeoutEvent(Name));
                TimeoutSent = true;
            }
            else
            {
                this.Send(this.Id, new Unit());
            }
        }

        void OnCancel()
        {
            if(!TimeoutSent)
            {
                this.Send(client, new TimeoutEvent(Name));
                TimeoutSent = true;
            }
        }
    }
}
