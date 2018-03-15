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
		public static bool IsEmpty = true;

		#region internal events

		internal class NonMachineSubClass { }

		internal class Configure : Event
		{
			public TaskCompletionSource<bool> TCS;

			public Configure(TaskCompletionSource<bool> tcs)
			{
				this.TCS = tcs;
			}
		}

		#endregion

		class T1 : TMachine
		{
			#region fields

			TimerId tid;
			object payload = new object();
			int count;

			#endregion

			#region states

			[Start]
			[OnEntry(nameof(Initialize))]
			[OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
			class Init : MachineState { }

			#endregion

			#region handlers
			private void Initialize()
			{
				count = 1;
				tid = StartTimer(payload, true, 50);
			}

			private async Task HandleTimeout()
			{
				this.Assert(false);
				count++;

				if(count == 10)
				{
					await StopTimer(tid, true);

					// Try to dequeue an event
					Event e = this.ReceivedEvent;

					this.Assert(e != null);
					this.Raise(new Halt());
				}
			}
			#endregion
		}

		[Fact]
		public void TestExceptionOnInvalidTimerType()
		{
			PSharpRuntime runtime = PSharpRuntime.Create();

			Exception ex = Assert.Throws<AssertionFailureException>(() => runtime.SetTimerMachineType(typeof(NonMachineSubClass)));
		}

		[Fact]
		public void TestInboxEmptyOnFlush()
		{
			TimerTests.IsEmpty = true;
			PSharpRuntime runtime = PSharpRuntime.Create();
			Exception ex = Assert.Throws<AssertionFailureException>(() => runtime.CreateMachine(typeof(T1)));
		}



    }
}
