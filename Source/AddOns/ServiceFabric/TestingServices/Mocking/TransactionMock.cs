using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
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
        /// True, if the transaction has been aborted
        /// </summary>
        public bool Aborted { get; private set; }

        /// <summary>
        /// Objects manipulated on this transaction
        /// </summary>
        HashSet<ITxState> StateObjects;

        /// <summary>
        /// To disable simulated failures
        /// </summary>
        public static bool AllowFailures = true;

        /// <summary>
        /// Inverse failure probability (i.e., 1 in N)
        /// </summary>
        public static int FailureInvProbability = 100;

        public TransactionMock(PSharpRuntime runtime, long transactionId)
        {
            this.Runtime = runtime == null ? PSharpRuntime.Create() : runtime;
            this.StateObjects = new HashSet<ITxState>();
            this.Committed = false;
            this.Aborted = false;
            this._TransactionId = transactionId;
        }

        public long CommitSequenceNumber => 0;

        public long TransactionId => _TransactionId;
        long _TransactionId;

        public void Abort()
        {
            this.Aborted = false;
            foreach(var obj in StateObjects)
            {
                obj.Abort(this);
            }

            StateObjects.Clear();
        }

        public Task CommitAsync()
        {
            if (Committed || Aborted)
            {
                throw new InvalidOperationException("Transaction Mock: multiple commits");
            }

            if (AllowFailures && this.Runtime.RandomInteger(FailureInvProbability) == 0)
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

            if ((timeout == null || timeout.Value != TimeSpan.MaxValue) && AllowFailures && Runtime.RandomInteger(FailureInvProbability) == 0)
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
