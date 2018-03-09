using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;

namespace MultiTimer
{
	/// <summary>
	/// An example to demonstrate how to associate multiple timers with a client machine.
	/// The idea is to define a "timer" machine T, which uses the timer APIs to send an appropriate event to the client, 
	/// when T receives the eTimeout event.
	/// </summary>
	class Client : Machine
    {
		#region fields
		MachineId t1;
		MachineId t2;
		int count;
		#endregion

		#region states
		[Start]
		[OnEntry(nameof(InitClient))]
		[OnEventDoAction(typeof(Timeout1), nameof(HandleTimeout1))]
		[OnEventDoAction(typeof(Timeout2), nameof(HandleTimeout2))]
		class Init : MachineState { }
		#endregion

		#region handlers
		private void InitClient()
		{
			// create first timer
			t1 = CreateMachine(typeof(Timer1), new InitializeTimer(this.Id));
			t2 = CreateMachine(typeof(Timer2), new InitializeTimer(this.Id));
			count = 1;
		}

		private void HandleTimeout1()
		{
			Console.WriteLine("Timer1 timeout received, count: " + count);
			count++;

			if (count == 20)
			{
				Console.WriteLine("count hit 20, stopping Timer1");
				Send(t1, new StopTimerEvent());
			}
		}

		private void HandleTimeout2()
		{
			Console.WriteLine("Timer2 timeout received, count: " + count);
			count++;

			if(count == 30)
			{
				Console.WriteLine("count hit 30, stopping Timer2");
				Send(t2, new StopTimerEvent());
			}
		}
		#endregion
	}
}
