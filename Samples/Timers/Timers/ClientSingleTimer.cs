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
		MachineId singleTimerModel;

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(eTimeOut), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(eCancelSucess), nameof(HandleSuccessfulCancellation))]
		[OnEventDoAction(typeof(eCancelFailure), nameof(HandleFailedCancellation))]
		class Init : MachineState { }

		private void Initialize()
		{
			singleTimerModel = this.CreateMachine(typeof(SingleTimerModel));

			// Initialize the timer. The period is unneccessary since we have a timer model.
			this.Send(this.singleTimerModel, new InitTimer(this.Id));

			// Start the timer
			this.Send(this.singleTimerModel, new eStartTimer());

			// Non-deterministically choose to cancel the timer
			bool choice = this.Random();

			if( choice )
			{
				this.Send(this.singleTimerModel, new eCancelTimer());
			}
		}

		private void HandleTimeout()
		{
			// Console.WriteLine("Client: Timeout received from timer");
			this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyTimeoutReceived());
		}

		private void HandleSuccessfulCancellation()
		{
			// Console.WriteLine("Client: Timer canceled successfully");
			this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyCancelSuccess());
		}

		private void HandleFailedCancellation()
		{
			// For a failed cancellation, a timeout event has already been fired. 
			// Thus, there should be a single timeout event in this machine's queue.
			Object timeout = this.ReceivedEvent;
			this.Assert(timeout.GetType() == typeof(eTimeOut));

			this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyCancelFailure());

			// Console.WriteLine("Client: Timer cancellation failed");
		}

	}
}
