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
		MachineId periodicTimerModel;
		int k;

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(eTimeOut), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(eCancelSucess), nameof(HandleSuccessfulCancellation))]
		[OnEventDoAction(typeof(eCancelFailure), nameof(HandleFailedCancellation))]
		class Init : MachineState { }

		private void Initialize()
		{
			periodicTimerModel = this.CreateMachine(typeof(PeriodicTimer), new InitTimer(this.Id, 1000));
			this.k = 0;

			// Start the timer
			this.Send(this.periodicTimerModel, new eStartTimer());
			// this.Send(this.periodicTimerModel, new eCancelTimer());

		}

		private void HandleTimeout()
		{
			Console.WriteLine("Client: Timeout received from timer with k: " + k);
			this.k++;

			if(this.k==20)
			{
				this.Send(this.periodicTimerModel, new eCancelTimer());
			}
		}

		private void HandleSuccessfulCancellation()
		{
			Console.WriteLine("Client: Timer canceled successfully");
			this.Raise(new Halt());
		}

		private void HandleFailedCancellation()
		{
			this.Send(this.Id, new MarkupEvent());

			Console.WriteLine("Client: Timer cancellation failed");
			this.Raise(new Halt());
		}
	}
}
