using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.Timers;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class IllegalTimerStoppageTest : BaseTest
    {
		#region internal events
		private class TransferTimer : Event
		{
			public TimerId tid;

			public TransferTimer(TimerId tid)
			{
				this.tid = tid;
			}
		}
		#endregion

		#region check illegal timer stoppage
		private class T2 : TMachine
		{
			#region fields

			TimerId tid;
			object payload = new object();
			MachineId m;

			#endregion
			[Start]
			[OnEntry(nameof(Initialize))]
			[IgnoreEvents(typeof(TimerElapsedEvent))]
			class Init : MachineState { }
			#region states

			#endregion

			#region handlers
			void Initialize()
			{
				tid = this.StartTimer(this.payload, true, 100);
				m = CreateMachine(typeof(T3), new TransferTimer(tid));
				this.Raise(new Halt());
			}

			#endregion
		}

		private class T3 : TMachine
		{
			#region states

			[Start]
			[OnEntry(nameof(Initialize))]
			class Init : MachineState { }

			#endregion

			#region handlers

			async Task Initialize()
			{
				TimerId tid = (this.ReceivedEvent as TransferTimer).tid;
	
				// trying to stop a timer created by a different machine. 
				// should throw an assertion violation
				await this.StopTimer(tid, true);
				this.Raise(new Halt());
			}
			#endregion
		}
		#endregion

		#region test
		[Fact]
		public void IllegalTimerStopTest()
		{
			var config = Configuration.Create().WithNumberOfIterations(100);
			config.MaxSchedulingSteps = 200;
			config.SchedulingStrategy = Utilities.SchedulingStrategy.Portfolio;
			config.RunAsParallelBugFindingTask = true;
			var test = new Action<PSharpRuntime>((r) => {
				r.CreateMachine(typeof(T2));
			});
			base.AssertFailed(test, 1, true);
		}
		#endregion
	}
}
