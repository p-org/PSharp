using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Timer;

namespace Timers
{

	/// <summary>
	/// Create a client machine which uses a P# single-timeout machine.
	/// </summary>
	class ClientSingleTimer : Machine  
	{
		MachineId singleTimer;

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(eTimeOut), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(eCancelSucess), nameof(HandleSuccessfulCancellation))]
		[OnEventDoAction(typeof(eCancelFailure), nameof(HandleFailedCancellation))]
		class Init : MachineState { }

		private void Initialize()
		{
			singleTimer = this.CreateMachine(typeof(SingleTimerModel), new InitTimer(this.Id));

			// Start the timer
			this.Send(this.singleTimer, new eStartTimer());

			// Non-deterministically cancel the timer
			if(this.Random())
			{
				this.Send(this.singleTimer, new eCancelTimer());
			}

		}

		private void HandleTimeout()
		{
			this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyTimeoutReceived());
		}

		private void HandleSuccessfulCancellation()
		{
			this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyCancelSuccess());
		}

		private void HandleFailedCancellation()
		{
			// For a failed cancellation, a timeout event has already been fired. 
			// Thus, there should be a single timeout event in this machine's queue.
			
			this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyCancelFailure());
		}

	}
}
