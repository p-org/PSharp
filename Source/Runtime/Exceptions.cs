//-----------------------------------------------------------------------
// <copyright file="Exceptions.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// This exception is thrown whenever the Return() statement
    /// is executed to pop a state from the call state stack.
    /// </summary>
    internal sealed class ReturnUsedException : Exception
    {
        /// <summary>
        /// State from which Return() was used.
        /// </summary>
        internal MachineState ReturningState;

        /// <summary>
        /// Default constructor of the ReturnUsedException class.
        /// </summary>
        /// <param name="s">State</param>
        public ReturnUsedException(MachineState s)
        {
            this.ReturningState = s;
        }
    }

    /// <summary>
    /// This exception is thrown whenever the scheduler detects
    /// non-deterministic behaviour.
    /// </summary>
    internal sealed class NondeterminismException : Exception
    {

    }
}
