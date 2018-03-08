using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Timers;

namespace Timers
{
    class Client : Timer
	{
		#region states
		[Start]
		[OnEntry(nameof(InitializeClient))]
		[OnEventDoAction(typeof(eTimeout), nameof(HandleTimeout))]
		class Init : MachineState { }

		#endregion

		#region handlers
		private void InitializeClient()
		{
			Timer.IsTestingMode = true;
			Timer.IsPeriodic = true;
			this.Start();
		}

		private void HandleTimeout()
		{
			Console.WriteLine("Timeout received!");
		}
		#endregion
	}
}
