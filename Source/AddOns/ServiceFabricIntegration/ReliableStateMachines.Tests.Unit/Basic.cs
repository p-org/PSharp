using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.ReliableServices.Tests.Unit
{
    [TestClass]
    public class BasicTests : BaseTest
    {
        class E : Event { }

        class M : ReliableStateMachine
        {
            public M(IReliableStateManager stateManager)
                : base(stateManager) { }

            IReliableDictionary<int, int> dictionary;
            int count;

            [Start]
            [OnEntry(nameof(Exec))]
            [OnEventDoAction(typeof(E), nameof(Exec))]
            class S1 : MachineState { }

            async Task Exec()
            {
                if(count == -1)
                {
                    count = await dictionary.GetOrAddAsync(CurrentTransaction, 0, 0);
                }

                if(count < 5 )
                {
                    count++;
                    var rcount = await dictionary.AddOrUpdateAsync(CurrentTransaction, 0, 0, (k, v) => v + 1);
                    this.Assert(rcount <= 5);
                    await this.ReliableSend(this.Id, new E());
                }
                else
                {
                    var rcount = await dictionary.TryGetValueAsync(CurrentTransaction, 0);
                    this.Assert(rcount.HasValue && rcount.Value == 5);
                }
            }

            public override async Task OnActivate()
            {
                dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>("dictionary");
                count = await dictionary.GetOrAddAsync(CurrentTransaction, 0, 0);
            }

            public override void OnTxAbort()
            {
                //count = -1;
            }

            public override Task ClearVolatileState()
            {
                dictionary = null;
                count = 0;
                return Task.FromResult(true);
            }
        }

        [TestMethod]
        public void ExactMessageCount()
        {
            AssertSucceeded(runtime =>
            {
                runtime.CreateMachine(typeof(M));
            });
        }

        [TestInit]
        public static void StartTesting()
        {
            //System.Diagnostics.Debugger.Launch();
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            var mockStateManager = new StateManagerMock(runtime);
            runtime.AddMachineFactory(new ReliableStateMachineFactory(mockStateManager, true));
            runtime.CreateMachine(typeof(M));
        }
    }
}
