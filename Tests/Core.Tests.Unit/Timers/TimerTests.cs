// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------


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

		// Test to check assertion failure when attempting to create a timer whose type does not extend Machine
		[Fact]
		public void ExceptionOnInvalidTimerTypeTest()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();

			Exception ex = Assert.Throws<AssertionFailureException>(() => runtime.SetTimerMachineType(typeof(NonMachineSubClass)));
		}

		// Check basic functions of a periodic timer.
		[Fact]
		public async Task BasicPeriodicTimerOperationTest()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();
			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T1), new Configure(tcs, true));
			var result = await tcs.Task;
			Assert.True(result);
		}

		[Fact]
		public async Task BasicSingleTimerOperationTest()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();
			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T1), new Configure(tcs, false));
			var result = await tcs.Task;
			Assert.True(result);
		}

		// Test if the flushing operation works correctly
		[Fact]
		public async Task InboxFlushOperationTest()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();
			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(FlushingClient), new Configure(tcs, true));
			var result = await tcs.Task;
			Assert.True(result);
		}
		
		[Fact]
		public async Task IllegalTimerStoppageTest()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();
			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T2), new Configure(tcs, true));
			var result = await tcs.Task;
			Assert.True(result);
		}

		[Fact]
		public async Task IllegalPeriodSpecificationTest()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();
			var tcs = new TaskCompletionSource<bool>();
			runtime.CreateMachine(typeof(T4), new ConfigureWithPeriod(tcs, -1));
			var result = await tcs.Task;
			Assert.True(result);
		}
		#endregion

	}
}
