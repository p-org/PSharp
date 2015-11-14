//-----------------------------------------------------------------------
// <copyright file="IDispatcher.cs" company="Microsoft">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Interface of a generic dispatcher that handles the
    /// communication with the P# runtime.
    /// </summary>
    internal interface IDispatcher
    {
        /// <summary>
        /// Tries to create a new machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        MachineId TryCreateMachine(Type type);

        /// <summary>
        /// Tries to create a new remote machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        MachineId TryCreateRemoteMachine(Type type);

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        void TryCreateMonitor(Type type);

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        void TryCreateTaskMachine(Task userTask);

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        void Send(MachineId mid, Event e);

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        void Monitor<T>(Event e);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        bool Random();

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        bool FairRandom();

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        bool FairRandom(string uniqueId);

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        void Assert(bool predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        void Assert(bool predicate, string s, params object[] args);

        /// <summary>
        /// Logs the given text with the runtime.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        void Log(string s, params object[] args);

        /// <summary>
        /// Notifies that a machine is waiting to receive an event.
        /// </summary>
        /// <param name="mid">MachineId</param>
        void NotifyWaitEvent(MachineId mid);

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="mid">MachineId</param>
        void NotifyReceivedEvent(MachineId mid);

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        void NotifyDefaultHandlerFired();

        /// <summary>
        /// Notifies that a scheduling point should be instrumented
        /// due to a wait synchronization operation.
        /// </summary>
        /// <param name="blockingTasks">Blocking tasks</param>
        /// <param name="waitAll">Boolean value</param>
        void ScheduleOnWait(IEnumerable<Task> blockingTasks, bool waitAll);
    }
}
