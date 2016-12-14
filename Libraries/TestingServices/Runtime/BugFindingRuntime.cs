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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Liveness;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Threading;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Machines;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Class implementing the P# bug-finding runtime.
    /// </summary>
    internal sealed class PSharpBugFindingRuntime : PSharpRuntime, IDisposable
    {
        #region fields

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The bug trace.
        /// </summary>
        internal BugTrace BugTrace;

        /// <summary>
        /// A map from unique machine ids to action traces.
        /// Only used for dynamic data race detection.
        /// </summary>
        internal IDictionary<MachineId, MachineActionTrace> MachineActionTraceMap;

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
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        internal CoverageInfo CoverageInfo;

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private int OperationIdCounter;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="coverageInfo">CoverageInfo</param>
        /// </summary>
        internal PSharpBugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy,
            CoverageInfo coverageInfo)
            : base(configuration)
        {
            this.RootTaskId = Task.CurrentId;
            
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

            this.ScheduleTrace = new ScheduleTrace();
            this.BugTrace = new BugTrace();
            this.MachineActionTraceMap = new ConcurrentDictionary<MachineId, MachineActionTrace>();

            this.StateCache = new StateCache(this);
            this.LivenessChecker = new LivenessChecker(this, strategy);
            this.CoverageInfo = coverageInfo;

            this.OperationIdCounter = 0;
        }

        /// <summary>
        /// Creates a new machine of the specified type and with
        /// the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.TryCreateMachine(creator, type, null, e);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and
        /// with the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.TryCreateMachine(creator, type, friendlyName, e);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and with
        /// the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.TryCreateRemoteMachine(creator, type, null, endpoint, e);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and name, and
        /// with the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.TryCreateRemoteMachine(creator, type, friendlyName, endpoint, e);
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

            this.Send(base.GetCurrentMachine(), target, e, false);
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

        #region internal methods

        /// <summary>
        /// Tries to create a new machine of the specified type.
        /// </summary>
        /// <param name="creator">Creator machine</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateMachine(Machine creator, Type type,
            string friendlyName, Event e)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' " +
                "is not a machine.");

            if (creator != null)
            {
                creator.AssertNoPendingRGP("CreateMachine");
            }

            MachineId mid = new MachineId(type, friendlyName, this);

            var isMachineNewlyConstructed = false;
            if (!MachineConstructorMap.ContainsKey(type))
            {
                Func<Machine> constructor = Expression.Lambda<Func<Machine>>(
                    Expression.New(type.GetConstructor(Type.EmptyTypes))).Compile();
                MachineConstructorMap[type] = constructor;
                isMachineNewlyConstructed = true;
            }

            Machine machine = MachineConstructorMap[type]();

            machine.SetMachineId(mid);
            machine.InitializeStateInformation();

            if (this.Configuration.ReportCodeCoverage && isMachineNewlyConstructed)
            {
                this.ReportCodeCoverageOfMachine(machine);
            }

            bool result = this.MachineMap.TryAdd(mid.Value, machine);
            this.Assert(result, $"Machine '{mid}' was already created.");

            IO.Log($"<CreateLog> Machine '{mid}' is created.");
            
            this.BugTrace.AddCreateMachineStep(creator, mid, e == null ? null : new EventInfo(e));
            if (this.Configuration.EnableDataRaceDetection)
            {
                // Traces machine actions, if data-race detection is enabled.
                this.MachineActionTraceMap.Add(mid, new MachineActionTrace(mid));
                if (creator != null && MachineActionTraceMap.Keys.Contains(creator.Id))
                {
                    this.MachineActionTraceMap[creator.Id].AddCreateMachineInfo(mid);
                }
            }

            Task task = new Task(() =>
            {
                try
                {
                    this.BugFinder.NotifyTaskStarted();
                    machine.GotoStartState(e);
                    machine.RunEventHandler();
                    this.BugFinder.NotifyTaskCompleted();
                }
                finally
                {
                    this.TaskMap.TryRemove(Task.CurrentId.Value, out machine);
                }
            });

            this.MachineTasks.Add(task);
            base.TaskMap.TryAdd(task.Id, machine);

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

            return mid;
        }

        /// <summary>
        /// Tries to create a new remote machine of the specified type.
        /// </summary>
        /// <param name="creator">Creator machine</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateRemoteMachine(Machine creator, Type type,
            string friendlyName, string endpoint, Event e)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' " +
                "is not a machine.");

            if (creator != null)
            {
                creator.AssertNoPendingRGP("CreateRemoteMachine");
            }

            return this.TryCreateMachine(creator, type, friendlyName, e);
        }

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal override void TryCreateMonitor(Type type)
        {
            this.Assert(type.IsSubclassOf(typeof(Monitor)), $"Type '{type.Name}' " +
                "is not a subclass of Monitor.\n");

            MachineId mid = new MachineId(type, null, this);
            Object monitor = Activator.CreateInstance(type);
            (monitor as Monitor).SetMachineId(mid);
            (monitor as Monitor).InitializeStateInformation();

            IO.Log($"<CreateLog> Monitor '{type.Name}' is created.");

            this.ReportCodeCoverageOfMachine(monitor as Monitor);
            this.BugTrace.AddCreateMonitorStep(mid);

            base.Monitors.Add(monitor as Monitor);
            this.LivenessChecker.RegisterMonitor(monitor as Monitor);

            (monitor as Monitor).GotoStartState();
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        internal override void TryCreateTaskMachine(Task userTask)
        {
            this.Assert(this.TaskScheduler is TaskWrapperScheduler, "Unable to wrap the " +
                "task in a machine, because the task wrapper scheduler is not enabled.\n");

            MachineId mid = new MachineId(typeof(TaskMachine), null, this);
            TaskMachine taskMachine = new TaskMachine(this.TaskScheduler as TaskWrapperScheduler,
                userTask);
            taskMachine.SetMachineId(mid);

            if (Task.CurrentId != null && TaskMap.ContainsKey((int)Task.CurrentId) &&
                this.Configuration.EnableDataRaceDetection)
            {
                // Traces machine actions, if data-race detection is enabled.
                this.MachineActionTraceMap.Add(mid, new MachineActionTrace(mid));
                var currentMachineId = this.GetCurrentMachineId();
                this.Assert(currentMachineId != null, "Unable to find current machine Id");
                this.MachineActionTraceMap[currentMachineId].AddTaskMachineCreationInfo(userTask.Id, mid);
            }

            IO.Log($"<CreateLog> '{mid}' is created.");

            Task task = new Task(() =>
            {
                this.BugFinder.NotifyTaskStarted();
                taskMachine.Run();
                this.BugFinder.NotifyTaskCompleted();
            });

            this.MachineTasks.Add(task);

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
                sender.AssertNoPendingRGP("Send");
            }

            EventOriginInfo originInfo = null;
            if (sender != null && sender is Machine)
            {
                originInfo = new EventOriginInfo(sender.Id,
                    (sender as Machine).GetType().Name,
                    StateGroup.GetQualifiedStateName((sender as Machine).CurrentState));
            }
            else
            {
                // Message comes from outside P#.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            EventInfo eventInfo = new EventInfo(e, originInfo);
            this.SetOperationIdForEvent(eventInfo, sender, isStarter);

            if (this.Configuration.BoundOperations && sender != null)
            {
                IO.Log($"<SendLog> Machine '{sender.Id}' sent event " +
                    $"'{eventInfo.EventName}({eventInfo.OperationId})' to '{mid}'.");
            }
            else if (sender != null)
            {
                IO.Log($"<SendLog> Machine '{sender.Id}' sent event " +
                    $"'{eventInfo.EventName}' to '{mid}'.");
            }
            else
            {
                IO.Log($"<SendLog> Event '{eventInfo.EventName}' was sent to '{mid}'.");
            }

            if (sender != null)
            {
                this.BugTrace.AddSendEventStep(sender.Id, this.GetStateNameOfMachine(sender),
                    eventInfo, mid);
                if (this.Configuration.EnableDataRaceDetection)
                {
                    // Traces machine actions, if data-race detection is enabled.
                    this.MachineActionTraceMap[sender.Id].AddSendActionInfo(mid, e);
                }
            }

            Machine machine = null;
            if (!this.MachineMap.TryGetValue(mid.Value, out machine))
            {
                return;
            }

            bool runNewHandler = false;
            machine.Enqueue(eventInfo, ref runNewHandler);
            
            if (!runNewHandler)
            {
                this.BugFinder.Schedule();
                return;
            }

            machine.SetOperationId(eventInfo.OperationId);

            Task task = new Task(() =>
            {
                try
                {
                    this.BugFinder.NotifyTaskStarted();
                    machine.RunEventHandler();
                    this.BugFinder.NotifyTaskCompleted();
                }
                finally
                {
                    this.TaskMap.TryRemove(Task.CurrentId.Value, out machine);
                }
            });
            
            this.MachineTasks.Add(task);
            base.TaskMap.TryAdd(task.Id, machine);

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
            if (sender != null)
            {
                sender.AssertNoPendingRGP("Monitor");
            }

            foreach (var m in base.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    if(this.Configuration.ReportCodeCoverage)
                    {
                        this.ReportCodeCoverageOfMonitorEvent(sender, m, e);
                        this.ReportCodeCoverageOfMonitorTransition(m, e);
                    }

                    m.MonitorEvent(e);
                }
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal override bool GetNondeterministicBooleanChoice(
            AbstractMachine machine, int maxValue)
        {
            if (machine != null)
            {
                machine.AssertNoPendingRGP("Random");
            }

            var choice = this.BugFinder.GetNextNondeterministicBooleanChoice(maxValue);
            if (machine != null)
            {
                IO.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{choice}'.");
            }
            else
            {
                IO.Log($"<RandomLog> Runtime nondeterministically chose '{choice}'.");
            }
            
            this.BugTrace.AddRandomChoiceStep(machine == null ? null : machine.Id, this.GetStateNameOfMachine(machine), choice);

            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal override bool GetFairNondeterministicBooleanChoice(
            AbstractMachine machine, string uniqueId)
        {
            if (machine != null)
            {
                machine.AssertNoPendingRGP("FairRandom");
            }

            var choice = this.BugFinder.GetNextNondeterministicBooleanChoice(2, uniqueId);
            if (machine != null)
            {
                IO.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{choice}'.");
            }
            else
            {
                IO.Log($"<RandomLog> Runtime nondeterministically chose '{choice}'.");
            }
            
            this.BugTrace.AddRandomChoiceStep(machine == null ? null : machine.Id, this.GetStateNameOfMachine(machine), choice);

            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal override int GetNondeterministicIntegerChoice(
            AbstractMachine machine, int maxValue)
        {
            if (machine != null)
            {
                machine.AssertNoPendingRGP("RandomInteger");
            }

            var choice = this.BugFinder.GetNextNondeterministicIntegerChoice(maxValue);
            if (machine != null)
            {
                IO.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{choice}'.");
            }
            else
            {
                IO.Log($"<RandomLog> Runtime nondeterministically chose '{choice}'.");
            }

            this.BugTrace.AddRandomChoiceStep(machine == null ? null : machine.Id, this.GetStateNameOfMachine(machine), choice);

            return choice;
        }

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyEnteredState(AbstractMachine machine)
        {
            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;
                this.BugTrace.AddGotoStateStep(machine.Id, machineState);

                IO.Log($"<StateLog> Machine '{machine.Id}' enters " +
                    $"state '{machineState}'.");

            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateNameWithTemperature;
                this.BugTrace.AddGotoStateStep(machine.Id, monitorState);

                string liveness = "";

                if ((machine as Monitor).IsInHotState())
                {
                    liveness = "'hot' ";
                }
                else if ((machine as Monitor).IsInColdState())
                {
                    liveness = "'cold' ";
                }

                IO.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' " +
                    $"enters {liveness}state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyExitedState(AbstractMachine machine)
        {
            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;

                IO.Log($"<StateLog> Machine '{machine.Id}' exits " +
                    $"state '{machineState}'.");
            }
            else if (machine is Monitor)
            {
                string liveness = "";
                string monitorState = (machine as Monitor).CurrentStateName;

                if ((machine as Monitor).IsInHotState())
                {
                    liveness = "'hot' ";
                    monitorState += "[hot]";
                }
                else if ((machine as Monitor).IsInColdState())
                {
                    liveness = "'cold' ";
                    monitorState += "[cold]";
                }

                IO.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' " +
                    $"exits {liveness}state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(AbstractMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;
                this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);

                IO.Log($"<ActionLog> Machine '{machine.Id}' invoked action " +
                    $"'{action.Name}' in state '{machineState}'.");

                if (this.Configuration.EnableDataRaceDetection)
                {
                    // Traces machine actions, if data-race detection is enabled.
                    this.MachineActionTraceMap[machine.Id].AddInvocationActionInfo(action.Name, receivedEvent);
                }
            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateName;
                this.BugTrace.AddInvokeActionStep(machine.Id, monitorState, action);

                IO.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' executed " +
                    $"action '{action.Name}' in state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            if (this.Configuration.BoundOperations)
            {
                IO.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                    $"event '{eventInfo.EventName}({eventInfo.OperationId})'.");
            }
            else
            {
                IO.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                    $"event '{eventInfo.EventName}'.");
            }
            
            this.BugTrace.AddDequeueEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(eventInfo.OperationId);
            
            if (this.Configuration.ReportCodeCoverage)
            {
                this.ReportCodeCoverageOfReceivedEvent(machine, eventInfo);
                this.ReportCodeCoverageOfStateTransition(machine, eventInfo);
            }

            //if (this.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.BugFinder.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine called Pop.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="fromState">Top of the stack state</param>
        /// <param name="toState">Next to top state of the stack</param>
        internal override void NotifyPop(Machine machine, Type fromState, Type toState)
        {
            machine.AssertCorrectRGPInvocation();

            if(this.Configuration.ReportCodeCoverage)
            {
                this.ReportCodeCoverageOfPopTransition(machine, fromState, toState);
            }
        }

        /// <summary>
        /// Notifies that a machine raised an event.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void NotifyRaisedEvent(AbstractMachine machine, EventInfo eventInfo,
            bool isStarter)
        {
            machine.AssertCorrectRGPInvocation();

            if (machine is Machine)
            {
                this.SetOperationIdForEvent(eventInfo, machine, isStarter);

                string machineState = (machine as Machine).CurrentStateName;
                this.BugTrace.AddRaiseEventStep(machine.Id, machineState, eventInfo);

                if (this.Configuration.BoundOperations)
                {
                    IO.Log($"<RaiseLog> Machine '{machine.Id}' raised " +
                        $"event '{eventInfo.EventName}({eventInfo.OperationId})'.");
                }
                else
                {
                    IO.Log($"<RaiseLog> Machine '{machine.Id}' raised " +
                        $"event '{eventInfo.EventName}'.");
                }
            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateName;
                this.BugTrace.AddRaiseEventStep(machine.Id, monitorState, eventInfo);

                IO.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' raised " +
                    $"event '{eventInfo.EventName}'.");

                if (this.Configuration.ReportCodeCoverage)
                {
                    this.ReportCodeCoverageOfMonitorTransition(machine as Monitor, eventInfo.Event);
                }
            }
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyReceiveCalled(AbstractMachine machine)
        {
            machine.AssertNoPendingRGP("Receive");
        }

        /// <summary>
        /// Notifies that a machine handles a raised event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(eventInfo.OperationId);
            
            if (this.Configuration.ReportCodeCoverage)
            {
                this.ReportCodeCoverageOfStateTransition(machine, eventInfo);
            }

            //if (this.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.BugFinder.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one
        /// or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="events">Events</param>
        internal override void NotifyWaitEvents(Machine machine, string events)
        {
            this.BugTrace.AddWaitToReceiveStep(machine.Id, machine.CurrentStateName, events);

            IO.Log($"<ReceiveLog> Machine '{machine.Id}' " +
                $"is waiting on events:{events}.");

            this.BugFinder.NotifyTaskBlockedOnEvent(Task.CurrentId);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            this.BugTrace.AddReceivedEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            if (this.Configuration.BoundOperations)
            {
                IO.Log($"<ReceiveLog> Machine '{machine.Id}' received " +
                    $"event '{eventInfo.EventName}({eventInfo.OperationId})' and unblocked.");
            }
            else
            {
                IO.Log($"<ReceiveLog> Machine '{machine.Id}' received " +
                    $"event '{eventInfo.EventName}' and unblocked.");
            }

            this.BugFinder.NotifyTaskReceivedEvent(machine);
            machine.IsWaitingToReceive = false;
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyHalted(Machine machine)
        {
            this.BugTrace.AddHaltStep(machine.Id, null);
            IO.Log($"<HaltLog> Machine '{machine.Id}' halted.");
            this.MachineMap.TryRemove(machine.Id.Value, out machine);
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

                foreach (var monitor in base.Monitors)
                {
                    hash = hash + 31 * monitor.GetCachedState();
                }

                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Reports code coverage for the specified received event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        private void ReportCodeCoverageOfReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            string originMachine = eventInfo.OriginInfo.SenderMachineName;
            string originState = eventInfo.OriginInfo.SenderStateName;
            string edgeLabel = eventInfo.EventType.Name;
            string destMachine = machine.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(machine.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports code coverage for the specified monitor event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="monitor">Monitor</param>
        /// <param name="e">Event</param>
        private void ReportCodeCoverageOfMonitorEvent(AbstractMachine sender, Monitor monitor, Event e)
        {
            string originMachine = (sender == null) ? "Env" : sender.GetType().Name;
            string originState = (sender == null) ? "Env" :
                (sender is Machine) ? StateGroup.GetQualifiedStateName((sender as Machine).CurrentState) :
                (sender is Monitor) ? StateGroup.GetQualifiedStateName((sender as Monitor).CurrentState) :
                "Env";

            string edgeLabel = e.GetType().Name;
            string destMachine = monitor.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(monitor.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports code coverage for the specified machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void ReportCodeCoverageOfMachine(AbstractMachine machine)
        {
            var machineName = machine.GetType().Name;

            // fetch states
            var states = machine.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(machineName, state);
            }

            // fetch registered events
            var pairs = machine.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(machineName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports code coverage for the specified state transition.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        private void ReportCodeCoverageOfStateTransition(Machine machine, EventInfo eventInfo)
        {
            string originMachine = machine.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(machine.CurrentState);
            string destMachine = machine.GetType().Name;

            string edgeLabel = "";
            string destState = "";
            if (eventInfo.Event is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = StateGroup.GetQualifiedStateName((eventInfo.Event as GotoStateEvent).State);
            }
            else if (machine.GotoTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    machine.GotoTransitions[eventInfo.EventType].TargetState);
            }
            else if(machine.PushTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    machine.PushTransitions[eventInfo.EventType].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }


        /// <summary>
        /// Reports code coverage for a pop transition.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="fromState">Top of the stack state</param>
        /// <param name="toState">Next to top state of the stack</param>
        private void ReportCodeCoverageOfPopTransition(Machine machine, Type fromState, Type toState)
        {
            string originMachine = machine.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(fromState);
            string destMachine = machine.GetType().Name;            
            string edgeLabel = "pop";
            string destState = StateGroup.GetQualifiedStateName(toState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }
        
        /// <summary>
        /// Reports code coverage for the specified state transition.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="e">Event</param>
        private void ReportCodeCoverageOfMonitorTransition(Monitor monitor, Event e)
        {
            string originMachine = monitor.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(monitor.CurrentState);
            string destMachine = originMachine;

            string edgeLabel = "";
            string destState = "";
            if (e is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = StateGroup.GetQualifiedStateName((e as GotoStateEvent).State);
            }
            else if (monitor.GotoTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().Name;
                destState = StateGroup.GetQualifiedStateName(
                    monitor.GotoTransitions[e.GetType()].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Sets the operation id for the given event.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        private void SetOperationIdForEvent(EventInfo eventInfo, AbstractMachine sender, bool isStarter)
        {
            if (isStarter)
            {
                this.OperationIdCounter++;
                eventInfo.SetOperationId(this.OperationIdCounter);
            }
            else if (sender != null)
            {
                eventInfo.SetOperationId(sender.OperationId);
            }
            else
            {
                eventInfo.SetOperationId(0);
            }
        }

        /// <summary>
        /// Returns the state name of the specified machine,
        /// if the machine is in such a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <returns>StateName</returns>
        private string GetStateNameOfMachine(AbstractMachine machine)
        {
            string machineState = null;
            if (machine is Machine)
            {
                machineState = (machine as Machine).CurrentStateName;
            }
            else if (machine is Monitor)
            {
                machineState = (machine as Monitor).CurrentStateName;
            }

            return machineState;
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

        #region cleanup methods

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            base.Monitors.Clear();
            this.MachineActionTraceMap.Clear();

            this.LivenessChecker = null;
            this.StateCache = null;
            this.ScheduleTrace = null;
            this.BugTrace = null;
        }

        #endregion
    }
}
