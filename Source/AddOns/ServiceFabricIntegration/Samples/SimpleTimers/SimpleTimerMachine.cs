using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Timers;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace SimpleTimers
{
    /// <summary>
    /// Simple machine to demonstrate the use of reliable timers
    /// </summary>
    class SimpleTimerMachine : ReliableStateMachine
    {
        /// <summary>
        /// Keeps counters
        /// </summary>
        IReliableDictionary<string, int> Counters;

        public SimpleTimerMachine(IReliableStateManager stateManager)
            : base(stateManager) { }

        [Start]
        [OnEntry(nameof(OnEnterA))]
        [OnExit(nameof(OnExitA))]
        [OnEventDoAction(typeof(TimeoutEvent), nameof(OnTimeoutA))]
        class A : MachineState { }

        [OnEntry(nameof(OnEnterB))]
        [OnExit(nameof(OnExitB))]
        [OnEventDoAction(typeof(TimeoutEvent), nameof(OnTimeoutB))]
        class B : MachineState { }

        private void OnEnterA()
        {
            StartTimer("A", 10);
        }

        private void OnExitA()
        {
            StopTimer("A");
        }

        private void OnEnterB()
        {
            StartTimer("B", 10);
        }

        private void OnExitB()
        {
            StopTimer("B");
        }

        private async Task OnTimeoutA()
        {
            this.Assert(CurrentTransaction != null);

            var name = (this.ReceivedEvent as TimeoutEvent).Name;
            var cnt = await Counters.AddOrUpdateAsync(CurrentTransaction, name, 1, (k, v) => v + 1);

            this.Assert(name == this.CurrentState.Name);
            this.Logger.WriteLine("SimpleTimer: Obtained timeout {0} in state A, count = {1}", name, cnt);

            if(cnt == 5)
            {
                this.Goto<B>();
                await Counters.AddOrUpdateAsync(CurrentTransaction, name, 0, (k, v) => 0);
            }
        }

        private async Task OnTimeoutB()
        {
            var name = (this.ReceivedEvent as Microsoft.PSharp.ReliableServices.Timers.TimeoutEvent).Name;
            var cnt = await Counters.AddOrUpdateAsync(CurrentTransaction, name, 1, (k, v) => v + 1);

            this.Assert(name == this.CurrentState.Name);
            this.Logger.WriteLine("SimpleTimer: Obtained timeout {0} in state B, count = {1}", name, cnt);

            if (cnt == 5)
            {
                this.Goto<A>();
                await Counters.AddOrUpdateAsync(CurrentTransaction, name, 0, (k, v) => 0);
            }
        }
        public override async Task OnActivate()
        {
            Counters = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("Counters");
        }
    }
}
