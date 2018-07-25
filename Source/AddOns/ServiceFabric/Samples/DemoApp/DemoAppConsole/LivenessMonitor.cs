using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace DemoAppConsole
{
    class LivenessMonitor : Monitor
    {
        #region monitor events
        public class ePoolManagerMachineUp : Event { }

        public class ePoolManagerMachineDown : Event { }

        public class eUpdateGoalCount : Event
        {
            public int count;

            public eUpdateGoalCount(int count)
            {
                this.count = count;
            }
        }
        #endregion

        #region fields
        // Desired number of pools
        private int goalCount;

        // Number of PoolManagerMachines in existence
        private int poolManagerMachineCount;
        #endregion

        #region monitor states
        [Start]
        [OnEntry(nameof(InitMonitor))]
        class Init : MonitorState { }

        [Cold]
        [OnEventDoAction(typeof(eUpdateGoalCount), nameof(UpdateGoalCount))]
        [OnEventDoAction(typeof(ePoolManagerMachineUp), nameof(IncPoolManagerMachine))]
        [OnEventDoAction(typeof(ePoolManagerMachineDown), nameof(DecPoolManagerMachine))]
        class Balance : MonitorState { }

        [Hot]
        [OnEventDoAction(typeof(eUpdateGoalCount), nameof(UpdateGoalCount))]
        [OnEventDoAction(typeof(ePoolManagerMachineUp), nameof(IncPoolManagerMachine))]
        [OnEventDoAction(typeof(ePoolManagerMachineDown), nameof(DecPoolManagerMachine))]
        class Imbalance : MonitorState { }

        #endregion

        #region handlers
        private void InitMonitor()
        {
            goalCount = 0;
            poolManagerMachineCount = 0;

            this.Goto<Balance>();
        }

        private void UpdateGoalCount()
        {
            eUpdateGoalCount ev = this.ReceivedEvent as eUpdateGoalCount;
            this.goalCount = ev.count;

            if(goalCount != poolManagerMachineCount)
            {
                this.Goto<Imbalance>();
            }
            else
            {
                this.Goto<Balance>();
            }
        }

        private void IncPoolManagerMachine()
        {
            poolManagerMachineCount++;

            if (goalCount != poolManagerMachineCount)
            {
                this.Goto<Imbalance>();
            }
            else
            {
                this.Goto<Balance>();
            }
        }

        private void DecPoolManagerMachine()
        {
            poolManagerMachineCount--;

            if (goalCount != poolManagerMachineCount)
            {
                this.Goto<Imbalance>();
            }
            else
            {
                this.Goto<Balance>();
            }
        }

        #endregion
    }
}
