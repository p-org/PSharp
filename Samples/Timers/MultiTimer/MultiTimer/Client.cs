using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace MultiTimer
{
	/// <summary>
	/// An example to demonstrate how to associate multiple timers with a client machine.
	/// The idea is to define a "timer" machine T, which uses the timer APIs to send an appropriate event to the client, 
	/// when T receives the eTimeout event.
	/// </summary>
	class Client : TMachine
    {
		#region fields
		MachineId t1;
		MachineId t2;
		int count;
		#endregion

		#region states
		[Start]
		[OnEntry(nameof(InitClient))]
		[OnEventDoAction(typeof(eTimeout), nameof(HandleTimeout))]
		class Init : MachineState { }
		#endregion

		#region handlers
		private void InitClient()
		{
			// create two production timers
			IsTestingMode = false;
			t1 = this.StartTimer(true, 1000);	// periodic timer with 1s periodicity
			t2 = this.StartTimer(true, 500);	// periodic timer with .5s periodicity
			count = 1;
		}

		private void HandleTimeout()
		{
			eTimeout e = this.ReceivedEvent as eTimeout;

			if (e.id == t1)
			{
				Console.WriteLine("Timer 1 timeout processed");
			}
			if (e.id == t2)
			{
				Console.WriteLine("Timer 2 timeout processed");
			}
			count++;

			if (count == 20)
			{
				this.StopTimer(t1, true);
				this.StopTimer(t2, true);
			}
		}
		#endregion
	}
}
