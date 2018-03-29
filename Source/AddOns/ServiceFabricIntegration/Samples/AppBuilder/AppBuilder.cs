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

		ReliableRegister<int> CurrentNumUsers;

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

		ReliableRegister<MachineId> UserMock;

		

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(RegisterUserEvent), nameof(RegisterUser))]
		[OnEventDoAction(typeof(TransferEvent), nameof(InitiateTransfer))]
		[OnEventDoAction(typeof(GetTxStatusDBEvent), nameof(ForwardTxStatusRequest))]
		[OnEventDoAction(typeof(TxDBStatus), nameof(ForwardTxStatusResponse))]
		class Init : MachineState { }
		#endregion

		#region handlers

		private async Task Initialize()
		{
			this.Logger.WriteLine("AppBuilder:Initialize()");
			MachineId userMock = await ReliableCreateMachine(typeof(UserMock), null,
								new UserMockInitEvent(this.Id, await NumUsers.Get(CurrentTransaction)));
			await UserMock.Set(CurrentTransaction, userMock);

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

			// Create the blockchain printer
			MachineId blockchainPrinter = await ReliableCreateMachine(typeof(BlockchainPrinter), null,
						new BlockchainPrinterInitEvent(blockchain));
		}

		private async Task RegisterUser()
		{
			RegisterUserEvent e = this.ReceivedEvent as RegisterUserEvent;

			// Validate unique id
			bool IsIdExists = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.id);
			Assert(!IsIdExists, "Registered id: " + e.id + " is not unique");

			// Add to registered users
			await RegisteredUsers.AddAsync(CurrentTransaction, e.id, e.user);

			int currNumUsers = await CurrentNumUsers.Get(CurrentTransaction);
			await CurrentNumUsers.Set(CurrentTransaction, currNumUsers + 1);
		}

		/// <summary>
		/// Initiate a transaction to transfer either from source acc --> dest acc
		/// </summary>
		/// <returns></returns>
		private async Task InitiateTransfer()
		{
			int currNumUsers = await CurrentNumUsers.Get(CurrentTransaction);
			int numUsers = await NumUsers.Get(CurrentTransaction);

			if (currNumUsers != numUsers)
			{
				return;
			}

			TransferEvent e = this.ReceivedEvent as TransferEvent;

			// Verify if the source and destinate accounts are registered
			bool SourceAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.from);
			bool DestAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.to);

			// Assign a new transaction id
			int txid = await TxId.Get(CurrentTransaction);
			txid++;

			// Set the transaction id
			await TxId.Set(CurrentTransaction, txid);

			if ( !SourceAccRegistered && !DestAccRegistered)
			{
				// send back the txid to the user
				await ReliableSend(await UserMock.Get(CurrentTransaction), new TxIdEvent(txid));

				// record the status of the transaction in the SQLDatabase
				await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction),
									new UpdateTxStatusDBEvent(txid, "aborted"));
				return;
			}

			// send back the txid to the user
			await ReliableSend(await UserMock.Get(CurrentTransaction), new TxIdEvent(txid));

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
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 50);
			CurrentNumUsers = new ReliableRegister<int>(QualifyWithMachineName("CurrNumUsers"), this.StateManager, 0);
			TxId = new ReliableRegister<int>(QualifyWithMachineName("TxId"), this.StateManager, 0);
			StorageBlobMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("StorageBlobMachine"), this.StateManager, null);
			SQLDatabaseMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("SQLDatabaseMachine"), this.StateManager, null);
			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"), this.StateManager, null);
			UserMock = new ReliableRegister<MachineId>(QualifyWithMachineName("UserMock"), this.StateManager, null);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion


	}
}
