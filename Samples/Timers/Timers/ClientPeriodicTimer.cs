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

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(Unit), nameof(SendCancellation))]
		[OnEventDoAction(typeof(eTimeOut), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(eCancelSucess), nameof(HandleSuccessfulCancellation))]
		[OnEventDoAction(typeof(eCancelFailure), nameof(HandleFailedCancellation))]
		class Init : MachineState { }

		private void Initialize()
		{
			periodicTimerModel = this.CreateMachine(typeof(SingleTimerModel));

			// Initialize the timer. The period is unneccessary since we have a timer model.
			this.Send(this.periodicTimerModel, new InitTimer(this.Id));

			// Start the timer
			this.Send(this.periodicTimerModel, new eStartTimer());

			// Raise Unit event to non-deterministically cancel the timer

		}

		private void SendCancellation()
		{
			// Non-deterministically choose to cancel the timer
			bool choice = this.Random();

			if (choice)
			{
				this.Send(this.periodicTimerModel, new eCancelTimer());
			}
			else
				this.Raise(new Unit());
		}

		private void HandleTimeout()
		{
			Console.WriteLine("Client: Timeout received from timer");
		}

		private void HandleSuccessfulCancellation()
		{
			Console.WriteLine("Client: Timer canceled successfully");
			this.Raise(new Halt());
		}

		private void HandleFailedCancellation()
		{
			this.Send(this.Id, new MarkupEvent());

			Object queuedEvent = this.ReceivedEvent;

			while(queuedEvent!= null && (queuedEvent.GetType() != typeof(MarkupEvent)))
			{
				if (queuedEvent.GetType() == typeof(eTimeOut))
					continue;
				else
					this.Send(this.Id, (Event)queuedEvent);
			}

			Console.WriteLine("Client: Timer cancellation failed");
			this.Raise(new Halt());
		}
	}
}
