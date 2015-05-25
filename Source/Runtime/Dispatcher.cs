//-----------------------------------------------------------------------
// <copyright file="Dispatcher.cs" company="Microsoft">
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
    sealed class Dispatcher : IDispatcher
    {
        #region API methods

        /// <summary>
        /// Attempts to create a new machine instance of type T with
        /// the given payload.
        /// </summary>
        /// <param name="m">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine</returns>
        Machine IDispatcher.TryCreateNewMachineInstance(Type m, params Object[] payload)
        {
            return Runtime.TryCreateNewMachineInstance(m, payload);
        }

        /// <summary>
        /// Attempts to create a new machine instance of type T with
        /// the given payload.
        /// </summary>
        /// <typeparam name="T">Type of the machine</typeparam>
        /// <param name="creator">Creator machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine</returns>
        T IDispatcher.TryCreateNewMachineInstance<T>(Machine creator, params Object[] payload)
        {
            return Runtime.TryCreateNewMachineInstance<T>(creator, payload);
        }

        /// <summary>
        /// Attempts to create a new monitor instance of type T with
        /// the given payload. There can be only one monitor instance
        /// of each monitor type.
        /// </summary>
        /// <param name="m">Type of the monitor</param>
        /// <param name="payload">Optional payload</param>
        void IDispatcher.TryCreateNewMonitorInstance(Type m, params Object[] payload)
        {
            Runtime.TryCreateNewMonitorInstance(m, payload);
        }

        /// <summary>
        /// Attempts to create a new monitor instance of type T with
        /// the given payload. There can be only one monitor instance
        /// of each monitor type.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="payload">Optional payload</param>
        void IDispatcher.TryCreateNewMonitorInstance<T>(params Object[] payload)
        {
            Runtime.TryCreateNewMonitorInstance<T>(payload);
        }

        /// <summary>
        /// Attempts to send (i.e. enqueue) an asynchronous event to a machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="target">Target machine</param>
        /// <param name="e">Event</param>
        void IDispatcher.Send(Machine sender, Machine target, Event e)
        {
            Runtime.Send(sender, target, e);
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

        /// <summary>
        /// Returns all registered event types.
        /// </summary>
        /// <returns>List of event types</returns>
        List<Type> IDispatcher.GetRegisteredEventTypes()
        {
            return Runtime.GetRegisteredEventTypes();
        }

        #endregion
    }
}
