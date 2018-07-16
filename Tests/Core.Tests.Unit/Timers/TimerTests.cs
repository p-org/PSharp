//-----------------------------------------------------------------------
// <copyright file="TimerTests.cs">
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
using Microsoft.PSharp.Runtime;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class TimerTests
    {
        // Test to check assertion failure when attempting to create a timer whose type does not extend Machine
        [Fact]
        public void ExceptionOnInvalidTimerTypeTest()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            Exception ex = Assert.Throws<AssertionFailureException>(() => runtime.SetTimerMachineType(typeof(NonMachineSubClass)));
        }

        // Check basic functions of a periodic timer.
        [Fact]
        public async Task BasicPeriodicTimerOperationTest()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(T1), new Configure(tcs, true));
            var result = await tcs.Task;
            Assert.True(result);
        }

        [Fact]
        public async Task BasicSingleTimerOperationTest()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(T1), new Configure(tcs, false));
            var result = await tcs.Task;
            Assert.True(result);
        }

        // Test if the flushing operation works correctly
        [Fact]
        public async Task InboxFlushOperationTest()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(FlushingClient), new Configure(tcs, true));
            var result = await tcs.Task;
            Assert.True(result);
        }

        [Fact]
        public async Task IllegalTimerStoppageTest()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(T2), new Configure(tcs, true));
            var result = await tcs.Task;
            Assert.True(result);
        }

        [Fact]
        public async Task IllegalPeriodSpecificationTest()
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(T4), new ConfigureWithPeriod(tcs, -1));
            var result = await tcs.Task;
            Assert.True(result);
        }
    }
}
