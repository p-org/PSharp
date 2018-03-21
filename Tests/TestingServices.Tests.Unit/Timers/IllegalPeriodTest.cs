//-----------------------------------------------------------------------
// <copyright file="IllegalPeriodTest.cs">
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
    public class IllegalPeriodTest : BaseTest
    {
		#region check illegal period specification
		private class T4 : TMachine
		{
			#region fields

			object payload = new object();

			#endregion

			#region states
			[Start]
			[OnEntry(nameof(Initialize))]
			class Init : MachineState { }
			#endregion

			#region handlers
			async Task Initialize()
			{
				// Incorrect period, will throw assertion violation
				TimerId tid = this.StartTimer(payload, true, -1);
				await this.StopTimer(tid, flush: true);
			}
			#endregion
		}
		#endregion

		#region test
		[Fact]
		public void IllegalTimerStopTest()
		{
			var config = Configuration.Create().WithNumberOfIterations(1000);
			config.MaxSchedulingSteps = 200;
			
			var test = new Action<PSharpRuntime>((r) => {
				r.CreateMachine(typeof(T4));
			});
			base.AssertFailed(test, 1, true);
		}
		#endregion
	}
}
