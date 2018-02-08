using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// Model of a timer, which sends a single timeout event.
	/// </summary>
	public class SingleTimer : Machine
	{
		MachineId client;	// the client with which this timer is registered
		bool timeoutSent;   // keeps track of whether timeout has been fired
		System.Timers.Timer timer;	

		private class Unit : Event { }

		[Start]
		[OnEventDoAction(typeof(InitTimer), nameof(InitializeTimer))]
		internal sealed class Init : MachineState { }

		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).getClientId();
			timer = new System.Timers.Timer();
			this.Goto<Await>();
		}

		[OnEventDoAction(typeof(eCancelTimer), nameof(SucceedCancellation))]
		[OnEventGotoState(typeof(eStartTimer), typeof(Active))]
		internal sealed class Await : MachineState { }

		private void SucceedCancellation()
		{
			this.Send(this.client, new eCancelSucess());
			this.Goto<Await>();
		}

		[IgnoreEvents(typeof(eStartTimer))]
		[OnEntry(nameof(StartActiveState))]
		[OnEventDoAction(typeof(Unit), nameof(SendTimeout))]
		[OnEventDoAction(typeof(eCancelTimer), nameof(AttemptCancellation))]
		internal sealed class Active : MachineState { }

		private void StartActiveState()
		{
			this.Raise(new Unit());
		}

		private void SendTimeout()
		{
			this.Send(this.client, new eTimeOut());
			timer.Elapsed += OnTimedEvent;
		}

		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			this.Send(this.client, new eTimeOut());
			this.timeoutSent = true;
			this.Goto<Await>();
		}

		private void AttemptCancellation()
		{
			if (! this.timeoutSent)
			{
				this.SucceedCancellation();
			}
			else
			{
				this.Send(this.client, new eCancelFailure());
				this.Goto<Await>();
			}
		}
	}
}
