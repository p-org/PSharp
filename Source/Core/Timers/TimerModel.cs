//-----------------------------------------------------------------------
// <copyright file="TimerModel.cs">
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

namespace Microsoft.PSharp.Timers
{
    class TimerModel : Machine
    {
		#region fields

		MachineId client;

		#endregion

		#region internal events

		private class Unit : Event { }

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(InitializeTimer))]
		private class Init : MachineState { }

		[OnEntry(nameof(SendTimeout))]
		[OnEventDoAction(typeof(Unit), nameof(SendTimeout))]
		private class Active : MachineState { }
		#endregion

		#region event handlers
		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).client;
			this.Goto<Active>();
		}

		private void SendTimeout()
		{
			// If not periodic, send a single timeout event
			if (!Timer.IsPeriodic)
			{
				this.Send(this.client, new eTimeout());
			}
			else
			{
				if (this.Random())
				{
					this.Send(this.client, new eTimeout());
				}
				this.Raise(new Unit());
			}
		}
		#endregion
	}
}
