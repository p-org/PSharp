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
	#region machine initialization

	/// <summary>
	/// Initialize the blockchain.
	/// </summary>
	[DataContract]
	class BlockchainInitEvent : Event
	{
		/// <summary>
		/// Handle to the blockchain machine.
		/// </summary>
		[DataMember]
		public MachineId dlt;

		public BlockchainInitEvent(MachineId dlt)
		{
			this.dlt = dlt;
		}
	}

	[DataContract]
	class DLTInitEvent : Event
	{
		[DataMember]
		public MachineId blockchain;

		[DataMember]
		public MachineId sqldb;

		public DLTInitEvent(MachineId blockchain, MachineId sqldb)
		{
			this.blockchain = blockchain;
			this.sqldb = sqldb;
		}
	}

	/// <summary>
	/// Initialize the sql database machine with a handle back to AppBuilder.
	/// </summary>
	[DataContract]
	class SQLDatabaseInitEvent : Event
	{
		[DataMember]
		public MachineId appBuilderMachine;

		public SQLDatabaseInitEvent(MachineId appBuilderMachine)
		{
			this.appBuilderMachine = appBuilderMachine;
		}
	}

	/// <summary>
	/// Initialize the machine mocking a bunch of users interacting with AppBuilder.
	/// </summary>
	[DataContract]
	class UserMockInitEvent : Event
	{
		[DataMember]
		public MachineId appBuilderMachine;

		[DataMember]
		public MachineId sqlDb;

		[DataMember]
		public int numUsers;

		public UserMockInitEvent(MachineId appBuilderMachine, MachineId sqlDb, int numUsers)
		{
			this.appBuilderMachine = appBuilderMachine;
			this.sqlDb = sqlDb;
			this.numUsers = numUsers;
		}
	}

	/// <summary>
	/// Initialize AppBuilder
	/// </summary>
	[DataContract]
	class AppBuilderInitEvent : Event
	{
		[DataMember]
		public MachineId blockchain;

		[DataMember]
		public MachineId sqlDatabase;

		public AppBuilderInitEvent(MachineId blockchain, MachineId sqlDatabase)
		{
			this.blockchain = blockchain;
			this.sqlDatabase = sqlDatabase;
		}
	}

	/// <summary>
	/// Initialize BlockchainPrinter with handle to the blockchain.
	/// </summary>
	[DataContract]
	class BlockchainPrinterInitEvent : Event
	{
		[DataMember]
		public MachineId blockchain;

		public BlockchainPrinterInitEvent(MachineId blockchain)
		{
			this.blockchain = blockchain;
		}
	}

	#endregion

	#region user registration 

	/// <summary>
	/// Issued by user to register himself/herself with AppBuilder.
	/// </summary>
	[DataContract]
	class RegisterUserEvent : Event
	{
		[DataMember]
		public int id;

		[DataMember]
		public MachineId user;

		public RegisterUserEvent(int id, MachineId user)
		{
			this.id = id;
			this.user = user;
		}
	}

	#endregion

	#region transaction

	/// <summary>
	/// Raise a request to transfer ether from one registered account to another
	/// </summary>
	[DataContract]
	class TransferEvent : Event
	{
		/// <summary>
		/// ID of account initiating transfer
		/// </summary>
		[DataMember]
		public int from;

		/// <summary>
		/// ID to which transfer is to be made
		/// </summary>
		[DataMember]
		public int to;

		/// <summary>
		/// Amount of ether to be transferred
		/// </summary>
		[DataMember]
		public int amount;

		/// <summary>
		/// Machine initiating the transfer request
		/// </summary>
		[DataMember]
		public MachineId source;

		public TransferEvent(int from, int to, int amount, MachineId source)
		{
			this.from = from;
			this.to = to;
			this.amount = amount;
			this.source = source;
		}
	}

	/// <summary>
	/// AppBuilder returns the unique transaction id of the new transaction to the user.
	/// </summary>
	[DataContract]
	class TxIdEvent : Event
	{
		[DataMember]
		public int txid;

		public TxIdEvent(int txid)
		{
			this.txid = txid;
		}
	}


	#endregion

	#region validation events

	/// <summary>
	/// Request from StorageBlob to the Blockchain to validate and commit a tx.
	/// </summary>
	[DataContract]
	class ValidateAndCommitEvent : Event
	{
		[DataMember]
		public TxObject e;

		public ValidateAndCommitEvent(TxObject e)
		{
			this.e = e;
		}
	}

	/// <summary>
	/// Response from Blockchain --> StorageBlob with the status of a tx.
	/// </summary>
	[DataContract]
	class ValidateAndCommitResponseEvent : Event
	{
		[DataMember]
		public int txid;

		[DataMember]
		public bool validation;

		public ValidateAndCommitResponseEvent(int txid, bool validation)
		{
			this.txid = txid;
			this.validation = validation;
		}
	}

	#endregion

	#region database

	/// <summary>
	/// Updates the status of a tx in the database. 
	/// Status \in {processing, committed, aborted}
	/// </summary>
	class UpdateTxStatusDBEvent : Event
	{
		/// <summary>
		/// Transaction id
		/// </summary>
		public int txid;

		/// <summary>
		/// Status of transaction
		/// </summary>
		public string status;

		public UpdateTxStatusDBEvent(int txid, string status)
		{
			this.txid = txid;
			this.status = status;
		}

	}
	
	/// <summary>
	/// Request for the current status of a transaction.
	/// </summary>
	[DataContract]
	class GetTxStatusDBEvent : Event
	{
		[DataMember]
		public int txid;

		[DataMember]
		public MachineId requestFrom;

		public GetTxStatusDBEvent(int txid, MachineId requestFrom)
		{
			this.txid = txid;
			this.requestFrom = requestFrom;
		}
	}

	/// <summary>
	/// Returns the current status of a transaction.
	/// </summary>
	[DataContract]
	class TxDBStatusResponseEvent : Event
	{
		/// <summary>
		/// Transaction id.
		/// </summary>
		[DataMember]
		public int txid;

		/// <summary>
		/// Status of requested transaction
		/// </summary>
		[DataMember]
		public string txStatus;

		public TxDBStatusResponseEvent(int txid, string txStatus)
		{
			this.txid = txid;
			this.txStatus = txStatus;
		}
	}
	#endregion

	#region blockchain specific

	/// <summary>
	/// Request to print the current status of the ledger.
	/// </summary>
	[DataContract]
	class PrintLedgerEvent : Event { }
	#endregion

	#region additional classes

	/// <summary>
	/// Holds transaction objects in the uncommitted queue of the blockchain.
	/// </summary>
	[DataContract]
	class TxObject
	{
		/// <summary>
		/// txid 
		/// </summary>
		public int txid;
		
		/// <summary>
		/// account from which ether is to be transferred.
		/// </summary>
		public int from;

		/// <summary>
		/// account to which ether is to be transferred.
		/// </summary>
		public int to;

		/// <summary>
		/// amount of ether to transfer
		/// </summary>
		public int amount;

		public TxObject(int txid, int from, int to, int amount)
		{
			this.txid = txid;
			this.to = to;
			this.from = from;
			this.amount = amount;
		}
	}

	/// <summary>
	/// Represents a set of transactions committed to a block in the blockchain.
	/// </summary>
	[DataContract]
	class TxBlock
	{
		/// <summary>
		/// Number of transactions in this block
		/// </summary>
		[DataMember]
		public int numTx;

		/// <summary>
		/// Set of committed transactions
		/// </summary>
		[DataMember]
		public HashSet<TxObject> transactions;

		public TxBlock()
		{
			numTx = 0;
			transactions = new HashSet<TxObject>();
		}
	}

	#endregion
}
