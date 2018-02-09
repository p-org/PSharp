//-----------------------------------------------------------------------
// <copyright file="PeriodicTimer.cs">
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
using System.Timers;

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// A P# timer, which sends out periodic timeout events.
	/// </summary>
	public class PeriodicTimer : Machine
	{
		MachineId client;   // the client with which this timer is registered
		bool timeoutSent;   // keeps track of whether timeout has been fired
		int period;         // periodicity of the timeout events
		System.Timers.Timer timer;  // use a system timer to fire timeout events

		[Start]
		[OnEventDoAction(typeof(InitTimer), nameof(InitializeTimer))]
		internal sealed class Init : MachineState { }

		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).getClientId();
			this.period = (this.ReceivedEvent as InitTimer).getPeriod();
			this.timeoutSent = false;
			timer = new System.Timers.Timer(this.period);  // default interval of 100ms used here
			timer.Elapsed += OnTimedEvent;
			this.Goto<Await>();
		}

		// Timer is in quiescent state. Awaiting either eCancelTimer or eStartTimer from the client.
		[OnEventDoAction(typeof(eCancelTimer), nameof(SucceedCancellation))]
		[OnEventGotoState(typeof(eStartTimer), typeof(Active))]
		internal sealed class Await : MachineState { }

		private void SucceedCancellation()
		{
			this.Send(this.client, new eCancelSucess());
			this.Raise(new Halt());
		}

		[IgnoreEvents(typeof(eStartTimer))]
		[OnEventDoAction(typeof(Default), nameof(SendTimeout))]
		[OnEventDoAction(typeof(eCancelTimer), nameof(AttemptCancellation))]
		internal sealed class Active : MachineState { }

		private void SendTimeout()
		{
			this.timer.Start();
		}

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			this.Send(this.client, new eTimeOut());
			this.timeoutSent = true;
		}

		private void AttemptCancellation()
		{
			if (this.timeoutSent)
			{

				this.timer.Stop();
				this.Send(this.client, new eCancelFailure());
				this.Raise(new Halt());
			}

			else
			{
				this.SucceedCancellation();
			}
		}
	}
}
