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
	internal sealed class Unit : Event { }
	/// <summary>
	/// Model of a timer, which sends out periodic timeout events.
	/// </summary>
	public class PeriodicTimerModel : Machine
	{
		MachineId client;   // the client with which this timer is registered

		[Start]
		[OnEventDoAction(typeof(InitTimer), nameof(InitializeTimer))]
		internal sealed class Init : MachineState { }

		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).getClientId();
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
		[OnEventDoAction(typeof(Unit), nameof(SendTimeout))]
		[OnEventDoAction(typeof(eCancelTimer), nameof(AttemptCancellation))]
		internal sealed class Active : MachineState { }

		private void SendTimeout()
		{
			this.Send(this.client, new eTimeOut());
			this.Raise(new Unit());
		}

		private void AttemptCancellation()
		{
			bool choice = this.Random();
			if (choice)
			{
				this.Send(this.client, new eTimeOut());
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
