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
    public static class BugFindingRuntime
    {
        #region fields
        
        /// <summary>
        /// List of machine tasks.
        /// </summary>
        private static List<Task> MachineTasks;

        /// <summary>
        /// A map from unique machine ids to machines.
        /// </summary>
        internal static Dictionary<int, Machine> MachineMap;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private static List<Monitor> Monitors;

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
        /// Creates a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        public static MachineId CreateMachine(Type type, params Object[] payload)
        {
            lock (BugFindingRuntime.Lock)
            {
                if (!BugFindingRuntime.IsRunning)
                {
                    BugFindingRuntime.Initialize();
                }
                else if (BugFindingRuntime.BugFinder != null)
                {
                    ErrorReporter.ReportAndExit("Cannot create new machines (other than " +
                        "main) from foreign code during systematic testing.");
                }
            }
            
            return BugFindingRuntime.TryCreateMachine(type, payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public static void SendEvent(MachineId target, Event e)
        {
            if (BugFindingRuntime.BugFinder != null)
            {
                ErrorReporter.ReportAndExit("Cannot send events from foreign " +
                    "code during systematic testing.");
            }

            BugFindingRuntime.Send(target, e);
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        internal static MachineId TryCreateMachine(Type type, params Object[] payload)
        {
            if (type.IsSubclassOf(typeof(Machine)))
            {
                Object machine = Activator.CreateInstance(type);
                (machine as Machine).AssignInitialPayload(payload);

                var mid = (machine as Machine).Id;
                BugFindingRuntime.MachineMap.Add(mid.Value, machine as Machine);
                
                Output.Debug(DebugType.Runtime, "<CreateLog> Machine {0}({1}) is created.",
                    type.Name, mid.Value);
                
                Task task = new Task(() =>
                {
                    if (BugFindingRuntime.BugFinder != null)
                    {
                        BugFindingRuntime.BugFinder.NotifyTaskStarted(Task.CurrentId);
                    }

                    (machine as Machine).Run();

                    if (BugFindingRuntime.BugFinder != null)
                    {
                        BugFindingRuntime.BugFinder.NotifyTaskCompleted(Task.CurrentId);
                    }
                });

                lock (BugFindingRuntime.Lock)
                {
                    BugFindingRuntime.MachineTasks.Add(task);
                }

                if (BugFindingRuntime.BugFinder != null)
                {
                    BugFindingRuntime.BugFinder.NotifyNewTaskCreated(task.Id, machine as Machine);
                }

                task.Start();

                if (BugFindingRuntime.BugFinder != null)
                {
                    BugFindingRuntime.BugFinder.WaitForTaskToStart(task.Id);
                    BugFindingRuntime.BugFinder.Schedule(Task.CurrentId);
                }

                return mid;
            }
            else
            {
                ErrorReporter.ReportAndExit("Type '{0}' is not a machine.", type.Name);
                return null;
            }
        }

        /// <summary>
        /// Tries to create a new monitor of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="payload">Optional payload</param>
        internal static void TryCreateMonitor(Type type, params Object[] payload)
        {
            if (BugFindingRuntime.BugFinder == null)
            {
                return;
            }

            BugFindingRuntime.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass " +
                "of Monitor.\n", type.Name);

            Object monitor = Activator.CreateInstance(type);
            (monitor as Monitor).AssignInitialPayload(payload);
            Output.Debug(DebugType.Runtime, "<CreateLog> Monitor {0} is created.", type.Name);

            BugFindingRuntime.Monitors.Add(monitor as Monitor);

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

            var machine = BugFindingRuntime.MachineMap[mid.Value];
            machine.Enqueue(e);

            if (BugFindingRuntime.BugFinder != null &&
                BugFindingRuntime.BugFinder.HasEnabledTaskForMachine(machine))
            {
                BugFindingRuntime.BugFinder.Schedule(Task.CurrentId);
                return;
            }

            Task task = new Task(() =>
            {
                if (BugFindingRuntime.BugFinder != null)
                {
                    BugFindingRuntime.BugFinder.NotifyTaskStarted(Task.CurrentId);
                }

                machine.Run();

                if (BugFindingRuntime.BugFinder != null)
                {
                    BugFindingRuntime.BugFinder.NotifyTaskCompleted(Task.CurrentId);
                }
            });

            lock (BugFindingRuntime.Lock)
            {
                BugFindingRuntime.MachineTasks.Add(task);
            }

            if (BugFindingRuntime.BugFinder != null)
            {
                BugFindingRuntime.BugFinder.NotifyNewTaskCreated(task.Id, machine);
            }

            task.Start();

            if (BugFindingRuntime.BugFinder != null)
            {
                BugFindingRuntime.BugFinder.WaitForTaskToStart(task.Id);
                BugFindingRuntime.BugFinder.Schedule(Task.CurrentId);
            }
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal static void Monitor<T>(Event e)
        {
            if (BugFindingRuntime.BugFinder == null)
            {
                return;
            }

            foreach (var m in BugFindingRuntime.Monitors)
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
            if (BugFindingRuntime.BugFinder != null)
            {
                return BugFindingRuntime.BugFinder.GetNextNondeterministicChoice();
            }
            else
            {
                var random = new Random(DateTime.Now.Millisecond);

                bool result = false;
                if (random.Next(2) == 1)
                {
                    result = true;
                }

                return result;
            }
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal static void WaitMachines()
        {
            Task[] taskArray = null;

            while (BugFindingRuntime.IsRunning)
            {
                lock (BugFindingRuntime.Lock)
                {
                    taskArray = BugFindingRuntime.MachineTasks.ToArray();
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
                lock (BugFindingRuntime.Lock)
                {
                    moreTasksExist = taskArray.Length != BugFindingRuntime.MachineTasks.Count;
                }

                if (!moreTasksExist)
                {
                    break;
                }
            }

            BugFindingRuntime.IsRunning = false;
        }

        #endregion

        #region private API

        /// <summary>
        /// Initializes the P# runtime.
        /// </summary>
        private static void Initialize()
        {
            BugFindingRuntime.MachineTasks = new List<Task>();
            BugFindingRuntime.MachineMap = new Dictionary<int, Machine>();
            BugFindingRuntime.Monitors = new List<Monitor>();

            MachineId.ResetMachineIDCounter();

            var dispatcher = new Dispatcher();
            Microsoft.PSharp.Machine.Dispatcher = dispatcher;
            Microsoft.PSharp.Monitor.Dispatcher = dispatcher;

            BugFindingRuntime.IsRunning = true;
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

                if (BugFindingRuntime.BugFinder != null)
                {
                    BugFindingRuntime.BugFinder.NotifyAssertionFailure();
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

                if (BugFindingRuntime.BugFinder != null)
                {
                    BugFindingRuntime.BugFinder.NotifyAssertionFailure();
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
