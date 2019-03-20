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
        internal ISchedulingStrategy HAX_strategy;
        internal MinimizationRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter) : base(configuration, strategy, reporter)
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
    }
}
