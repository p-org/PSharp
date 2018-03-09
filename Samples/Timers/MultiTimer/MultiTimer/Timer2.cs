using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace MultiTimer
{
	class Timer2 : TMachine
	{
		#region fields
		MachineId client;
		#endregion

		#region states
		[Start]
		[OnEntry(nameof(InitTimer2))]
		[OnEventDoAction(typeof(eTimeout), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(StopTimerEvent), nameof(HaltTimer))]
		class Init : MachineState { }

		#endregion

		#region handlers
		private void InitTimer2()
		{
			this.client = (this.ReceivedEvent as InitializeTimer).client;

			// start a periodic timer with interval 2s
			this.StartTimer(false, true, 2000);
		}

		// The timer sends eTimeout to this machine, which then forwards the appropriate event to the client
		private void HandleTimeout()
		{
			this.Send(this.client, new Timeout2());
		}

		private void HaltTimer()
		{
			// Stop the timer machine.
			// Note that only the queue of Timer2 is flushed of eTimeout events. 
			// For a similar flushing of Client, use the flushing logic in Timers.cs
			this.StopTimer(true);
		}
		#endregion
	}
}
