using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vNextRepro
{
    class Environment : Machine
    {
        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        [OnEventDoAction(typeof(TimerMachine.TimerTickEvent), nameof(OnTimerTickEvent))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            CreateMachine(typeof(TimerMachine), new TimerMachine.Config(this.Id));
        }
        void OnTimerTickEvent()
        {
            Console.WriteLine("Timer Tick event received");
        }
        #endregion
    }
}
