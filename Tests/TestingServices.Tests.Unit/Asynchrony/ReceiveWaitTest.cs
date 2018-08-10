//-----------------------------------------------------------------------
// <copyright file="ReceiveWaitTest.cs">
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
    public class ReceiveWaitTest : BaseTest
    {
        public ReceiveWaitTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Send(this.Id, new E());
                this.Receive(typeof(E)).Wait();
                this.Assert(false);
            }
        }

        [Fact]
        public void TestAsyncReceiveEvent()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            var bugReport = "Detected an assertion failure.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
