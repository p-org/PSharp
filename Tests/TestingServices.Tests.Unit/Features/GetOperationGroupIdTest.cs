//-----------------------------------------------------------------------
// <copyright file="GetOperationGroupIdTest.cs">
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

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GetOperationGroupIdTest : BaseTest
    {
        public GetOperationGroupIdTest(ITestOutputHelper output)
            : base(output)
        { }

        static Guid OperationGroup = Guid.NewGuid();

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
                var id = this.Id.Runtime.GetCurrentOperationGroupId(Id);
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
                var runtime = this.Id.Runtime;
                runtime.SendEvent(Id, new E(Id), OperationGroup);
            }

            void CheckEvent()
            {
                var id = this.Id.Runtime.GetCurrentOperationGroupId(Id);
                Assert(id == OperationGroup, $"OperationGroupId is not '{OperationGroup}', but {id}.");
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
                this.Id.Runtime.GetCurrentOperationGroupId(target);
            }
        }

        class M4 : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestGetOperationGroupIdNotSet()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestGetOperationGroupIdSet()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestGetOperationGroupIdOfNotCurrentMachine()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            string bugReport = "Trying to access the operation group id of 'Microsoft.PSharp.TestingServices.Tests.Unit.GetOperationGroupIdTest+M4()', which is not the currently executing machine.";
            AssertFailed(test, bugReport, true);
        }
    }
}
