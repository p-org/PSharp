using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Timers;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace TailSpin
{
	/// <summary>
	/// A machine which conducts surveys.
	/// A survey is a sum of responses.
	/// A response is may be a no-show, or a number between 0-10.
	/// Each response is made at 10ms time intervals.
	/// </summary>
	class SurveyHandlerMachine : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Handle to the main TailSpinCore machine.
		/// </summary>
		ReliableRegister<MachineId> TailSpinCoreMachine;

		/// <summary>
		/// How long does a survey last. A default of 10s is assumed.
		/// </summary>
		ReliableRegister<int> SurveyDuration;

		/// <summary>
		/// Reliably store the id of the subscriber who created the survey.
		/// </summary>
		ReliableRegister<int> SubscriberId;

		/// <summary>
		/// Reliably store the id of the survey.
		/// </summary>
		ReliableRegister<int> SurveyId;

		/// <summary>
		/// A reliable accumulator, abstracting a survey as a "number of votes".
		/// </summary>
		ReliableRegister<int> Survey;

		#endregion

		#region states

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(SurveyResponse), nameof(HandleSurveyResponse))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(HandleTimeout))]
		class Init : MachineState { }

		#endregion

		#region handlers

		private async Task Initialize()
		{
			// Initialize all the fields
			SurveyHandlerInitEvent e = (this.ReceivedEvent as SurveyHandlerInitEvent);
			await TailSpinCoreMachine.Set(CurrentTransaction, e.TailSpinCoreMachine);
			await SurveyDuration.Set(CurrentTransaction, e.SurveyDuration);
			await SubscriberId.Set(CurrentTransaction, e.SubscriberId);
			await SurveyId.Set(CurrentTransaction, e.SurveyId);
	
			// Create the survey duration timer
			await StartTimer(QualifyWithMachineName("SurveyTimer"), await SurveyDuration.Get(CurrentTransaction));

			// Create the survey response timer. Assume responses coming at 10ms intervals.
			await StartTimer(QualifyWithMachineName("ResponseTimer"), 10);
		}

		private async Task HandleTimeout()
		{
			TimeoutEvent e = (this.ReceivedEvent as TimeoutEvent);

			// If timeout is from a response timer, nondeterministically submit a response.
			if(e.Name == QualifyWithMachineName("ResponseTimer"))
			{
				int response = this.RandomInteger(11);
				if (this.FairRandom())
				{
					await this.ReliableSend(this.Id, new SurveyResponse(response));
				}
			}
			// If the survey time is up, send the survey summary to TailSpinCore machine
			if(e.Name == QualifyWithMachineName("SurveyTimer"))
			{
				// Stop the timers
				await this.StopTimer(QualifyWithMachineName("ResponseTimer"));
				await this.StopTimer(QualifyWithMachineName("SurveyTimer"));

				// Get the final vote count, which should be non-negative
				int finalVote = await Survey.Get(CurrentTransaction);
				this.Assert(finalVote >= 0);

				// Compile the results and send it back to TailSpinCore
				MachineId tsMachine = await TailSpinCoreMachine.Get(CurrentTransaction);
				int subscriberId = await SubscriberId.Get(CurrentTransaction);
				int surveyId = await SurveyId.Get(CurrentTransaction);
				await this.ReliableSend(tsMachine, new CompletedSurveyEvent(subscriberId, surveyId, finalVote));
			}
		}

		/// <summary>
		/// Increment the accumulator with the received set of votes.
		/// </summary>
		/// <returns></returns>
		private async Task HandleSurveyResponse()
		{
			SurveyResponse e = (this.ReceivedEvent as SurveyResponse);
			int currentCount = await Survey.Get(CurrentTransaction);
			await Survey.Set(CurrentTransaction, currentCount + e.response);
		}

		#endregion

		#region methods

		public SurveyHandlerMachine(IReliableStateManager stateManager) : base(stateManager) { }

		public override Task OnActivate()
		{
			// this.Logger.WriteLine("SurveyHandlerMachine.OnActivate()");
			TailSpinCoreMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("TailSpinCoreMachine"), this.StateManager, null);
			SurveyDuration = new ReliableRegister<int>(QualifyWithMachineName("SurveyDuration"), this.StateManager, 0);
			SubscriberId = new ReliableRegister<int>(QualifyWithMachineName("SubscriberId"), this.StateManager, 0);
			SurveyId = new ReliableRegister<int>(QualifyWithMachineName("SurveyId"), this.StateManager, 0);
			Survey = new ReliableRegister<int>(QualifyWithMachineName("SurveyResponse"), this.StateManager, 0);
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}


		#endregion
	}
}
