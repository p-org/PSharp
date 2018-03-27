using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Runtime.Serialization;

namespace AppBuilder
{

	class AzureStorageBlobMock : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Store the set of transaction ids which have already been processed.
		/// </summary>
		IReliableDictionary<int, int> TxIdObserved;

		ReliableRegister<MachineId> Blockchain;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		class Init : MachineState { }
		#endregion

		#region handlers
		private async Task Initialize()
		{
			StorageBlobInitEvent e = this.ReceivedEvent as StorageBlobInitEvent;
			await Blockchain.Set(CurrentTransaction, e.blockchain);

		}
		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public AzureStorageBlobMock(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("AzureStorageBlobMock starting.");
			TxIdObserved = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>
							(QualifyWithMachineName("TxIdObserved"));
			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"),
							this.StateManager, null);

		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
