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
	
	class AppBuilder : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Models the Azure Key Vault
		/// </summary>
		IReliableDictionary<int, MachineId> RegisteredUsers;

		/// <summary>
		/// Unique transaction id assigned to every transaction
		/// </summary>
		ReliableRegister<int> TxId;

		/// <summary>
		/// Handle to the storage blob, containing the dapps.
		/// </summary>
		IReliableDictionary<int, Object> AzureStorageBlob;

		/// <summary>
		/// Handle to the mock of the SQL Database
		/// </summary>
		ReliableRegister<MachineId> SQLDatabaseMachine;

		/// <summary>
		/// Handle to the actual blockchain machine.
		/// </summary>
		ReliableRegister<MachineId> Blockchain;

		#endregion

		#region states
		[Start]
		[OnEventDoAction(typeof(AppBuilderInitEvent), nameof(Initialize))]
		[OnEventDoAction(typeof(RegisterUserEvent), nameof(RegisterUser))]
		[OnEventDoAction(typeof(TransferEvent), nameof(InitiateTransfer))]
		class Init : MachineState { }

		#endregion

		#region handlers

		/// <summary>
		/// Create the component machines.
		/// </summary>
		/// <returns></returns>
		private async Task Initialize()
		{
			this.Logger.WriteLine("AppBuilder:Initialize()");
			AppBuilderInitEvent e = this.ReceivedEvent as AppBuilderInitEvent;
			
			// Set handle to the database
			await SQLDatabaseMachine.Set(CurrentTransaction, e.sqlDatabase);

			// Set handle to the blockchain
			await Blockchain.Set(CurrentTransaction, e.blockchain);

			// Create the DLT machine 
			MachineId dlt = await ReliableCreateMachine(typeof(DLT), null,
						new DLTInitEvent(e.blockchain, e.sqlDatabase));

			// Create the blockchain printer
			MachineId blockchainPrinter = await ReliableCreateMachine(typeof(BlockchainPrinter), null,
						new BlockchainPrinterInitEvent(e.blockchain));
		}

		/// <summary>
		/// Register a new user. Here, we abstract away any authentication logic.
		/// In production, this would be substituted with a call to the AzureStorageVault service for authentication.
		/// </summary>
		/// <returns></returns>
		private async Task RegisterUser()
		{
			RegisterUserEvent e = this.ReceivedEvent as RegisterUserEvent;

			// Validate unique id
			bool IsIdExists = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.id);
			Assert(!IsIdExists, "Registered id: " + e.id + " is not unique");

			// Add to registered users
			await RegisteredUsers.AddAsync(CurrentTransaction, e.id, e.user);
		}

		/// <summary>
		/// Initiate a transaction to transfer either from source acc --> dest acc
		/// Transfer of ether from A to B is the only operation supported in this sample.
		/// Additional ops can easily be added to AzureStorageBlobMock.
		/// </summary>
		/// <returns></returns>
		private async Task InitiateTransfer()
		{
			TransferEvent e = this.ReceivedEvent as TransferEvent;

			// Verify if the source and destinatation accounts are registered
			bool SourceAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.from);
			bool DestAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.to);

			// Assign a new transaction id
			int txid = await TxId.Get(CurrentTransaction);
			txid++;

			// Set the transaction id
			await TxId.Set(CurrentTransaction, txid);

			// Abort the tx if one of the accounts isn't registered
			if ( !SourceAccRegistered && !DestAccRegistered)
			{
				// send back the txid to the user
				await ReliableSend(e.source, new TxIdEvent(txid));

				// record the status of the transaction in the SQLDatabase
				await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction),
									new UpdateTxStatusDBEvent(txid, "aborted"));
				return;
			}

			// send back the txid to the user
			await ReliableSend(e.source, new TxIdEvent(txid));

			// Create a transaction object
			TxObject tx = new TxObject(txid, e.from, e.to, e.amount);

			// forward the transaction request to the blockchain, which validates and commits it to the ledger.
			await ReliableSend(await Blockchain.Get(CurrentTransaction), new ValidateAndCommitEvent(tx));

			// record the status of the transaction in the SQLDatabase
			await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction),
								new UpdateTxStatusDBEvent(txid, "processing"));

		}

		/// <summary>
		/// An enquiry from the user (UI) about the status of a tx is forwarded to the database machine
		/// </summary>
		/// <returns></returns>
		private async Task ForwardTxStatusRequest()
		{
			GetTxStatusDBEvent e = this.ReceivedEvent as GetTxStatusDBEvent;

			// Forward the TxStatus request to the SQL Database
			await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction), e);
		}

		/// <summary>
		/// Once the tx status is received from the database machine, the response is fwd back to user (UI)
		/// </summary>
		/// <returns></returns>
		private async Task ForwardTxStatusResponse()
		{
			TxDBStatus e = this.ReceivedEvent as TxDBStatus;

			// Forward the TxStatus response from the database to the appropriate user
			await ReliableSend(e.requestFrom, e);
		}

		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public AppBuilder(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("AppBuilder starting.");

			RegisteredUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, MachineId>>(QualifyWithMachineName("RegisteredUsers"));
			TxId = new ReliableRegister<int>(QualifyWithMachineName("TxId"), this.StateManager, 0);
			StorageBlobMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("StorageBlobMachine"), this.StateManager, null);
			SQLDatabaseMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("SQLDatabaseMachine"), this.StateManager, null);
			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"), this.StateManager, null);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
