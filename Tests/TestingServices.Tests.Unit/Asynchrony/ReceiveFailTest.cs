//-----------------------------------------------------------------------
// <copyright file="ReceiveFailTest.cs">
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
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class ReceiveFailTest : BaseTest
    {
        class E : Event { }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var m2 = this.CreateMachine(typeof(M2));
                this.Send(m2, new E());
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Receive(typeof(E)); // no wait here!
            }
        }
        [Fact]
        public void TestAsyncReceiveEvent()
        {
            var config = Configuration.Create()
                .WithNumberOfIterations(10);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            var bugReport = "Detected an assertion failure.";
            base.AssertFailed(config, test, bugReport);
        }
    }
}
