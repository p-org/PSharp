//-----------------------------------------------------------------------
// <copyright file="InboxFlushOperationTest.cs">
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
using Microsoft.PSharp.Timers;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class InboxFlushOperationTest : BaseTest
    {
		#region check flushing
		private class FlushingClient : TMachine
		{
			#region fields
			/// <summary>
			/// A dummy payload object received with timeout events.
			/// </summary>
			object payload = new object();

			/// <summary>
			/// Timer used in the Ping State.
			/// </summary>
			TimerId pingTimer;

			/// <summary>
			/// Timer used in the Pong state.
			/// </summary>
			TimerId pongTimer;

			#endregion

			#region states

			/// <summary>
			/// Start the pingTimer and start handling the timeout events from it.
			/// After handling 10 events, stop pingTimer and move to the Pong state.
			/// </summary>
			[Start]
			[OnEntry(nameof(DoPing))]
			[IgnoreEvents(typeof(TimerElapsedEvent))]
			class Ping : MachineState { }

			/// <summary>
			/// Start the pongTimer and start handling the timeout events from it.
			/// After handling 10 events, stop pongTimer and move to the Ping state.
			/// </summary>
			[OnEntry(nameof(DoPong))]
			[OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeoutForPong))]
			class Pong : MachineState { }
			#endregion

			#region event handlers

			private async Task DoPing()
			{
				// Start a periodic timer with timeout interval of 1sec.
				// The timer generates TimerElapsedEvent with 'm' as payload.
				pingTimer = StartTimer(payload, true, 5);
				await this.StopTimer(pingTimer, flush: true);
				this.Goto<Pong>();
			}

			/// <summary>
			/// Handle timeout events from the pongTimer.
			/// </summary>
			private void DoPong()
			{
				// Start a periodic timer with timeout interval of 0.5sec.
				// The timer generates TimerElapsedEvent with 'm' as payload.
				pongTimer = StartTimer(payload, false, 50);
			}

			private void HandleTimeoutForPong()
			{
				TimerElapsedEvent e = (this.ReceivedEvent as TimerElapsedEvent);
				this.Assert(e.Tid == this.pongTimer);
			}
			#endregion
		}
		#endregion

		#region test
		[Fact]
		public void InboxFlushTest()
		{
			var config = Configuration.Create().WithNumberOfIterations(100);
			config.MaxSchedulingSteps = 200;
			config.SchedulingStrategy = Utilities.SchedulingStrategy.Portfolio;
			config.RunAsParallelBugFindingTask = true;
			var test = new Action<PSharpRuntime>((r) => {
				r.CreateMachine(typeof(FlushingClient));
			});
			base.AssertSucceeded(test);
		}
		#endregion

	}
}
