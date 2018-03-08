using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace Timers
{
    class Client : Timer
	{
		#region fields
		int count;
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
			Timer.IsTestingMode = false;
			Timer.IsPeriodic = true;
			Timer.period = 1000;
			this.StartTimer();
		}

		private void HandleTimeout()
		{
			Console.WriteLine("Timeout received!");
			this.count++;

			if(count == 20)
			{
				this.StopTimer(true);
			}
		}
		#endregion
	}
}
