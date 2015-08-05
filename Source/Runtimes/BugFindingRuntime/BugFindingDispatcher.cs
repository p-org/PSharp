//-----------------------------------------------------------------------
// <copyright file="BugFindingDispatcher.cs" company="Microsoft">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class implementing the P# dispatcher.
    /// </summary>
    internal sealed class BugFindingDispatcher : IDispatcher
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
            return PSharpRuntime.TryCreateMachine(type, payload);
        }

        /// <summary>
        /// Tries to create a new remote machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        MachineId IDispatcher.TryCreateRemoteMachine(Type type, params Object[] payload)
        {
            // Remote does not work in the bug-finding runtime.
            return PSharpRuntime.TryCreateMachine(type, payload);
        }

        /// <summary>
        /// Tries to create a new monitor of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        void IDispatcher.TryCreateMonitor(Type type, params Object[] payload)
        {
            PSharpRuntime.TryCreateMonitor(type, payload);
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        void IDispatcher.TryCreateTaskMachine(Task userTask)
        {
            PSharpRuntime.TryCreateTaskMachine(userTask);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="e">Event</param>
        void IDispatcher.Send(MachineId mid, Event e)
        {
            PSharpRuntime.Send(mid, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        void IDispatcher.Monitor<T>(Event e)
        {
            PSharpRuntime.Monitor<T>(e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        bool IDispatcher.Nondet()
        {
            return PSharpRuntime.Nondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        bool IDispatcher.FairNondet()
        {
            return PSharpRuntime.Nondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        bool IDispatcher.FairNondet(string uniqueId)
        {
            return PSharpRuntime.FairNondet(uniqueId);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        void IDispatcher.Assert(bool predicate)
        {
            PSharpRuntime.Assert(predicate);
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
            PSharpRuntime.Assert(predicate, s, args);
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        void IDispatcher.NotifyDefaultHandlerFired()
        {
            PSharpRuntime.NotifyDefaultHandlerFired();
        }

        #endregion
    }
}
