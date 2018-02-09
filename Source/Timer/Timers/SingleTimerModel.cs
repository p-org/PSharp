using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// Model of a timer, which sends a single timeout event.
	/// </summary>
	public class SingleTimerModel : Machine
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
		[OnEventDoAction(typeof(eCancelTimer), nameof(AttemptCancellation))]
		internal sealed class Active : MachineState { }

		private void SendTimeout()
		{
			this.Send(this.client, new eTimeOut());
			this.Raise(new Halt());
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
