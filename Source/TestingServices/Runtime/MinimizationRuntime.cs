using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    internal class MinimizationRuntime : TestingRuntime
    {
        internal MinimizationStrategy HAX_strategy;
        internal MinimizationRuntime(Configuration configuration, MinimizationStrategy strategy, IRegisterRuntimeOperation reporter) : base(configuration, strategy, reporter)
        {
            this.HAX_strategy = strategy;
        }

        protected override EventInfo EnqueueEvent(Machine machine, Event e, BaseMachine sender, Guid operationGroupId, bool mustHandle, ref bool runNewHandler)
        {
            EventOriginInfo originInfo = null;
            if (sender != null && sender is Machine)
            {
                originInfo = new EventOriginInfo(sender.Id, (sender as Machine).GetType().Name,
                    (sender as Machine).CurrentState == null ? "None" :
                    StateGroup.GetQualifiedStateName((sender as Machine).CurrentState));
            }
            else
            {
                // Message comes from outside P#.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            EventInfo eventInfo = new EventInfo(e, originInfo, Scheduler.ScheduledSteps);
            eventInfo.SetOperationGroupId(operationGroupId);
            eventInfo.SetMustHandle(mustHandle);

            var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
            bool deliver = (HAX_strategy as MinimizationStrategy).ShouldDeliverEvent(sender, e, machine);
            if (!deliver)
            {
                this.Logger.OnSend(machine.Id, sender?.Id, senderState,
                    "__DROPPED__" + e.GetType().FullName, operationGroupId, isTargetHalted: false);
            }
            else
            {
                this.Logger.OnSend(machine.Id, sender?.Id, senderState,
                    e.GetType().FullName, operationGroupId, isTargetHalted: false);

                if (sender != null)
                {
                    var stateName = sender is Machine ? (sender as Machine).CurrentStateName : "";
                    this.BugTrace.AddSendEventStep(sender.Id, stateName, eventInfo, machine.Id);
                    if (base.Configuration.EnableDataRaceDetection)
                    {
                        this.Reporter.RegisterEnqueue(sender.Id, machine.Id, e, (ulong)Scheduler.ScheduledSteps);
                    }
                }

                machine.Enqueue(eventInfo, ref runNewHandler);
            }

            return eventInfo;
        }

        internal override void Monitor(Type type, BaseMachine sender, Event e)
        {
            base.Monitor(type, sender, e);
            HAX_strategy.recordMonitorEvent(type, sender, e);
        }


        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            base.NotifyEnteredState(monitor);
            if (monitor.IsInHotState())
            {
                HAX_strategy.recordEnterHotState(monitor);
            }
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal override void NotifyExitedState(Monitor monitor)
        {
            base.NotifyExitedState(monitor);
            HAX_strategy.recordExitHotState(monitor);
        }


        internal override MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            MachineId returnedMid = base.CreateMachine(mid, type, friendlyName, e, creator, operationGroupId);
            this.NotifyMachineCreated(returnedMid, type, creator);
            return returnedMid;
        }

        private void NotifyMachineCreated(MachineId mid, Type type, Machine creator)
        {
            HAX_strategy.recordMachineCreated(mid, type, creator);
        }
    }
}
