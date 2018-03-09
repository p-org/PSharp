using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;

namespace MultiTimer
{
	// Timeout event for the first timer
	class Timeout1 : Event { }

	// Timeout event for the second timer
	class Timeout2 : Event { }

	// Initialize timer machines
	class InitializeTimer : Event
	{
		public MachineId client;

		public InitializeTimer(MachineId client)
		{
			this.client = client;
		}
	}

	// Event to stop timers
	class StopTimerEvent : Event { }
}
