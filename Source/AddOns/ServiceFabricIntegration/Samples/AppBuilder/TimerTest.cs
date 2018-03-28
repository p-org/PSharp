using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.PSharp.ReliableServices.Timers;
using Microsoft.ServiceFabric.Data;

namespace AppBuilder
{
	class TimerTest : ReliableStateMachine
	{
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(HandleTimeout))]
		class Init : MachineState { }

		private async Task Initialize()
		{
			await StartTimer("A", 1000);
		}

		private async Task HandleTimeout()
		{
			this.Logger.WriteLine("Timeout received");
			await StopTimer("A");
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public TimerTest(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override Task OnActivate()
		{
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
	}
}
