using System;
using System.Threading;
using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric.Utilities
{
    /// <summary>
    /// Base class for registers.
    /// </summary>
    public abstract class RsmRegister 
    {
        /// <summary>
        /// Set current transaction of the state object.
        /// </summary>
        /// <param name="tx">ITransaction</param>
        internal abstract void SetTransaction(ITransaction tx);

        /// <summary>
        /// Set current transaction of the state object.
        /// </summary>
        /// <param name="tx">ITransaction</param>
        /// <param name="timeSpan">TimeSpan</param>
        /// <param name="cancellationToken">CancellationToken</param>
        internal abstract void SetTransaction(ITransaction tx, TimeSpan timeSpan, CancellationToken cancellationToken);
    }
}
