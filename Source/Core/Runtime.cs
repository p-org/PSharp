//-----------------------------------------------------------------------
// <copyright file="Runtime.cs" company="Microsoft">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class implementing the P# runtime.
    /// </summary>
    public class PSharpRuntime
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// The application assembly.
        /// </summary>
        internal Assembly AppAssembly;

        /// <summary>
        /// A map from unique machine ids to machines.
        /// </summary>
        protected ConcurrentDictionary<int, Machine> MachineMap;

        /// <summary>
        /// A map from task ids to machines.
        /// </summary>
        protected ConcurrentDictionary<int, Machine> TaskMap;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new P# runtime.
        /// </summary>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create()
        {
            return new PSharpRuntime();
        }

        /// <summary>
        /// Creates a new P# runtime.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(Configuration configuration)
        {
            return new PSharpRuntime(configuration);
        }

        /// <summary>
        /// Creates a new machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        public virtual MachineId CreateMachine(Type type)
        {
            return this.TryCreateMachine(type);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public virtual void SendEvent(MachineId target, Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.Send(target, e);
        }

        /// <summary>
        /// Blocks and waits to receive an event of the given types. Returns
        /// the received event.
        /// </summary>
        /// <returns>Received event</returns>
        public virtual Event Receive(params Type[] events)
        {
            this.Assert(Task.CurrentId != null, "Only machines can wait to receive an event.");
            this.Assert(this.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task {0} does not belong to a machine.",
                (int)Task.CurrentId);
            Machine machine = this.TaskMap[(int)Task.CurrentId];
            machine.Receive(events);
            return machine.ReceivedEvent;
        }

        /// <summary>
        /// Blocks and waits to receive an event of the given types, and
        /// executes a given action on receiving the event. Returns the
        /// received event.
        /// </summary>
        /// <returns>Received event</returns>
        public virtual Event Receive(params Tuple<Type, Action>[] events)
        {
            this.Assert(Task.CurrentId != null, "Only machines can wait to receive an event.");
            this.Assert(this.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task {0} does not belong to a machine.",
                (int)Task.CurrentId);
            Machine machine = this.TaskMap[(int)Task.CurrentId];
            machine.Receive(events);
            return machine.ReceivedEvent;
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public virtual void InvokeMonitor<T>(Event e)
        {
            // No-op for real execution.
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        public virtual bool Random()
        {
            return this.GetNondeterministicChoice(2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [1..maxValue], where 1 triggers true.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        public virtual bool Random(int maxValue)
        {
            return this.GetNondeterministicChoice(maxValue);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public virtual void Assert(bool predicate)
        {
            if (!predicate)
            {
                ErrorReporter.Report("Assertion failure.");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public virtual void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                string message = Output.Format(s, args);
                ErrorReporter.Report(message);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Logs the given text.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public virtual void Log(string s, params object[] args)
        {
            // No-op for real execution.
        }

        #endregion

        #region protected API

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected PSharpRuntime()
        {
            this.MachineMap = new ConcurrentDictionary<int, Machine>();
            this.TaskMap = new ConcurrentDictionary<int, Machine>();

            MachineId.ResetMachineIDCounter();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected PSharpRuntime(Configuration configuration)
        {
            this.Configuration = configuration;

            this.MachineMap = new ConcurrentDictionary<int, Machine>();
            this.TaskMap = new ConcurrentDictionary<int, Machine>();

            MachineId.ResetMachineIDCounter();
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        internal virtual MachineId TryCreateMachine(Type type)
        {
            if (type.IsSubclassOf(typeof(Machine)))
            {
                MachineId mid = new MachineId(type, this);
                Machine machine = Activator.CreateInstance(type) as Machine;
                machine.SetMachineId(mid);
                machine.InitializeStateInformation();
                
                if (!this.MachineMap.TryAdd(mid.Value, machine))
                {
                    ErrorReporter.ReportAndExit("Machine {0}({1}) was already created.",
                        type.Name, mid.Value);
                }
                
                Task task = new Task(() =>
                {
                    this.TaskMap.TryAdd(Task.CurrentId.Value, machine);

                    try
                    {
                        machine.GotoStartState();
                        machine.RunEventHandler();
                    }
                    finally
                    {
                        this.TaskMap.TryRemove(Task.CurrentId.Value, out machine);
                    }
                });

                task.Start();

                return mid;
            }
            else
            {
                ErrorReporter.ReportAndExit("Type '{0}' is not a machine.", type.Name);
                return null;
            }
        }

        /// <summary>
        /// Tries to create a new remote machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        internal virtual MachineId TryCreateRemoteMachine(Type type)
        {
            return this.TryCreateMachine(type);
        }

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal virtual void TryCreateMonitor(Type type)
        {
            // No-op for real execution.
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        internal virtual void TryCreateTaskMachine(Task userTask)
        {
            // No-op for real execution.
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        internal virtual void Send(MachineId mid, Event e)
        {
            if (mid == null)
            {
                ErrorReporter.ReportAndExit("Cannot send to a null machine.");
            }
            else if (e == null)
            {
                ErrorReporter.ReportAndExit("Cannot send a null event.");
            }

            Machine machine = this.MachineMap[mid.Value];

            bool runHandler = false;
            machine.Enqueue(e, ref runHandler);

            if (!runHandler)
            {
                return;
            }

            Task task = new Task(() =>
            {
                this.TaskMap.TryAdd(Task.CurrentId.Value, machine as Machine);

                try
                {
                    machine.RunEventHandler();
                }
                finally
                {
                    this.TaskMap.TryRemove(Task.CurrentId.Value, out machine);
                }
            });

            task.Start();
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        internal virtual void SendRemotely(MachineId mid, Event e)
        {
            this.Send(mid, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal virtual void Monitor<T>(Event e)
        {
            // No-op for real execution.
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal virtual bool GetNondeterministicChoice(int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(maxValue) == 1)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal virtual bool GetFairNondeterministicChoice(string uniqueId)
        {
            return this.GetNondeterministicChoice(2);
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal virtual void NotifyWaitEvent(MachineId mid)
        {
            Machine machine = this.MachineMap[mid.Value];
            lock (machine)
            {
                System.Threading.Monitor.Wait(machine);
            }
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal virtual void NotifyReceivedEvent(MachineId mid)
        {
            Machine machine = this.MachineMap[mid.Value];
            lock (machine)
            {
                System.Threading.Monitor.Pulse(machine);
            }
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        internal virtual void NotifyDefaultHandlerFired()
        {
            // No-op for real execution.
        }

        #endregion

        #region TPL methods

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public virtual void WaitAll(params Task[] tasks)
        {
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public virtual void WaitAll(Task[] tasks, CancellationToken cancellationToken)
        {
            Task.WaitAll(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public virtual void WaitAll(Task[] tasks, int millisecondsTimeout)
        {
            Task.WaitAll(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public virtual void WaitAll(Task[] tasks, TimeSpan timeout)
        {
            Task.WaitAll(tasks, timeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public virtual void WaitAll(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            Task.WaitAll(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public virtual void WaitAny(params Task[] tasks)
        {
            Task.WaitAny(tasks);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public virtual void WaitAny(Task[] tasks, CancellationToken cancellationToken)
        {
            Task.WaitAny(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public virtual void WaitAny(Task[] tasks, int millisecondsTimeout)
        {
            Task.WaitAny(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public virtual void WaitAny(Task[] tasks, TimeSpan timeout)
        {
            Task.WaitAny(tasks, timeout);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public virtual void WaitAny(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            Task.WaitAny(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an array have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task WhenAll(params Task[] tasks)
        {
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task WhenAll(IEnumerable<Task> tasks)
        {
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an array have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task<Task> WhenAny(params Task[] tasks)
        {
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public virtual Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return Task.WhenAny(tasks);
        }

        #endregion
    }
}
