//-----------------------------------------------------------------------
// <copyright file="PeriodicTimerModel.cs">
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

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// Model of a timer, which sends periodic timeout events.
	/// The timer is started with a eStartTimer event, and canceled by eCancelTimer event.
	/// A canceled timer can be restarted.
	/// </summary>
	public class PeriodicTimerModel : Machine
	{
		#region fields
		/// <summary>
		/// The client with which this timer is registered.
		/// </summary>
		MachineId client;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(InitializeTimer))]
		internal sealed class Init : MachineState { }

		/// <summary>
		/// Timer is in quiescent state. Awaiting either eCancelTimer or eStartTimer from the client. 
		/// </summary>		
		[OnEventDoAction(typeof(eCancelTimer), nameof(SucceedCancellation))]
		[OnEventGotoState(typeof(eStartTimer), typeof(Active))]
		internal sealed class Quiescent : MachineState { }

		/// <summary>
		/// Timer has been started with eStartTimer, and can send timeout events.
		/// </summary>
		[IgnoreEvents(typeof(eStartTimer))]
		[OnEventGotoState(typeof(Default), typeof(NonzeroTimeouts), nameof(OnTimedEvent))]
		[OnEventDoAction(typeof(eCancelTimer), nameof(AttemptCancellation))]
		internal sealed class Active : MachineState { }

		/// <summary>
		/// Timer state where at least one timeout event has been sent out.
		/// </summary>
		[OnEventGotoState(typeof(eCancelTimer), typeof(FailedCancellationState), nameof(FailedCancellation))]
		[OnEventGotoState(typeof(Default), typeof(NonzeroTimeouts), nameof(OnTimedEvent))]
		internal sealed class NonzeroTimeouts : MachineState { }

		/// <summary>
		/// State reached after eCancelTimer is received after at least one eTimeout has been sent.
		/// </summary>
		[OnEventDoAction(typeof(eCancelTimer), nameof(FailedCancellation))]
		[OnEventGotoState(typeof(eStartTimer), typeof(Active))]
		internal sealed class FailedCancellationState : MachineState { }

		#endregion

		#region handlers

		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).getClientId();
			this.Goto<Quiescent>();
		}

		private void SucceedCancellation()
		{
			this.Send(this.client, new eCancelSucess());
			this.Goto<NonzeroTimeouts>();
		}

		private void FailedCancellation()
		{
			this.Send(this.client, new eCancelFailure());
		}


		private void OnTimedEvent()
		{
			this.Send(this.client, new eTimeOut());
		}

		private void AttemptCancellation()
		{
			if (this.Random())
			{
				this.Send(this.client, new eTimeOut());
				this.FailedCancellation();
				this.Goto<FailedCancellationState>();
			}

			else
			{
				this.SucceedCancellation();
				this.Goto<Quiescent>();
			}
		}

		#endregion
	}
}
