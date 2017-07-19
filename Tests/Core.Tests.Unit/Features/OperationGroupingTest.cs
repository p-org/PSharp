//-----------------------------------------------------------------------
// <copyright file="OperationGroupingTest.cs">
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


namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class OperationGroupingTest
    {
        static Guid OperationGroup1 = Guid.NewGuid();
        static Guid OperationGroup2 = Guid.NewGuid();

        class E : Event
        {
            public MachineId Id;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var id = this.OperationGroupId;
                Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M2S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                Runtime.SendEvent(Id, new E(), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M4));
            }
        }

        class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var id = this.OperationGroupId;
                Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M6));
                Send(target, new E());
            }
        }

        class M6 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M5S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M6S));
                Runtime.SendEvent(target, new E(), OperationGroup1);
            }
        }

        class M6S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M8));
                Runtime.SendEvent(target, new E(Id), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        class M8 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
                Send((ReceivedEvent as E).Id, new E());
            }
        }

        class M7S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M8S));
                Runtime.SendEvent(target, new E(Id), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup2, $"OperationGroupId is not '{OperationGroup2}', but {id}.");
            }
        }

        class M8S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
                Runtime.SendEvent((ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        class M9S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M10S));
                Runtime.SendEvent(target, new E(Id), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup2, $"OperationGroupId is not '{OperationGroup2}', but {id}.");
            }
        }

        class M10S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var target = CreateMachine(typeof(M11S));
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
                Runtime.SendEvent((ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        class M11S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var id = this.OperationGroupId;
                Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        public void AssertSucceeded(Type machine)
        {
            var runtime = PSharpRuntime.Create();
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            runtime.CreateMachine(machine);

            tcs.Task.Wait(100);
            Assert.False(failed);
        }

        [Fact]
        public void TestOperationGroupingSingleMachineNoSend()
        {
            AssertSucceeded(typeof(M1));
        }

        [Fact]
        public void TestOperationGroupingSingleMachineSend()
        {
            AssertSucceeded(typeof(M2));
        }

        [Fact]
        public void TestOperationGroupingSingleMachineSendStarter()
        {
            AssertSucceeded(typeof(M2S));
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesCreate()
        {
            AssertSucceeded(typeof(M3));
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSend()
        {
            AssertSucceeded(typeof(M5));
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSendStarter()
        {
            AssertSucceeded(typeof(M5S));
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSendBack()
        {
            AssertSucceeded(typeof(M7));
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSendBackStarter()
        {
            AssertSucceeded(typeof(M7S));
        }

        [Fact]
        public void TestOperationGroupingThreeMachinesSendStarter()
        {
            AssertSucceeded(typeof(M9S));
        }
    }
}
