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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class TimerTests
    {
		#region tests

		[Fact]
		public void TestExceptionOnInvalidTimerType()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();

			Exception ex = Assert.Throws<AssertionFailureException>(() => runtime.SetTimerMachineType(typeof(NonMachineSubClass)));
		}

		[Fact]
		public void TestBasicPeriodicTimerOperation()
		{
			var tcsFail = new TaskCompletionSource<bool>();

			PSharpRuntime runtime = PSharpRuntime.Create();
			runtime.OnFailure += delegate (Exception exception)
			{
				if (!(exception is MachineActionExceptionFilterException))
				{
					tcsFail.SetException(exception);
				}
			};

			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T1), new Configure(tcs, true));
			tcs.Task.Wait();

			AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
			Assert.IsType<AssertionFailureException>(ex.InnerException);

		}

		[Fact]
		public void TestBasicSingleTimerOperation()
		{
			var tcsFail = new TaskCompletionSource<bool>();

			PSharpRuntime runtime = PSharpRuntime.Create();
			runtime.OnFailure += delegate (Exception exception)
			{
				if (!(exception is MachineActionExceptionFilterException))
				{
					tcsFail.SetException(exception);
				}
			};

			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T1), new Configure(tcs, false));
			tcs.Task.Wait();

			AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
			Assert.IsType<AssertionFailureException>(ex.InnerException);
		}

		// Test if the flushing operation works correctly
		[Fact]
		public void TestInboxFlushOperation()
		{
			var tcsFail = new TaskCompletionSource<bool>();

			PSharpRuntime runtime = PSharpRuntime.Create();
			runtime.OnFailure += delegate (Exception exception)
			{
				if (!(exception is MachineActionExceptionFilterException))
				{
					tcsFail.SetException(exception);
				}
			};

			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(FlushingClient), new Configure(tcs, true));
			tcs.Task.Wait();

			AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
			Assert.IsType<AssertionFailureException>(ex.InnerException);
		}
		
		[Fact]
		public void TestIllegalTimerStoppage()
		{
			var tcsFail = new TaskCompletionSource<bool>();

			PSharpRuntime runtime = PSharpRuntime.Create();
			runtime.OnFailure += delegate (Exception exception)
			{
				if (!(exception is MachineActionExceptionFilterException))
				{
					tcsFail.SetException(exception);
				}
			};

			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T2), new Configure(tcs, true));
			tcs.Task.Wait();

			AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
			Assert.IsType<AssertionFailureException>(ex.InnerException);
		}

		[Fact]
		public void CheckIllegalPeriodSpecification()
		{
			var tcsFail = new TaskCompletionSource<bool>();

			PSharpRuntime runtime = PSharpRuntime.Create();
			runtime.OnFailure += delegate (Exception exception)
			{
				if (!(exception is MachineActionExceptionFilterException))
				{
					tcsFail.SetException(exception);
				}
			};

			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T4), new ConfigureWithPeriod(tcs, -1));
			tcs.Task.Wait();

			AggregateException ex = Assert.Throws<AggregateException>(() => tcsFail.Task.Wait());
			Assert.IsType<AssertionFailureException>(ex.InnerException);
		}
		#endregion

	}
}
