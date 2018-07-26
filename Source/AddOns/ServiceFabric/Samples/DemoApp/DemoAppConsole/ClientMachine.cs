using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;
using PoolServicesContract;

namespace DemoAppConsole
{
    // A mock of the client service, fires requests to the driver
    class ClientMachine : ReliableMachine
    {
        #region fields
        
        // Handle to the driver machine
        ReliableRegister<MachineId> PoolDriver;

        #endregion

        public ClientMachine(IReliableStateManager stateManager)
             : base(stateManager)
        { }

        #region states

        [Start]
        [OnEntry(nameof(InitClient))]
        class Init : MachineState { }

        #endregion

        #region handlers
        private async Task InitClient()
        {
            eInitClient ev = this.ReceivedEvent as eInitClient;

            await PoolDriver.Set(ev.driver);

            // Setup a config
            ePoolDriverConfigChangeEvent evConfigChange = new ePoolDriverConfigChangeEvent();
            evConfigChange.Configuration = new PoolDriverConfig();
            evConfigChange.Configuration.PoolData = new Dictionary<string, int>();

            // Fire off some pool creation/resize requests to the driver
            evConfigChange.Configuration.PoolData.Add("Pool1", PoolDriverMachine.numVMsPerPool);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.eUpdateGoalCount(1));
            Send(await PoolDriver.Get(), evConfigChange);

            // Scale Up
            evConfigChange = new ePoolDriverConfigChangeEvent();
            evConfigChange.Configuration = new PoolDriverConfig();
            evConfigChange.Configuration.PoolData = new Dictionary<string, int>();
            evConfigChange.Configuration.PoolData.Add("Pool1", PoolDriverMachine.numVMsPerPool);
            evConfigChange.Configuration.PoolData.Add("Pool2", PoolDriverMachine.numVMsPerPool);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.eUpdateGoalCount(2));
            Send(await PoolDriver.Get(), evConfigChange);

            // Scale down
            evConfigChange = new ePoolDriverConfigChangeEvent();
            evConfigChange.Configuration = new PoolDriverConfig();
            evConfigChange.Configuration.PoolData = new Dictionary<string, int>();
            evConfigChange.Configuration.PoolData.Add("Pool1", PoolDriverMachine.numVMsPerPool);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.eUpdateGoalCount(1));
            Send(await PoolDriver.Get(), evConfigChange);
        }
        #endregion

        protected override Task OnActivate()
        {
            PoolDriver = this.GetOrAddRegister<MachineId>("ClientPoolDriver" + this.Id.ToString(), null);
            return Task.CompletedTask;
        }


    }
}
