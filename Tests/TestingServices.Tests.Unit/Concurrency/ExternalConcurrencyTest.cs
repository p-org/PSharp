//-----------------------------------------------------------------------
// <copyright file="ExternalConcurrencyTest.cs">
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
    public class ExternalConcurrencyTest : BaseTest
    {
        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                Task task = Task.Run(() => {
                    this.Send(this.Id, new E());
                });
                task.Wait();
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                Task task = Task.Run(() => {
                    this.Random();
                });
                task.Wait();
            }
        }

        [Fact]
        public void TestExternalTaskSendingEvent()
        {
            var test = new Action<IPSharpRuntime>((r) => { r.CreateMachine(typeof(M)); });
            string bugReport = @"Detected task with id '' that is not controlled by the P# runtime.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestExternalTaskInvokingRandom()
        {
            var test = new Action<IPSharpRuntime>((r) => { r.CreateMachine(typeof(N)); });
            string bugReport = @"Detected task with id '' that is not controlled by the P# runtime.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
