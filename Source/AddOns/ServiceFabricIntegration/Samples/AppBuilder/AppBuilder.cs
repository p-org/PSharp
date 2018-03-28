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
		/// Monotonically increasing count of number of users registered so far
		/// </summary>
		ReliableRegister<int> NumUsers;

		/// <summary>
		/// Unique transaction id
		/// </summary>
		ReliableRegister<int> TxId;

		/// <summary>
		/// Handle to the storage blob, containing the dapps.
		/// </summary>
		ReliableRegister<MachineId> StorageBlobMachine;

		/// <summary>
		/// Handle to the mock of the SQL Database
		/// </summary>
		ReliableRegister<MachineId> SQLDatabaseMachine;

		ReliableRegister<MachineId> Blockchain;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(UserRegisterEvent), nameof(RegisterUser))]
		[OnEventDoAction(typeof(TransferEvent), nameof(InitiateTransfer))]
		[OnEventDoAction(typeof(GetTxStatusDBEvent), nameof(ForwardTxStatusRequest))]
		[OnEventDoAction(typeof(TxDBStatus), nameof(ForwardTxStatusResponse))]
		class Init : MachineState { }
		#endregion

		#region handlers

		private async Task Initialize()
		{
			this.Logger.WriteLine("AppBuilder:Initialize()");

			// Create the database where transaction statuses are kept
			MachineId sqlDatabase = await ReliableCreateMachine(typeof(SQLDatabaseMock), null,
						new SQLDatabaseInitEvent(this.Id));
			await SQLDatabaseMachine.Set(CurrentTransaction, sqlDatabase);

			// Create the blockchain
			MachineId blockchain = await ReliableCreateMachine(typeof(Blockchain), null);
			await Blockchain.Set(CurrentTransaction, blockchain);

			// Create Storage Blob 
			MachineId storageBlob = await ReliableCreateMachine(typeof(AzureStorageBlobMock), null,
						new StorageBlobInitEvent(blockchain, sqlDatabase));
			await StorageBlobMachine.Set(CurrentTransaction, storageBlob);
		}

		/// <summary>
		/// Raise a registration request with AzureKeyVault
		/// </summary>
		private async Task RegisterUser()
		{
			UserRegisterEvent e = this.ReceivedEvent as UserRegisterEvent;

			int numUsers = await NumUsers.Get(CurrentTransaction);
			numUsers++;

			// Regiser user
			await NumUsers.Set(CurrentTransaction, numUsers);
			await RegisteredUsers.AddAsync(CurrentTransaction, numUsers, e.user);

			// Send registration id back to user
			await this.ReliableSend(e.user, new UserRegisterResponseEvent(numUsers));
		}

		/// <summary>
		/// Initiate a transaction to transfer either from source acc --> dest acc
		/// </summary>
		/// <returns></returns>
		private async Task InitiateTransfer()
		{
			TransferEvent e = this.ReceivedEvent as TransferEvent;

			// Verify if the source and destinate accounts are registered
			bool SourceAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.from);
			bool DestAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.to);
			this.Assert(SourceAccRegistered && DestAccRegistered);

			// Assign a new transaction id
			int txid = await TxId.Get(CurrentTransaction);
			txid++;

			// send the initiator the transaction id
			MachineId initiator = (await RegisteredUsers.TryGetValueAsync(CurrentTransaction, e.from)).Value;
			await ReliableSend(initiator, new TxIdEvent(txid));

			// start the transfer by invoking the appropriate action in StorageBlob
			await ReliableSend(await StorageBlobMachine.Get(CurrentTransaction),
								new StorageBlobTransferEvent(txid, e.from, e.to, e.amount));

			// record the status of the transaction in the SQLDatabase
			await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction),
								new UpdateTxStatusDBEvent(txid, "processing"));
		}

		private async Task ForwardTxStatusRequest()
		{
			GetTxStatusDBEvent e = this.ReceivedEvent as GetTxStatusDBEvent;

			// Forward the TxStatus request to the SQL Database
			await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction), e);
		}

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
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 0);
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
