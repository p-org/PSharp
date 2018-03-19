using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
	public class BasicSingleTimeoutTest : BaseTest
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

				// Start a one-off timer 
				tid = StartTimer(payload, false, 10);
				
			}

			void HandleTimeout()
			{
				count++;
				this.Assert(count == 1);
			}
			#endregion
		}
		#endregion

		#region test
		[Fact]
		public void SingleTimeoutTest()
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
