//-----------------------------------------------------------------------
// <copyright file="BasicSingleTimeoutTest.cs">
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
using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class BasicSingleTimeoutTest : BaseTest
    {
        private class T1 : TimedMachine
        {
            TimerId tid;
            object payload = new object();
            int count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                count = 0;

                // Start a one-off timer.
                tid = StartTimer(payload, 10, false);
            }

            void HandleTimeout()
            {
                count++;
                this.Assert(count == 1);
            }
        }

        [Fact]
        public void SingleTimeoutTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(T1));
            });

            base.AssertSucceeded(test);
        }
    }
}
