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

using Microsoft.PSharp.Exploration;
using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.StateCaching;
using Microsoft.PSharp.Threading;
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
        /// A map from task ids to machines.
        /// </summary>
        private static Dictionary<int, Machine> TaskMap;

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
        /// The P# program trace.
        /// </summary>
        internal static Trace ProgramTrace;

        /// <summary>
        /// The P# task scheduler.
        /// </summary>
        internal static TaskScheduler TaskScheduler;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal static int? RootTaskId;

        /// <summary>
        /// The P# bugfinding scheduler.
        /// </summary>
        internal static BugFindingScheduler BugFinder;

        /// <summary>
        /// The P# program state cache.
        /// </summary>
        internal static StateCache StateCache;

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
            // If the event is null then report an error and exit.
            PSharpRuntime.Assert(e != null, "Cannot send a null event.");
            e.AssignPayload(payload);

            try
            {
                PSharpRuntime.Send(target, e);
            }
            catch (TaskCanceledException)
            {
                Output.Debug(DebugType.Testing, "<Exception> TaskCanceledException was thrown.");
            }
        }

        /// <summary>
        /// Blocks and waits to receive an event of the given types.
        /// </summary>
        /// <returns>Payload</returns>
        public static Object Receive(params Type[] events)
        {
            PSharpRuntime.Assert(Task.CurrentId != null, "Only machines can wait to receive an event.");
            PSharpRuntime.Assert(PSharpRuntime.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task {0} does not belong to a machine.",
                (int)Task.CurrentId);
            Machine machine = PSharpRuntime.TaskMap[(int)Task.CurrentId];
            machine.Receive(events);
            return machine.Payload;
        }

        /// <summary>
        /// Blocks and waits to receive an event of the given types, and
        /// executes a given action on receiving the event.
        /// </summary>
        /// <returns>Payload</returns>
        public static Object Receive(params Tuple<Type, Action>[] events)
        {
            PSharpRuntime.Assert(Task.CurrentId != null, "Only machines can wait to receive an event.");
            PSharpRuntime.Assert(PSharpRuntime.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task {0} does not belong to a machine.",
                (int)Task.CurrentId);
            Machine machine = PSharpRuntime.TaskMap[(int)Task.CurrentId];
            machine.Receive(events);
            return machine.Payload;
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        public static void InvokeMonitor<T>(Event e, params Object[] payload)
        {
            // If the event is null then report an error and exit.
            PSharpRuntime.Assert(e != null, "Cannot send a null event.");
            e.AssignPayload(payload);
            PSharpRuntime.Monitor<T>(e);
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
                bool killTasks = true;
                if (Task.CurrentId == PSharpRuntime.RootTaskId)
                {
                    killTasks = false;
                }

                string message = "Assertion failure.";
                PSharpRuntime.BugFinder.NotifyAssertionFailure(message, killTasks);
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
                bool killTasks = true;
                if (Task.CurrentId == PSharpRuntime.RootTaskId)
                {
                    killTasks = false;
                }

                string message = Output.Format(s, args);
                PSharpRuntime.BugFinder.NotifyAssertionFailure(message, killTasks);
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

                MachineId mid = (machine as Machine).Id;
                PSharpRuntime.MachineMap.Add(mid.Value, machine as Machine);
                
                Output.Log("<CreateLog> Machine {0}({1}) is created.", type.Name, mid.MVal);

                Task task = new Task(() =>
                {
                    PSharpRuntime.BugFinder.NotifyTaskStarted();

                    (machine as Machine).AssignInitialPayload(payload);
                    (machine as Machine).GotoStartState();
                    (machine as Machine).RunEventHandler();

                    PSharpRuntime.BugFinder.NotifyTaskCompleted();
                });

                lock (PSharpRuntime.Lock)
                {
                    PSharpRuntime.MachineTasks.Add(task);
                    PSharpRuntime.TaskMap.Add(task.Id, machine as Machine);
                }

                PSharpRuntime.BugFinder.NotifyNewTaskCreated(task.Id, machine as Machine);

                if (Configuration.ScheduleIntraMachineConcurrency)
                {
                    task.Start(PSharpRuntime.TaskScheduler);
                }
                else
                {
                    task.Start();
                }

                PSharpRuntime.BugFinder.WaitForTaskToStart(task.Id);
                PSharpRuntime.BugFinder.Schedule();

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
            
            Output.Log("<CreateLog> Monitor {0} is created.", type.Name);

            PSharpRuntime.Monitors.Add(monitor as Monitor);

            if (Configuration.CheckLiveness)
            {
                PSharpRuntime.LivenessChecker.RegisterMonitor(monitor as Monitor);
            }

            (monitor as Monitor).AssignInitialPayload(payload);
            (monitor as Monitor).GotoStartState();
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        internal static void TryCreateTaskMachine(Task userTask)
        {
            PSharpRuntime.Assert(PSharpRuntime.TaskScheduler is TaskWrapperScheduler, "Unable to wrap " +
                "the task in a machine, because the task wrapper scheduler is not enabled.\n");
            TaskMachine taskMachine = new TaskMachine(PSharpRuntime.TaskScheduler as TaskWrapperScheduler, userTask);

            MachineId mid = taskMachine.Id;
            Output.Log("<CreateLog> TaskMachine({0}) is created.", mid.MVal);

            Task task = new Task(() =>
            {
                PSharpRuntime.BugFinder.NotifyTaskStarted();
                taskMachine.Run();
                PSharpRuntime.BugFinder.NotifyTaskCompleted();
            });

            lock (PSharpRuntime.Lock)
            {
                PSharpRuntime.MachineTasks.Add(task);
            }

            PSharpRuntime.BugFinder.NotifyNewTaskCreated(task.Id, taskMachine);

            if (Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(PSharpRuntime.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            PSharpRuntime.BugFinder.WaitForTaskToStart(task.Id);
            PSharpRuntime.BugFinder.Schedule();
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
            
            if (PSharpRuntime.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                Machine sender = PSharpRuntime.TaskMap[(int)Task.CurrentId];
                Output.Log("<SendLog> Machine '{0}({1})' sent event '{2}' to '{3}({4})'.",
                    sender, sender.Id.MVal, e.GetType(), mid.Type, mid.MVal);
            }
            else
            {
                Output.Log("<SendLog> Event '{2}' was sent to '{3}({4})'.",
                    e.GetType(), mid.Type, mid.MVal);
            }

            Machine machine = PSharpRuntime.MachineMap[mid.Value];

            bool runHandler = false;
            machine.Enqueue(e, ref runHandler);

            if (!runHandler)
            {
                PSharpRuntime.BugFinder.Schedule();
                return;
            }

            Task task = new Task(() =>
            {
                PSharpRuntime.BugFinder.NotifyTaskStarted();
                machine.RunEventHandler();
                PSharpRuntime.BugFinder.NotifyTaskCompleted();
            });

            lock (PSharpRuntime.Lock)
            {
                PSharpRuntime.MachineTasks.Add(task);
                PSharpRuntime.TaskMap.Add(task.Id, machine as Machine);
            }

            PSharpRuntime.BugFinder.NotifyNewTaskCreated(task.Id, machine);

            if (Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(PSharpRuntime.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            PSharpRuntime.BugFinder.WaitForTaskToStart(task.Id);
            PSharpRuntime.BugFinder.Schedule();
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
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal static bool FairNondet(string uniqueId)
        {
            return PSharpRuntime.BugFinder.GetNextNondeterministicChoice(uniqueId);
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        internal static void NotifyDefaultHandlerFired()
        {
            PSharpRuntime.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event.
        /// </summary>
        internal static void NotifyWaitEvent()
        {
            PSharpRuntime.BugFinder.NotifyTaskBlockedOnEvent(Task.CurrentId);
            PSharpRuntime.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="mid">Machine id</param>
        internal static void NotifyReceivedEvent(MachineId mid)
        {
            Machine machine = PSharpRuntime.MachineMap[mid.Value];
            PSharpRuntime.BugFinder.NotifyTaskReceivedEvent(machine);
        }

        /// <summary>
        /// Notifies that a scheduling point should be instrumented
        /// due to a wait synchronization operation.
        /// </summary>
        /// <param name="blockingTasks">Blocking tasks</param>
        /// <param name="waitAll">Boolean value</param>
        internal static void ScheduleOnWait(IEnumerable<Task> blockingTasks, bool waitAll)
        {
            PSharpRuntime.Assert(PSharpRuntime.BugFinder is TaskAwareBugFindingScheduler,
                "Cannot schedule on wait without enabling the task-aware bug finding scheduler.");
            (PSharpRuntime.BugFinder as TaskAwareBugFindingScheduler).NotifyTaskBlocked(
                Task.CurrentId, blockingTasks, waitAll);
            PSharpRuntime.BugFinder.Schedule();
        }

        /// <summary>
        /// Returns the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        internal static Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var machine in PSharpRuntime.MachineMap.Values)
                {
                    hash = hash + 31 * machine.GetCachedState();
                }

                foreach (var monitor in PSharpRuntime.Monitors)
                {
                    hash = hash + 31 * monitor.GetCachedState();
                }

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
            PSharpRuntime.RootTaskId = Task.CurrentId;

            PSharpRuntime.MachineTasks = new List<Task>();
            PSharpRuntime.MachineMap = new Dictionary<int, Machine>();
            PSharpRuntime.TaskMap = new Dictionary<int, Machine>();
            PSharpRuntime.Monitors = new List<Monitor>();

            if (Configuration.ScheduleIntraMachineConcurrency)
            {
                PSharpRuntime.TaskScheduler = new TaskWrapperScheduler(PSharpRuntime.MachineTasks);
                TaskMachineExtensions.TaskScheduler = PSharpRuntime.TaskScheduler as TaskWrapperScheduler;
            }

            MachineId.ResetMachineIDCounter();

            BugFindingDispatcher dispatcher = new BugFindingDispatcher();
            Machine.Dispatcher = dispatcher;
            PSharp.Monitor.Dispatcher = dispatcher;

            PSharpRuntime.ProgramTrace = new Trace();
            PSharpRuntime.StateCache = new StateCache();
            PSharpRuntime.LivenessChecker = new LivenessChecker();

            PSharpRuntime.IsRunning = true;
        }

        #endregion
    }
}
