using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{
    class TimerModel : Machine
    {
		#region fields

		MachineId client;

		#endregion

		#region internal events

		private class InitTimer : Event
		{
			public MachineId client;

			public InitTimer(MachineId client)
			{
				this.client = client;
			}
		}

		private class Unit : Event { }

		#endregion

		#region states

		[OnEntry(nameof(InitializeTimer))]
		private class Init : MachineState { }

		[OnEntry(nameof(SendTimeout))]
		[OnEventDoAction(typeof(Unit), nameof(SendTimeout))]
		private class Active : MachineState { }
		#endregion

		#region event handlers
		private void InitializeTimer()
		{
			this.client = (this.ReceivedEvent as InitTimer).client;
			this.Goto<Active>();
		}

		private void SendTimeout()
		{
			// If not periodic, send a single timeout event
			if (!Timer.IsPeriodic)
			{
				this.Send(this.client, new eTimeout());
			}
			else
			{
				if (this.Random())
				{
					this.Send(this.client, new eTimeout());
				}
				this.Raise(new Unit());
			}
		}
		#endregion
	}
}
