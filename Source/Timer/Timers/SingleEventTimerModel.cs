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
	public class SingleEventPSTImer : Machine
	{
		MachineId client;

		private class Unit : Event { }

		[Start]
		[OnEventDoAction(typeof(InitTimer), nameof(InitializeTimer))]
		internal sealed class Init : MachineState { }

		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).getClientId();
			this.Goto<Await>();
		}

		[OnEventDoAction(typeof(eCancelTimer), nameof(SucceedCancellation(true))]
		[OnEventGotoState(typeof(eStartTimer), typeof(Active))]
		internal sealed class Await : MachineState { }

		private void SucceedCancelAndHalt()
		{
			this.Send(this.client, new eCancelSucess());
		}

		[IgnoreEvents(typeof(eStartTimer))]
		[OnEntry(nameof(StartActiveState))]
		[OnEventDoAction(typeof(Unit), nameof(SendTimeoutAndHalt))]
		[OnEventDoAction(typeof(eCancelTimer), nameof(AttemptCancelAndHalt))]
		internal sealed class Active : MachineState { }

		private void StartActiveState()
		{
			this.Raise(new Unit());
		}

		private void SendTimeoutAndHalt()
		{
			this.Send(this.client, new eTimeOut());
			this.Goto<Await>();
		}

		private void AttemptCancelAndHalt()
		{
			bool choice = this.Random();

			if(choice)
			{
				this.Send(this.client, new eCancelSucess());
				this.Raise(new Halt());
			}
			else
			{
				this.Send(this.client, new eTimeOut());
				this.Send(this.client, new eCancelFailure());
				this.Raise(new Halt());
			}
		}
	}
}
