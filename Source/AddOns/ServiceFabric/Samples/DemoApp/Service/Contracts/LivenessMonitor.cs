using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace PoolServicesContract
{
    public class LivenessMonitor : Monitor
    {
        #region monitor events
        public class ePoolManagerMachineUp : Event { }

        public class ePoolManagerMachineDown : Event { }

        public class eVmManagerMachineUp : Event { }

        public class eVmManagerMachineDown : Event { }

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

        // Number of VmManagerMachines in existence
        private int vmManagerMachineCount;
        #endregion

        #region monitor states
        [Start]
        [OnEntry(nameof(InitMonitor))]
        class Init : MonitorState { }

        [Cold]
        [OnEventDoAction(typeof(eUpdateGoalCount), nameof(UpdateGoalCount))]
        [OnEventDoAction(typeof(ePoolManagerMachineUp), nameof(IncPoolManagerMachine))]
        [OnEventDoAction(typeof(ePoolManagerMachineDown), nameof(DecPoolManagerMachine))]
        [OnEventDoAction(typeof(eVmManagerMachineUp), nameof(IncVmManagerMachine))]
        [OnEventDoAction(typeof(eVmManagerMachineDown), nameof(DecVmManagerMachine))]
        class Balance : MonitorState { }

        [Hot]
        [OnEventDoAction(typeof(eUpdateGoalCount), nameof(UpdateGoalCount))]
        [OnEventDoAction(typeof(ePoolManagerMachineUp), nameof(IncPoolManagerMachine))]
        [OnEventDoAction(typeof(ePoolManagerMachineDown), nameof(DecPoolManagerMachine))]
        [OnEventDoAction(typeof(eVmManagerMachineUp), nameof(IncVmManagerMachine))]
        [OnEventDoAction(typeof(eVmManagerMachineDown), nameof(DecVmManagerMachine))]
        class Imbalance : MonitorState { }

        #endregion

        #region handlers
        private void InitMonitor()
        {
            goalCount = 0;
            poolManagerMachineCount = 0;
            vmManagerMachineCount = 0;

            this.Goto<Balance>();
        }

        private bool IsBalanced()
        {
            return (goalCount == poolManagerMachineCount && vmManagerMachineCount == goalCount * PoolDriverMachine.numVMsPerPool);
        }

        private void Transition()
        {
            if (IsBalanced())
            {
                this.Goto<Balance>();
            }
            else
            {
                this.Goto<Imbalance>();
            }
        }

        private void UpdateGoalCount()
        {
            eUpdateGoalCount ev = this.ReceivedEvent as eUpdateGoalCount;
            this.goalCount = ev.count;

            Transition();
        }

        private void IncPoolManagerMachine()
        {
            poolManagerMachineCount++;

            Transition();
        }

        private void DecPoolManagerMachine()
        {
            poolManagerMachineCount--;

            Transition();
        }

        private void IncVmManagerMachine()
        {
            vmManagerMachineCount++;

            Transition();
        }

        private void DecVmManagerMachine()
        {
            vmManagerMachineCount--;

            Transition();
        }

        #endregion
    }
}
