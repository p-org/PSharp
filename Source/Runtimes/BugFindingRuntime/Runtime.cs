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
        private static Dictionary<int, Machine> MachineMap;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private static List<Monitor> Monitors;

        /// <summary>
        /// Lock used by the runtime.
        /// </summary>
        private static Object Lock = new Object();

        /// <summary>
        /// The main thread id.
        /// </summary>
        private static int? MainThreadId;

        /// <summary>
        /// True if runtime is running. False otherwise.
        /// </summary>
        private static bool IsRunning = false;

        /// <summary>
        /// The P# program state explorer.
        /// </summary>
        internal static StateExplorer StateExplorer;

        /// <summary>
        /// The P# bugfinder.
        /// </summary>
        internal static Scheduler BugFinder;

        /// <summary>
        /// The P# liveness checker.
        /// </summary>
        internal static LivenessChecker LivenessChecker;

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

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        public static bool Nondeterministic()
        {
            return PSharpRuntime.Nondet();
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public static void Assert(bool predicate)
        {
            if (!predicate)
            {
                var terminateScheduler = true;
                if (Task.CurrentId == PSharpRuntime.MainThreadId)
                {
                    terminateScheduler = false;
                }

                ErrorReporter.Report("Assertion failure.");
                PSharpRuntime.BugFinder.NotifyAssertionFailure(terminateScheduler);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public static void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                var terminateScheduler = true;
                if (Task.CurrentId == PSharpRuntime.MainThreadId)
                {
                    terminateScheduler = false;
                }

                string message = Output.Format(s, args);
                ErrorReporter.Report(message);
                PSharpRuntime.BugFinder.NotifyAssertionFailure(terminateScheduler);
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
            PSharpRuntime.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a " +
                "subclass of Monitor.\n", type.Name);

            Object monitor = Activator.CreateInstance(type);
            
            Output.Debug(DebugType.Runtime, "<CreateLog> Monitor {0} is created.", type.Name);

            PSharpRuntime.Monitors.Add(monitor as Monitor);

            if (Configuration.CheckLiveness)
            {
                PSharpRuntime.LivenessChecker.RegisterMonitor(monitor as Monitor);
            }

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
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal static bool Nondet(string uniqueId)
        {
            return PSharpRuntime.BugFinder.GetNextNondeterministicChoice(uniqueId);
        }

        /// <summary>
        /// Captures the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        internal static Fingerprint CaptureProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                var hash = 19;

                foreach (var machine in PSharpRuntime.MachineMap.Values)
                {
                    hash = hash + 31 * machine.GetHashedState();
                }

                foreach (var monitor in PSharpRuntime.Monitors)
                {
                    hash = hash + 31 * monitor.GetHashedState();
                }

                Output.Log("<LivenessDebug> Captured program state with fingerprint '{0}'.", hash);
                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
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
            PSharpRuntime.MainThreadId = Task.CurrentId;

            PSharpRuntime.MachineTasks = new List<Task>();
            PSharpRuntime.MachineMap = new Dictionary<int, Machine>();
            PSharpRuntime.Monitors = new List<Monitor>();

            MachineId.ResetMachineIDCounter();

            var dispatcher = new BugFindingDispatcher();
            Microsoft.PSharp.Machine.Dispatcher = dispatcher;
            Microsoft.PSharp.Monitor.Dispatcher = dispatcher;

            PSharpRuntime.StateExplorer = new StateExplorer();
            PSharpRuntime.LivenessChecker = new LivenessChecker();

            PSharpRuntime.IsRunning = true;
        }

        #endregion
    }
}
