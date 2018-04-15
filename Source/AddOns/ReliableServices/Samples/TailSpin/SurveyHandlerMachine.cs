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
		ReliableRegister<IRsmId> TailSpinCoreMachine;

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
			await TailSpinCoreMachine.Set(e.TailSpinCoreMachine);
			await SurveyDuration.Set(e.SurveyDuration);
			await SubscriberId.Set(e.SubscriberId);
			await SurveyId.Set(e.SurveyId);
	
			// Create the survey duration timer
			await StartTimer("SurveyTimer", await SurveyDuration.Get());

			// Create the survey response timer. Assume responses coming at 10ms intervals.
			await StartTimer("ResponseTimer", 10);
		}

		private async Task HandleTimeout()
		{
			TimeoutEvent e = (this.ReceivedEvent as TimeoutEvent);

			// If timeout is from a response timer, nondeterministically submit a response.
			if(e.Name == "ResponseTimer")
			{
				int response = this.RandomInteger(11);
				if (this.FairRandom())
				{
					await this.ReliableSend(this.ReliableId, new SurveyResponse(response));
				}
			}
			// If the survey time is up, send the survey summary to TailSpinCore machine
			if(e.Name == "SurveyTimer")
			{
				// Stop the timers
				await this.StopTimer("ResponseTimer");
                await this.StopTimer("SurveyTimer");

				// Get the final vote count, which should be non-negative
				int finalVote = await Survey.Get();
				this.Assert(finalVote >= 0);

				// Compile the results and send it back to TailSpinCore
				var tsMachine = await TailSpinCoreMachine.Get();
				int subscriberId = await SubscriberId.Get();
				int surveyId = await SurveyId.Get();
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
			int currentCount = await Survey.Get();
			await Survey.Set(currentCount + e.response);
		}

		#endregion

		#region methods

		protected override Task OnActivate()
		{
			// this.Logger.WriteLine("SurveyHandlerMachine.OnActivate()");
			TailSpinCoreMachine = this.Host.GetOrAddRegister<IRsmId>("TailSpinCoreMachine", null);
			SurveyDuration = this.Host.GetOrAddRegister<int>("SurveyDuration", 0); 
			SubscriberId = this.Host.GetOrAddRegister<int>("SubscriberId", 0);
			SurveyId = this.Host.GetOrAddRegister<int>("SurveyId", 0);
			Survey = this.Host.GetOrAddRegister<int>("SurveyResponse", 0);
			return Task.CompletedTask;
		}

		#endregion
	}
}
