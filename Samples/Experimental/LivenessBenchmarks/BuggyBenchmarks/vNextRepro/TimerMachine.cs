using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vNextRepro
{
    class TimerMachine : Machine
    {
        #region events
        public class Config : Event
        {
            public MachineId Target;
            public Config(MachineId target)
            {
                this.Target = target;
            }
        }
        public class TimerTickEvent : Event { }
        class Unit : Event { }
        #endregion

        #region fields
        MachineId Target;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

        [OnEntry(nameof(ProcessTickEvent))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Active: MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var e = ReceivedEvent as Config;
            Target = e.Target;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.MonitorEvent());
            Raise(new Unit());
        }
        void ProcessTickEvent()
        {
            if (this.FairRandom())
            {
                this.Send(this.Target, new TimerTickEvent());
            }
            
            this.Raise(new Unit());
        }
        #endregion
    }
}
