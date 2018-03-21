//-----------------------------------------------------------------------
// <copyright file="BasicPeriodicTimeoutTest.cs">
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
    public class BasicPeriodicTimeoutTest : BaseTest
    {
		#region check basic StartTimer/StopTimer
		private class T1 : TimedMachine
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
				tid = StartTimer(payload, 10, true);

			}

			async Task HandleTimeout()
			{
				count++;
				this.Assert(count <= 10);

				if(count == 10)
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
			var config = Configuration.Create().WithNumberOfIterations(1000);
            ModelTimerMachine.NumStepsToSkip = 1;

			var test = new Action<PSharpRuntime>((r) => {
				r.CreateMachine(typeof(T1));
			});
			base.AssertSucceeded(test);
		}
		#endregion
	}
}
