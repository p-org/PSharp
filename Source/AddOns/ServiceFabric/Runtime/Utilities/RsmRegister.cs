using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Microsoft.PSharp.ServiceFabric.Utilities
{
    /// <summary>
    /// Base class for registers
    /// </summary>
    public abstract class RsmRegister 
    {
        /// <summary>
        /// Set current transaction of the state object
        /// </summary>
        /// <param name="tx"></param>
        internal abstract void SetTransaction(ITransaction tx);

        /// <summary>
        /// Set current transaction of the state object
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="timeSpan"></param>
        /// <param name="cancellationToken"></param>
        internal abstract void SetTransaction(ITransaction tx, TimeSpan timeSpan, CancellationToken cancellationToken);

    }
}
