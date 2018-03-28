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

namespace AppBuilder
{
	class Blockchain : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Set of uncommitted transactions
		/// </summary>
		IReliableQueue<TxObject> UncommittedTxPool;

		/// <summary>
		/// Caches the balances of each user.
		/// </summary>
		IReliableDictionary<int, int> Balances;

		/// <summary>
		/// The ledger maps a block id to a set of committed transactions
		/// </summary>
		IReliableDictionary<int, TxBlock> Ledger;
		#endregion

		#region states

		#endregion

		#region handlers

		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public Blockchain(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("Blockchain starting.");

			UncommittedTxPool = await this.StateManager.GetOrAddAsync<IReliableQueue<TxObject>>(QualifyWithMachineName("UncommittedTxPool"));

			Balances = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(QualifyWithMachineName("Balances"));

			Ledger = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, TxBlock>>(QualifyWithMachineName("Ledger"));
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
		#endregion
	}
}
