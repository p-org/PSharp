using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    internal class MockTransaction : ITransaction
    {
        /// <summary>
        /// The PSharp runtime.
        /// </summary>
        public PSharpRuntime Runtime { get; private set; }

        /// <summary>
        /// The transaction id.
        /// </summary>
        public long TransactionId { get; }

        /// <summary>
        /// True, if the transaction has been committed.
        /// </summary>
        public bool Committed { get; private set; }

        /// <summary>
        /// Objects manipulated on this transaction.
        /// </summary>
        List<ITxState> StateObjects;

        /// <summary>
        /// To disable simulated failures.
        /// </summary>
        public static bool AllowFailures = true;

        /// <summary>
        /// Inverse failure probability (i.e., 1 in N).
        /// </summary>
        public static int FailureInvProbability = 100;

        /// <summary>
        /// The commit sequence number.
        /// </summary>
        public long CommitSequenceNumber => 0;

        public MockTransaction(PSharpRuntime runtime, long transactionId)
        {
            // TODO: fix this.
            this.Runtime = runtime == null ? PSharpRuntime.Create() : runtime;
            this.StateObjects = new List<ITxState>();
            this.Committed = false;
            this.TransactionId = transactionId;
        }

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
            this.Runtime.Assert(!Committed, "Transaction '{0}' has already been committed.", this.TransactionId);
            if (AllowFailures && this.Runtime.RandomInteger(FailureInvProbability) == 0)
            {
                throw new System.Fabric.TransactionFaultedException(
                    $"TransactionMock: simulated fault in transaction '{this.TransactionId}'");
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
            if (doThrow)
            {
                throw new TimeoutException($"ReliableTx: simulated timeout in transaction '{this.TransactionId}'");
            }

            if ((timeout == null || timeout.Value != TimeSpan.MaxValue) &&
                AllowFailures && Runtime.RandomInteger(FailureInvProbability) == 0)
            {
                throw new TimeoutException($"ReliableTx: simulated timeout in transaction '{this.TransactionId}'");
            }
        }

        public void RegisterStateObject(ITxState obj)
        {
            StateObjects.Add(obj);
        }

        public void Dispose()
        {
            if (!Committed)
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
            return string.Format($"TransactionMock[{TransactionId}]");
        }
    }
}
