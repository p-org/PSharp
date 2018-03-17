using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace Microsoft.PSharp.ReliableServices
{
    public class TransactionMock : ITransaction
    {
        /// <summary>
        /// The PSharp runtime
        /// </summary>
        public PSharpRuntime Runtime { get; private set; }

        /// <summary>
        /// True, if the transaction has been committed
        /// </summary>
        public bool Committed { get; private set; }

        /// <summary>
        /// Objects manipulated on this transaction
        /// </summary>
        List<ITxState> StateObjects;

        /// <summary>
        /// To disable simulated failures
        /// </summary>
        static bool AllowFailures = true;

        public TransactionMock(PSharpRuntime runtime, long transactionId)
        {
            this.Runtime = runtime;
            this.StateObjects = new List<ITxState>();
            this.Committed = false;
            this._TransactionId = transactionId;
        }

        public long CommitSequenceNumber => 0;

        public long TransactionId => _TransactionId;
        long _TransactionId;

        public void Abort()
        {
            foreach(var obj in StateObjects)
            {
                obj.Abort(this);
            }

            StateObjects.Clear();
        }

        public Task CommitAsync()
        {
            if (Committed)
            {
                throw new InvalidOperationException("Transaction Mock: multiple commits");
            }
            
            if (AllowFailures && this.Runtime.RandomInteger(100) == 0)
            {
                throw new System.Fabric.TransactionFaultedException("TransactionMock: simulated fault");
            }
            

            foreach (var obj in StateObjects)
            {
                obj.Commit(this);
            }

            StateObjects.Clear();

            Committed = true;
            return Task.FromResult(true);
        }

        public void CheckTimeout(TimeSpan? timeout = null)
        {
            bool doThrow = false;
            if(doThrow)
            {
                throw new TimeoutException("ReliableTx: simulated timeout in Tx " + TransactionId);
            }

            if ((timeout == null || timeout.Value != TimeSpan.MaxValue) && AllowFailures && Runtime.RandomInteger(100) == 0)
            {
                throw new TimeoutException("ReliableTx: simulated timeout in Tx " + TransactionId);
            }
        }

        public void RegisterStateObject(ITxState obj)
        {
            StateObjects.Add(obj);
        }

        public void Dispose()
        {
            if(!Committed)
            {
                Abort();
            }
        }

        public Task<long> GetVisibilitySequenceNumberAsync()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("TransactionMock[{0}]", TransactionId);
        }
    }
}
