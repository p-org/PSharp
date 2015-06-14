//-----------------------------------------------------------------------
// <copyright file="RemoteDispatcher.cs" company="Microsoft">
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
using System.Collections.Generic;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class implementing the P# dispatcher.
    /// </summary>
    sealed class RemoteDispatcher : IDispatcher
    {
        #region API methods

        /// <summary>
        /// Tries to create a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        MachineId IDispatcher.TryCreateMachine(Type type, params Object[] payload)
        {
            return Runtime.TryCreateMachineRemotely(type, payload);
        }

        /// <summary>
        /// Tries to create a new local or remote machine of the given type
        /// with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="isRemote">Create in another node</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        MachineId IDispatcher.TryCreateMachine(Type type, bool isRemote, params Object[] payload)
        {
            if (isRemote)
            {
                return Runtime.TryCreateMachine(type, payload);
            }
            else
            {
                return Runtime.TryCreateMachine(type, payload);
            }
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="e">Event</param>
        void IDispatcher.Send(MachineId mid, Event e)
        {
            Runtime.SendRemotely(mid, e);
        }

        /// <summary>
        /// Tries to create a new monitor of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        void IDispatcher.TryCreateMonitor(Type type, params Object[] payload)
        {
            Runtime.TryCreateMonitor(type, payload);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        void IDispatcher.Monitor<T>(Event e)
        {
            Runtime.Monitor<T>(e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        bool IDispatcher.Nondet()
        {
            return Runtime.Nondet();
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        void IDispatcher.Assert(bool predicate)
        {
            Runtime.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        void IDispatcher.Assert(bool predicate, string s, params object[] args)
        {
            Runtime.Assert(predicate, s, args);
        }

        #endregion
    }
}
