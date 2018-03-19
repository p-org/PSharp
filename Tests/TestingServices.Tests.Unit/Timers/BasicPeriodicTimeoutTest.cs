using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.Timers;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class BasicPeriodicTimeoutTest : BaseTest
    {
		#region check basic StartTimer/StopTimer
		private class T1 : TMachine
		{
			#region fields

			TimerId tid;
			object payload = new object();
			int count;

			#endregion

			#region states
			[Start]
			[OnEntry(nameof(InitOnEntry))]
			[OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
			class Init : MachineState { }

			#endregion

			#region handlers
			void InitOnEntry()
			{
				count = 0;

				// Start a periodic timer 
				tid = StartTimer(payload, true, 10);

			}

			async Task HandleTimeout()
			{
				count++;
				this.Assert(count <= 100);

				if(count == 100)
				{
					await StopTimer(tid, flush: true);
				}
			}
			#endregion
		}
		#endregion

		#region test
		[Fact]
		public void PeriodicTimeoutTest()
		{
			var config = Configuration.Create().WithNumberOfIterations(100);
			config.MaxSchedulingSteps = 200;
			config.SchedulingStrategy = Utilities.SchedulingStrategy.Portfolio;
			config.RunAsParallelBugFindingTask = true;
			var test = new Action<PSharpRuntime>((r) => {
				r.CreateMachine(typeof(T1));
			});
			base.AssertSucceeded(test);
		}
		#endregion
	}
}
