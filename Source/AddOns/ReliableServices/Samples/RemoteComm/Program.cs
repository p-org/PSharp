using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;

namespace RemoteComm
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();

            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            var config = Configuration.Create(); //.WithVerbosityEnabled(2);
            var clientRuntime = PSharpRuntime.Create(config);
            var origHost = RsmHost.Create(stateManager, "ThisPartition", config);
            origHost.ReliableCreateMachine<M1>(new RsmInitEvent()).Wait();

            Console.ReadLine();

        }
    }

    class M1 : ReliableStateMachine
    {

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        protected async Task InitOnEntry()
        {
            var mid = await this.ReliableCreateMachine<M2>("OtherPartition");

            for(int i = 0; i < 5; i++)
            {
                this.Logger.WriteLine("Machine {0} sending payload {1}", this.Id, i);
                await this.ReliableSend(mid, new E(i));
            }

            this.Logger.WriteLine("Machine {0} sending payload {1}", this.Id, "End");
            await this.ReliableSend(mid, new End());
        }

        protected override Task OnActivate()
        {
            return Task.CompletedTask;
        }
    }

    class M2 : ReliableStateMachine
    {
        ReliableRegister<HashSet<int>> RR;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(E), nameof(OnE))]
        [OnEventDoAction(typeof(End), nameof(OnEnd))]
        class Init : MachineState { }

        protected void InitOnEntry()
        {
            this.Assert(this.ReliableId.PartitionName == "OtherPartition");
        }

        protected async Task OnE()
        {
            var v = (this.ReceivedEvent as E).v;
            var set = await RR.Get();

            this.Logger.WriteLine("Machine {0} got payload {1}", this.Id, v);

            this.Assert(set.Count == v);
            this.Assert(!set.Contains(v));

            var nset = new HashSet<int>(set);
            nset.Add(v);

            await RR.Set(nset);
        }

        protected async Task OnEnd()
        {
            this.Logger.WriteLine("Machine {0} got payload {1}", this.Id, "End");

            this.Monitor<Safety>(new End());
            var set = await RR.Get();
            this.Assert(set.Count == 5);
        }

        protected override Task OnActivate()
        {
            RR = this.Host.GetOrAddRegister<HashSet<int>>("RR", new HashSet<int>());
            return Task.CompletedTask;
        }
    }

    class Safety : Monitor
    {
        [Start]
        [Hot]
        [OnEventGotoState(typeof(End), typeof(F))]
        class Init : MonitorState { }

        [Cold]
        class F : MonitorState { }
    }

    class E : Event
    {
        public int v;

        public E(int a)
        {
            v = a;
        }
    }

    class End : Event { }
}
