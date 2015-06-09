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
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Static class implementing the P# runtime.
    /// </summary>
    public static class Runtime
    {
        #region fields
        
        /// <summary>
        /// List of machine tasks.
        /// </summary>
        private static List<Task> MachineTasks = new List<Task>();

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private static List<Monitor> Monitors = new List<Monitor>();

        /// <summary>
        /// Lock used by the runtime.
        /// </summary>
        private static Object Lock = new Object();

        /// <summary>
        /// True if runtime is running. False otherwise.
        /// </summary>
        private static bool IsRunning = false;

        /// <summary>
        /// The P# bugfinder.
        /// </summary>
        internal static Scheduler BugFinder = null;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new machine of type T with the given payload.
        /// </summary>
        /// <typeparam name="T">Type of the machine</typeparam>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        public static MachineId CreateMachine<T>(params Object[] payload)
        {
            Configuration.Debug = DebugType.Runtime;
            lock (Runtime.Lock)
            {
                if (!Runtime.IsRunning)
                {
                    Runtime.Initialize();
                }
                else if (Runtime.BugFinder != null)
                {
                    ErrorReporter.ReportAndExit("Cannot create new machines (other than " +
                        "main) from foreign code during systematic testing.");
                }
            }
            
            return Runtime.TryCreateMachine<T>(payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public static void SendEvent(MachineId target, Event e)
        {
            if (Runtime.BugFinder != null)
            {
                ErrorReporter.ReportAndExit("Cannot send events from foreign " +
                    "code during systematic testing.");
            }

            Runtime.Send(target, e);
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new machine of type T with the given payload.
        /// </summary>
        /// <typeparam name="T">Type of the machine</typeparam>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        internal static MachineId TryCreateMachine<T>(params Object[] payload)
        {
            if (typeof(T).IsSubclassOf(typeof(Machine)))
            {
                Object machine = Activator.CreateInstance(typeof(T));

                var mid = (machine as Machine).Id;
                mid.Machine.AssignInitialPayload(payload);

                Output.Debug(DebugType.Runtime, "<CreateLog> Machine {0}({1}) is created.",
                    typeof(T), mid.Value);

                Task task = new Task(() =>
                {
                    if (Runtime.BugFinder != null)
                    {
                        Runtime.BugFinder.NotifyTaskStarted(Task.CurrentId);
                    }

                    mid.Machine.Run();

                    if (Runtime.BugFinder != null)
                    {
                        Runtime.BugFinder.NotifyTaskCompleted(Task.CurrentId);
                    }
                });

                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add(task);
                }

                if (Runtime.BugFinder != null)
                {
                    Runtime.BugFinder.NotifyNewTaskCreated(task.Id, machine as Machine);
                }

                task.Start();

                if (Runtime.BugFinder != null)
                {
                    Runtime.BugFinder.WaitForTaskToStart(task.Id);
                    Runtime.BugFinder.Schedule(Task.CurrentId);
                }

                return mid;
            }
            else
            {
                ErrorReporter.ReportAndExit("Type '{0}' is not a machine.", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// Tries to create a new monitor of type T with the given payload.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="payload">Optional payload</param>
        internal static void TryCreateMonitor<T>(params Object[] payload)
        {
            if (Runtime.BugFinder == null)
            {
                return;
            }

            Runtime.Assert(typeof(T).IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass " +
                "of Monitor.\n", typeof(T).Name);

            Object monitor = Activator.CreateInstance(typeof(T));
            (monitor as Monitor).AssignInitialPayload(payload);
            Output.Debug(DebugType.Runtime, "<CreateLog> Monitor {0} is created.", typeof(T));

            Runtime.Monitors.Add(monitor as Monitor);

            (monitor as Monitor).Run();
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="e">Event</param>
        internal static void Send(MachineId mid, Event e)
        {
            if (mid == null)
            {
                ErrorReporter.ReportAndExit("Cannot send to a null machine.");
            }
            else if (e == null)
            {
                ErrorReporter.ReportAndExit("Cannot send a null event.");
            }
            
            if (mid.Machine is Machine)
            {
                mid.Machine.Enqueue(e);

                if (Runtime.BugFinder != null &&
                    Runtime.BugFinder.HasEnabledTaskForMachine(mid.Machine))
                {
                    Runtime.BugFinder.Schedule(Task.CurrentId);
                    return;
                }

                Task task = new Task(() =>
                {
                    if (Runtime.BugFinder != null)
                    {
                        Runtime.BugFinder.NotifyTaskStarted(Task.CurrentId);
                    }

                    mid.Machine.Run();

                    if (Runtime.BugFinder != null)
                    {
                        Runtime.BugFinder.NotifyTaskCompleted(Task.CurrentId);
                    }
                });

                lock (Runtime.Lock)
                {
                    Runtime.MachineTasks.Add(task);
                }

                if (Runtime.BugFinder != null)
                {
                    Runtime.BugFinder.NotifyNewTaskCreated(task.Id, mid.Machine);
                }

                task.Start();

                if (Runtime.BugFinder != null)
                {
                    Runtime.BugFinder.WaitForTaskToStart(task.Id);
                    Runtime.BugFinder.Schedule(Task.CurrentId);
                }
            }
            else
            {
                ErrorReporter.ReportAndExit("Can only send to a machine.");
            }
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal static void Monitor<T>(Event e)
        {
            if (Runtime.BugFinder == null)
            {
                return;
            }

            foreach (var m in Runtime.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    m.Enqueue(e);
                    m.Run();
                }
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        internal static bool Nondet()
        {
            return Runtime.BugFinder.GetNextNondeterministicChoice();
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal static void WaitMachines()
        {
            Task[] taskArray = null;

            while (Runtime.IsRunning)
            {
                lock (Runtime.Lock)
                {
                    taskArray = Runtime.MachineTasks.ToArray();
                }

                try
                {
                    Task.WaitAll(taskArray);
                }
                catch (AggregateException)
                {
                    break;
                }

                bool moreTasksExist = false;
                lock (Runtime.Lock)
                {
                    moreTasksExist = taskArray.Length != Runtime.MachineTasks.Count;
                }

                if (!moreTasksExist)
                {
                    break;
                }
            }

            Runtime.IsRunning = false;
        }

        #endregion

        #region private API

        /// <summary>
        /// Initializes the P# runtime.
        /// </summary>
        private static void Initialize()
        {
            Runtime.MachineTasks = new List<Task>();
            Runtime.Monitors = new List<Monitor>();

            MachineId.ResetMachineIDCounter();

            var dispatcher = new Dispatcher();
            Microsoft.PSharp.Machine.Dispatcher = dispatcher;
            Microsoft.PSharp.Monitor.Dispatcher = dispatcher;

            Runtime.IsRunning = true;
        }

        #endregion
        
        #region error checking and reporting

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        internal static void Assert(bool predicate)
        {
            if (!predicate)
            {
                ErrorReporter.Report("Assertion failure.");

                if (Runtime.BugFinder != null)
                {
                    Runtime.BugFinder.NotifyAssertionFailure();
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        internal static void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                string message = Output.Format(s, args);
                ErrorReporter.Report(message);

                if (Runtime.BugFinder != null)
                {
                    Runtime.BugFinder.NotifyAssertionFailure();
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        #endregion
    }
}
