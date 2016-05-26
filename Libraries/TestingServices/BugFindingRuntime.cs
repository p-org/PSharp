//-----------------------------------------------------------------------
// <copyright file="BugFindingRuntime.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Exploration;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Threading;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.Visualization;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Static class implementing the P# bug-finding runtime.
    /// </summary>
    internal sealed class PSharpBugFindingRuntime : PSharpRuntime
    {
        #region fields
        
        /// <summary>
        /// List of machine tasks.
        /// </summary>
        private List<Task> MachineTasks;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        /// <summary>
        /// Lock used by the runtime.
        /// </summary>
        private Object Lock = new Object();

        /// <summary>
        /// The P# program trace.
        /// </summary>
        internal Trace ProgramTrace;

        /// <summary>
        /// The P# task scheduler.
        /// </summary>
        internal TaskScheduler TaskScheduler;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal int? RootTaskId;

        /// <summary>
        /// The P# bugfinding scheduler.
        /// </summary>
        internal BugFindingScheduler BugFinder;

        /// <summary>
        /// The P# program state cache.
        /// </summary>
        internal StateCache StateCache;

        /// <summary>
        /// The P# liveness checker.
        /// </summary>
        internal LivenessChecker LivenessChecker;

        /// <summary>
        /// The P# program visualizer.
        /// </summary>
        internal IProgramVisualizer Visualizer;

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private int OperationIdCounter;

        /// <summary>
        /// True if runtime is running.
        /// </summary>
        private bool IsRunning = false;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="visualizer">Visualizer</param>
        internal PSharpBugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy,
            IProgramVisualizer visualizer)
            : base(configuration)
        {
            this.RootTaskId = Task.CurrentId;

            this.MachineTasks = new List<Task>();
            this.Monitors = new List<Monitor>();

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                this.TaskScheduler = new TaskWrapperScheduler(this, this.MachineTasks);
                TaskMachineExtensions.TaskScheduler = this.TaskScheduler as TaskWrapperScheduler;
                this.BugFinder = new TaskAwareBugFindingScheduler(this, strategy);
            }
            else
            {
                this.BugFinder = new BugFindingScheduler(this, strategy);
            }

            this.ProgramTrace = new Trace();
            this.StateCache = new StateCache(this);
            this.LivenessChecker = new LivenessChecker(this);
            this.Visualizer = visualizer;

            this.OperationIdCounter = 0;

            this.IsRunning = true;
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void SendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");

            try
            {
                this.Send(null, target, e, false);
            }
            catch (OperationCanceledException)
            {
                IO.Debug("<Exception> OperationCanceledException was thrown.");
            }
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine, which
        /// is modeled as a local machine during testing.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void RemoteSendEvent(MachineId target, Event e)
        {
            this.SendEvent(target, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public override void InvokeMonitor<T>(Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor<T>(null, e);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                string message = "Assertion failure.";
                this.BugFinder.NotifyAssertionFailure(message);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                string message = IO.Format(s, args);
                this.BugFinder.NotifyAssertionFailure(message);
            }
        }

        /// <summary>
        /// Logs the given text.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public override void Log(string s, params object[] args)
        {
            IO.Log(s, args);
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="friendlyName">Friendly name given to the machine for logging</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateMachine(Type type, Event e, string friendlyName)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), "Type '{0}' is not a machine.", type.Name);

            MachineId mid = new MachineId(type, this);
            Object machine = Activator.CreateInstance(type);
            (machine as Machine).SetMachineId(mid);
            (machine as Machine).InitializeStateInformation();
            (machine as Machine).SetFriendlyName(friendlyName);

            bool result = this.MachineMap.TryAdd(mid.Value, machine as Machine);
            this.Assert(result, "Machine {0}({1}) was already created.", type.Name, mid.Value);

            IO.Log("<CreateLog> Machine {0} is created.", (machine as Machine).UniqueFriendlyName);

            Task task = new Task(() =>
            {
                this.BugFinder.NotifyTaskStarted();

                (machine as Machine).GotoStartState(e);
                (machine as Machine).RunEventHandler();

                this.BugFinder.NotifyTaskCompleted();
            });

            lock (this.Lock)
            {
                this.MachineTasks.Add(task);
                this.TaskMap.TryAdd(task.Id, machine as Machine);
            }

            this.BugFinder.NotifyNewTaskCreated(task.Id, machine as Machine);

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(this.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            this.BugFinder.WaitForTaskToStart(task.Id);
            this.BugFinder.Schedule();

            return mid;
        }

        /// <summary>
        /// Tries to create a new remote machine of the given type.
        /// The remote machine is created locally for testing.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <param name="friendlyName">Friendly name given to the machine for logging</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateRemoteMachine(Type type, string endpoint, Event e, string friendlyName)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), "Type '{0}' is not a machine.", type.Name);
            return this.TryCreateMachine(type, e, friendlyName);
        }

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal override void TryCreateMonitor(Type type)
        {
            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a " +
                "subclass of Monitor.\n", type.Name);

            MachineId mid = new MachineId(type, this);
            Object monitor = Activator.CreateInstance(type);
            (monitor as Monitor).SetMachineId(mid);
            (monitor as Monitor).InitializeStateInformation();

            IO.Log("<CreateLog> Monitor {0} is created.", type.Name);

            this.Monitors.Add(monitor as Monitor);
            this.LivenessChecker.RegisterMonitor(monitor as Monitor);

            (monitor as Monitor).GotoStartState();
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        internal override void TryCreateTaskMachine(Task userTask)
        {
            this.Assert(this.TaskScheduler is TaskWrapperScheduler, "Unable to wrap " +
                "the task in a machine, because the task wrapper scheduler is not enabled.\n");

            MachineId mid = new MachineId(typeof(TaskMachine), this);
            TaskMachine taskMachine = new TaskMachine(this.TaskScheduler as TaskWrapperScheduler, userTask);
            taskMachine.SetMachineId(mid);
            
            IO.Log("<CreateLog> TaskMachine({0}) is created.", mid.MVal);

            Task task = new Task(() =>
            {
                this.BugFinder.NotifyTaskStarted();
                taskMachine.Run();
                this.BugFinder.NotifyTaskCompleted();
            });

            lock (this.Lock)
            {
                this.MachineTasks.Add(task);
            }

            this.BugFinder.NotifyNewTaskCreated(task.Id, taskMachine);

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(this.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            this.BugFinder.WaitForTaskToStart(task.Id);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Returns the name of the machine corresponding to a given Id
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <returns>Friendly name of the machine</returns>
        internal string IdToName(MachineId mid)
        {
            Machine machine;
            bool result = this.MachineMap.TryGetValue(mid.Value, out machine);
            if (result)
                return machine.UniqueFriendlyName;
            else
                return string.Format("{0}({1})", mid.Type, mid.MVal);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void Send(AbstractMachine sender, MachineId mid, Event e, bool isStarter)
        {
            if (sender != null)
            {
                e.SetSenderMachine(sender.Id);
            }

            this.SetOperationIdForEvent(e, sender, isStarter);

            if (this.Configuration.BoundOperations && sender != null)
            {
                //this.MachineMap[mid.MVal]
                IO.Log("<SendLog> Machine '{0}' sent event '{1}({2})' to '{3}'.",
                    sender.UniqueFriendlyName, e.GetType().FullName, e.OperationId, IdToName(mid));
            }
            else if (sender != null)
            {
                IO.Log("<SendLog> Machine '{0}' sent event '{1}' to '{2}'.",
                    sender.UniqueFriendlyName, sender.Id.MVal, e.GetType().FullName, IdToName(mid));
            }
            else
            {
                IO.Log("<SendLog> Event '{0}' was sent to '{1}'.",
                    e.GetType().FullName, IdToName(mid));
            }

            Machine machine = this.MachineMap[mid.Value];

            bool runNewHandler = false;
            machine.Enqueue(e, ref runNewHandler);
            
            if (!runNewHandler)
            {
                this.BugFinder.Schedule();
                return;
            }

            machine.SetOperationId(e.OperationId);

            Task task = new Task(() =>
            {
                this.BugFinder.NotifyTaskStarted();
                machine.RunEventHandler();
                this.BugFinder.NotifyTaskCompleted();
            });

            lock (this.Lock)
            {
                this.MachineTasks.Add(task);
                this.TaskMap.TryAdd(task.Id, machine as Machine);
            }

            this.BugFinder.NotifyNewTaskCreated(task.Id, machine);

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(this.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            this.BugFinder.WaitForTaskToStart(task.Id);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine, which
        /// is modeled as a local machine during testing.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void SendRemotely(AbstractMachine sender, MachineId mid, Event e, bool isStarter)
        {
            this.Send(sender, mid, e, isStarter);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal override void Monitor<T>(AbstractMachine sender, Event e)
        {
            foreach (var m in this.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    m.MonitorEvent(e);
                }
            }
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="raiser">Raiser machine</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void Raise(AbstractMachine raiser, Event e, bool isStarter)
        {
            this.SetOperationIdForEvent(e, raiser, isStarter);

            if (this.Configuration.BoundOperations)
            {
                IO.Log("<RaiseLog> Machine '{0}' raised event '{1}({2})'.",
                    raiser.UniqueFriendlyName, e.GetType().FullName, e.OperationId);
            }
            else
            {
                IO.Log("<RaiseLog> Machine '{0}' raised event '{1}'.",
                    raiser.UniqueFriendlyName, e.GetType().FullName);
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal override bool GetNondeterministicChoice(AbstractMachine machine, int maxValue)
        {
            var choice = this.BugFinder.GetNextNondeterministicChoice(maxValue);
            if (machine != null)
            {
                IO.Log("<RandomLog> Machine '{0}' nondeterministically chose '{1}'.",
                    machine.UniqueFriendlyName, machine.Id.MVal, choice);
            }
            else
            {
                IO.Log("<RandomLog> Runtime nondeterministically chose '{0}'.", choice);
            }
            
            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal override bool GetFairNondeterministicChoice(AbstractMachine machine, string uniqueId)
        {
            var choice = this.BugFinder.GetNextNondeterministicChoice(2, uniqueId);
            if (machine != null)
            {
                IO.Log("<RandomLog> Machine '{0}' nondeterministically chose '{1}'.",
                    machine.UniqueFriendlyName, choice);
            }
            else
            {
                IO.Log("<RandomLog> Runtime nondeterministically chose '{0}'.", choice);
            }

            return choice;
        }

        /// <summary>
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        internal override void NotifyDequeuedEvent(Machine machine, Event e)
        {
            if (this.Configuration.BoundOperations)
            {
                IO.Log("<DequeueLog> Machine '{0}' dequeued event '{1}({2})'.",
                    machine.UniqueFriendlyName, e.GetType().FullName, e.OperationId);
            }
            else
            {
                IO.Log("<DequeueLog> Machine '{0}' dequeued event '{1}'.",
                    machine.UniqueFriendlyName, e.GetType().FullName);
            }
            
            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(e.OperationId);
            //if (this.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.BugFinder.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine raised an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        internal override void NotifyRaisedEvent(Machine machine, Event e)
        {
            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(e.OperationId);
            //if (this.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.BugFinder.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyWaitEvent(Machine machine)
        {
            this.BugFinder.NotifyTaskBlockedOnEvent(Task.CurrentId);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        internal override void NotifyReceivedEvent(Machine machine, Event e)
        {
            if (this.Configuration.BoundOperations)
            {
                IO.Log("<ReceiveLog> Machine '{0}' received event '{1}({2})' and unblocked.",
                    machine.UniqueFriendlyName, e.GetType().FullName, e.OperationId);
            }
            else
            {
                IO.Log("<ReceiveLog> Machine '{0}' received event '{1}' and unblocked.",
                    machine.UniqueFriendlyName, e.GetType().FullName);
            }

            this.BugFinder.NotifyTaskReceivedEvent(machine);
            machine.IsWaitingToReceive = false;
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        internal override void NotifyDefaultHandlerFired()
        {
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a scheduling point should be instrumented
        /// due to a wait synchronization operation.
        /// </summary>
        /// <param name="blockingTasks">Blocking tasks</param>
        /// <param name="waitAll">Boolean</param>
        internal void ScheduleOnWait(IEnumerable<Task> blockingTasks, bool waitAll)
        {
            this.Assert(this.BugFinder is TaskAwareBugFindingScheduler,
                "Cannot schedule on wait without enabling the task-aware bug finding scheduler.");
            (this.BugFinder as TaskAwareBugFindingScheduler).NotifyTaskBlocked(
                Task.CurrentId, blockingTasks, waitAll);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Returns the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        internal Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var machine in this.MachineMap.Values)
                {
                    hash = hash + 31 * machine.GetCachedState();
                }

                foreach (var monitor in this.Monitors)
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
        internal void Wait()
        {
            Task[] taskArray = null;

            while (this.IsRunning)
            {
                lock (this.Lock)
                {
                    taskArray = this.MachineTasks.ToArray();
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
                lock (this.Lock)
                {
                    moreTasksExist = taskArray.Length != this.MachineTasks.Count;
                }

                if (!moreTasksExist)
                {
                    break;
                }
            }

            this.IsRunning = false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Sets the operation id for the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        private void SetOperationIdForEvent(Event e, AbstractMachine sender, bool isStarter)
        {
            if (isStarter)
            {
                this.OperationIdCounter++;
                e.SetOperationId(this.OperationIdCounter);
            }
            else if (sender != null)
            {
                e.SetOperationId(sender.OperationId);
            }
            else
            {
                e.SetOperationId(0);
            }
        }

        #endregion

        #region TPL methods

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public override void WaitAll(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAll(Task[] tasks, CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public override void WaitAll(Task[] tasks, int millisecondsTimeout)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public override void WaitAll(Task[] tasks, TimeSpan timeout)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, timeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAll(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public override void WaitAny(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAny(Task[] tasks, CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public override void WaitAny(Task[] tasks, int millisecondsTimeout)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public override void WaitAny(Task[] tasks, TimeSpan timeout)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, timeout);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAny(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an array have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task WhenAll(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task WhenAll(IEnumerable<Task> tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an array have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task> WhenAny(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        #endregion
    }
}
