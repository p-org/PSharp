//-----------------------------------------------------------------------
// <copyright file="SingleStateMachineTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SingleStateMachineTest : BaseTest
    {
        class E : Event
        {
            public int counter;
            public MachineId Id;

            public E(MachineId id)
            {
                counter = 0;
                Id = id;
            }
            public E(int c, MachineId id)
            {
                counter = c;
                Id = id;
            }
        }

        class M : SingleStateMachine
        {
            int count;
            MachineId sender;

            protected override Task InitOnEntry(Event e)
            {
                count = 1;
                sender = (e as E).Id;
                return Task.CompletedTask;
            }

            protected override Task ProcessEvent(Event e)
            {
                count++;
                return Task.CompletedTask;
            }

            protected override void OnHalt()
            {
                count++;
                this.Runtime.SendEvent(sender, new E(count, this.Id));
            }
        }

        class Harness : SingleStateMachine
        {
            protected override async Task InitOnEntry(Event e)
            {
                var m = this.CreateMachine(typeof(M), new E(this.Id));
                this.Send(m, new E(this.Id));
                this.Send(m, new Halt());
                var r = await this.Receive(typeof(E));
                this.Assert((r as E).counter == 3);
            }
            protected override Task ProcessEvent(Event e)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void TestSingleStateMachine()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });
            var configuration = Configuration.Create();
            configuration.SchedulingIterations = 100;

            AssertSucceeded(configuration, test);
        }

    }
}
