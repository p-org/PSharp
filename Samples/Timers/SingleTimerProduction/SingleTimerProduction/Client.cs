using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace SingleTimerProduction
{
	/// <summary>
	/// A client machine which uses a timer model.
	/// </summary>
	class Client : TMachine
	{
		#region fields
		int count;
		MachineId m;
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

			// Start a periodic timer.
			// this.StartTimer(false) would generate a single timeout event.
			m = this.StartTimer(true, 1000);
		}

		private void HandleTimeout()
		{
			Console.WriteLine("Timeout received!: " + count);
			this.count++;

			if (count == 20)
			{
				// Stop the timer and flush the queue of this machine of all eTimeout events
				this.StopTimer(m, true);
			}
		}
		#endregion
	}
}
