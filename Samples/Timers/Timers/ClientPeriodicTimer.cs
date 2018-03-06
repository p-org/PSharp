using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Timer;

namespace Timers
{
	internal class MarkupEvent : Event { }
	internal class Unit : Event { }

	class ClientPeriodicTimer : Machine
	{
		MachineId periodicTimer;
		int k;

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(eTimeOut), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(eCancelSucess), nameof(HandleSuccessfulCancellation))]
		[OnEventDoAction(typeof(eCancelFailure), nameof(HandleFailedCancellation))]
		class Init : MachineState { }

		private void Initialize()
		{
			periodicTimer = this.CreateMachine(typeof(PeriodicTimerModel), new InitTimer(this.Id, 1000));

			// Start the timer
			this.Send(this.periodicTimer, new eStartTimer());
		}

		private void HandleTimeout()
		{
			this.Monitor<PeriodicSafetyMonitor>(new PeriodicSafetyMonitor.NotifyTimeoutReceived());

			if(this.Random())
			{
				this.Send(this.periodicTimer, new eCancelTimer());
			}
		}

		private void HandleSuccessfulCancellation()
		{
			this.Monitor<PeriodicSafetyMonitor>(new PeriodicSafetyMonitor.NotifyCancelSuccess());
		}

		private void HandleFailedCancellation()
		{
			this.Monitor<PeriodicSafetyMonitor>(new PeriodicSafetyMonitor.NotifyCancelFailure());
		}
		
	}
}
