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
using Microsoft.PSharp.StateCaching;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Static class implementing the P# runtime.
    /// </summary>
    public static class PSharpRuntime
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
        internal static Scheduler BugFinder;

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
            lock (PSharpRuntime.Lock)
            {
                if (!PSharpRuntime.IsRunning)
                {
                    PSharpRuntime.Initialize();
                }
            }
            
            return PSharpRuntime.TryCreateMachine(type, payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        public static void SendEvent(MachineId target, Event e, params Object[] payload)
        {
            e.AssignPayload(payload);

            try
            {
                PSharpRuntime.Send(target, e);
            }
            catch (TaskCanceledException)
            {
                Output.Log("<Exception> TaskCanceledException was thrown.");
            }
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

                var mid = (machine as Machine).Id;
                PSharpRuntime.MachineMap.Add(mid.Value, machine as Machine);
                
                Output.Debug(DebugType.Runtime, "<CreateLog> Machine {0}({1}) is created.",
                    type.Name, mid.Value);
                
                Task task = new Task(() =>
                {
                    PSharpRuntime.BugFinder.NotifyTaskStarted(Task.CurrentId);

                    (machine as Machine).AssignInitialPayload(payload);
                    (machine as Machine).GotoStartState();
                    (machine as Machine).RunEventHandler();

                    PSharpRuntime.BugFinder.NotifyTaskCompleted(Task.CurrentId);
                });

                lock (PSharpRuntime.Lock)
                {
                    PSharpRuntime.MachineTasks.Add(task);
                }

                PSharpRuntime.BugFinder.NotifyNewTaskCreated(task.Id, machine as Machine);

                task.Start();

                PSharpRuntime.BugFinder.WaitForTaskToStart(task.Id);
                PSharpRuntime.BugFinder.Schedule(Task.CurrentId);

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
            PSharpRuntime.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass " +
                "of Monitor.\n", type.Name);

            Object monitor = Activator.CreateInstance(type);
            
            Output.Debug(DebugType.Runtime, "<CreateLog> Monitor {0} is created.", type.Name);

            PSharpRuntime.Monitors.Add(monitor as Monitor);

            (monitor as Monitor).AssignInitialPayload(payload);
            (monitor as Monitor).GotoStartState();
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

            var machine = PSharpRuntime.MachineMap[mid.Value];

            var runHandler = false;
            machine.Enqueue(e, ref runHandler);

            if (!runHandler)
            {
                PSharpRuntime.BugFinder.Schedule(Task.CurrentId);
                return;
            }

            Task task = new Task(() =>
            {
                PSharpRuntime.BugFinder.NotifyTaskStarted(Task.CurrentId);

                machine.RunEventHandler();

                PSharpRuntime.BugFinder.NotifyTaskCompleted(Task.CurrentId);
            });

            lock (PSharpRuntime.Lock)
            {
                PSharpRuntime.MachineTasks.Add(task);
            }

            PSharpRuntime.BugFinder.NotifyNewTaskCreated(task.Id, machine);

            task.Start();

            PSharpRuntime.BugFinder.WaitForTaskToStart(task.Id);
            PSharpRuntime.BugFinder.Schedule(Task.CurrentId);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal static void Monitor<T>(Event e)
        {
            foreach (var m in PSharpRuntime.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    m.MonitorEvent(e);
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
            return PSharpRuntime.BugFinder.GetNextNondeterministicChoice();
        }

        /// <summary>
        /// Checks the liveness monitors for violations.
        /// </summary>
        internal static void CheckLivenessMonitors()
        {
            foreach (var m in PSharpRuntime.Monitors)
            {
                var stateName = "";
                if (m.IsInHotState(out stateName))
                {
                    string message = Output.Format("Program terminated with monitor '{0}' " +
                        "in hot state '{1}'.", m.GetType().Name, stateName);
                    ErrorReporter.Report(message);
                    PSharpRuntime.BugFinder.NotifyAssertionFailure(false);
                }
            }
        }

        /// <summary>
        /// Returns the current scheduling choice as a program trace step.
        /// </summary>
        /// <returns>TraceStep</returns>
        internal static TraceStep GetSchedulingChoiceTraceStep()
        {
            var fingerprint = PSharpRuntime.CaptureProgramState();
            var traceStep = TraceStep.CreateSchedulingChoice(fingerprint);

            foreach (var monitor in PSharpRuntime.Monitors)
            {
                MonitorStatus status = MonitorStatus.None;
                if (monitor.IsInHotState())
                {
                    status = MonitorStatus.Hot;
                }
                else if (monitor.IsInColdState())
                {
                    status = MonitorStatus.Cold;
                }

                traceStep.Monitors.Add(monitor, status);
            }

            return traceStep;
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal static void WaitMachines()
        {
            Task[] taskArray = null;

            while (PSharpRuntime.IsRunning)
            {
                lock (PSharpRuntime.Lock)
                {
                    taskArray = PSharpRuntime.MachineTasks.ToArray();
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
                lock (PSharpRuntime.Lock)
                {
                    moreTasksExist = taskArray.Length != PSharpRuntime.MachineTasks.Count;
                }

                if (!moreTasksExist)
                {
                    break;
                }
            }

            PSharpRuntime.IsRunning = false;
        }

        #endregion

        #region private API

        /// <summary>
        /// Initializes the P# runtime.
        /// </summary>
        private static void Initialize()
        {
            PSharpRuntime.MachineTasks = new List<Task>();
            PSharpRuntime.MachineMap = new Dictionary<int, Machine>();
            PSharpRuntime.Monitors = new List<Monitor>();

            MachineId.ResetMachineIDCounter();

            var dispatcher = new BugFindingDispatcher();
            Microsoft.PSharp.Machine.Dispatcher = dispatcher;
            Microsoft.PSharp.Monitor.Dispatcher = dispatcher;

            PSharpRuntime.IsRunning = true;
        }

        /// <summary>
        /// Captures the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        private static Fingerprint CaptureProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                var hash = 19;

                foreach (var machine in PSharpRuntime.MachineMap.Values)
                {
                    hash = hash + 31 * machine.GetHashCode();
                }

                foreach (var monitor in PSharpRuntime.Monitors)
                {
                    hash = hash + 31 * monitor.GetHashCode();
                }

                Console.WriteLine("Fingerprint: " + hash);
                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
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
                PSharpRuntime.BugFinder.NotifyAssertionFailure();
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
                PSharpRuntime.BugFinder.NotifyAssertionFailure();
            }
        }

        #endregion
    }
}
