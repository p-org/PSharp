using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace SingleTimerModel
{
	/// <summary>
	/// A client machine which uses a timer model.
	/// </summary>
    class Client : TMachine
    {
		#region fields
		int count;
		MachineId m1;
		#endregion

		#region states
		[Start]
		[OnEntry(nameof(InitializeClient))]
		[OnEventDoAction(typeof(eTimeout), nameof(HandleTimeout))]
		class Init : MachineState { }

		#endregion

		#region handlers
		private void InitializeClient()
		{
			this.count = 0;

			// Start a periodic timer in test mode
			IsTestingMode = true;
			m1 = StartTimer(true);
			
		}

		private void HandleTimeout()
		{
			eTimeout e = (this.ReceivedEvent as eTimeout);

			count++;
			// For a periodic counter, halt the timer after processing 10 eTimeout events.
			if (count == 10)
			{
				// Stop the timer and flush the queue of this machine of all eTimeout events
				this.StopTimer(m1, true);	
			}
		}
		#endregion
	}
}

