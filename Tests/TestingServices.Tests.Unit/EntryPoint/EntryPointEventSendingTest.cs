//-----------------------------------------------------------------------
// <copyright file="EntryPointEventSendingTest.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class EntryPointEventSendingTest : BaseTest
    {
        class Transfer : Event
        {
            public int Value;

            public Transfer(int value)
            {
                this.Value = value;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Transfer), nameof(HandleTransfer))]
            class Init : MachineState { }

            void HandleTransfer()
            {
                int value = (this.ReceivedEvent as Transfer).Value;
                this.Assert(value > 0, "Value is 0.");
            }
        }

        [Fact]
        public void TestEntryPointEventSending()
        {
            var test = new Action<PSharpRuntime>((r) => {
                MachineId m = r.CreateMachine(typeof(M));
                r.SendEvent(m, new Transfer(0));
            });

            var bugReport = "Value is 0.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
