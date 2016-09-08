using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    class Environment : Machine
    {
        #region fields
        private MachineId client;
        private MachineId server;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInitEntry))]
        class Init : MachineState { }
        #endregion

        #region actions
        void OnInitEntry()
        {
            var lk_machine = CreateMachine(typeof(Lk_Machine));
            var rlock_machine = CreateMachine(typeof(RLock_Machine));
            var rwant_machine = CreateMachine(typeof(RWant_Machine));
            var state_machine = CreateMachine(typeof(State_Machine));
            client = CreateMachine(typeof(Client), new Client.Initialize(lk_machine, rlock_machine, rwant_machine, state_machine));
            server = CreateMachine(typeof(Server), new Server.Initialize(lk_machine, rlock_machine, rwant_machine, state_machine));
        }
        #endregion
    }
}
