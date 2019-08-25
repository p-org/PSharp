// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

// Appends a SpecialProgramStep as the NextMachineStep of the operation which called receive
// The actual receive step is a SpecialProgramStep with
//    the SendStep as CreatorParent and
//    the original handler as PrevInboxOrderingStep
#define CREATE_EXPLICIT_RECEIVE_CALLED_STEP

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    internal class ProgramModel
    {
        /// <summary>
        /// If true, the program model appends a ExplicitReceiveCalled step
        /// as NextMachineStep of the step which called the explicit Receive
        /// </summary>
        public const bool MustCreateExplicitReceiveCalledStep = true;

        private const bool ConnectSuccessiveHandlers = false;
        // if true, check if the monitor was indeed invoked by the machine executing CurrentStep
        private const bool AllowRuntimeInvokeMonitor = true;
        private const bool HashMachines = true;
        private const bool UseReflectionToHashMachines = false;

        internal ProgramStep Rootstep;
        internal ProgramStep ActiveStep;
        internal List<ProgramStep> OrderedSteps;

        private readonly Dictionary<ProgramStepEventInfo, ProgramStep> PendingEventToSendStep;
        private readonly Dictionary<ulong, ProgramStep> MachineIdToCreateStep;
        private readonly Dictionary<ulong, ProgramStep> MachineIdToLastStep;
        private readonly Dictionary<ulong, ProgramStep> MachineIdToLatestSendTo;
        private readonly Dictionary<ulong, ProgramStep> MachineIdToLatestReceive;
        private readonly Dictionary<Type, ProgramStep> MonitorTypeToLatestSendTo;
        internal readonly Dictionary<ulong, Machine> MachineIdToMachine;

        internal ProgramStep BugTriggeringStep;
        private readonly Dictionary<Monitor, ProgramStep> HotMonitors;
        private Type LivenessViolatingMonitor;

        private bool IsLivenessBug
        {
            get => this.LivenessViolatingMonitor != null;
        }

        private bool IsRecording;
        private static readonly HashSet<Type> IgnoredMachineHashTypes = new HashSet<Type> { typeof(AsyncMachine), typeof(Machine) };
        private readonly HashSet<ProgramStep> DroppedEventSendSteps;

        internal ProgramModel()
        {
            this.MachineIdToCreateStep = new Dictionary<ulong, ProgramStep>();
            this.MachineIdToLastStep = new Dictionary<ulong, ProgramStep>();
            this.PendingEventToSendStep = new Dictionary<ProgramStepEventInfo, ProgramStep>();
            this.MachineIdToLatestSendTo = new Dictionary<ulong, ProgramStep>();
            this.MachineIdToLatestReceive = new Dictionary<ulong, ProgramStep>();
            this.MonitorTypeToLatestSendTo = new Dictionary<Type, ProgramStep>();
            this.MachineIdToMachine = new Dictionary<ulong, Machine>();

            this.HotMonitors = new Dictionary<Monitor, ProgramStep>();

            this.DroppedEventSendSteps = new HashSet<ProgramStep>();

            this.OrderedSteps = new List<ProgramStep>();

            this.BugTriggeringStep = null;
            this.LivenessViolatingMonitor = null;

            this.Initialize(0);

            this.IsRecording = true;
        }

        internal void StopRecording()
        {
            this.IsRecording = false;
        }

        public void Initialize(ulong testHarnessMachineId)
        {
            // TODO: Come up with a logical, consistent, common first step.
            ProgramStep firstStep = ProgramStep.CreateSpecialProgramStep(0, null, null, AsyncOperationType.Start);

            this.Rootstep = firstStep;
            this.ActiveStep = this.Rootstep;
            this.MachineIdToLastStep[testHarnessMachineId] = this.Rootstep;
            // this.MachineIdToMachine[testHarnessMachineId] = typeof(TestHarnessMachine);

            this.OrderedSteps.Add(this.Rootstep);
        }

        public void RecordStep(ProgramStep programStep, int stepIndex)
        {
            if (!this.IsRecording)
            {
                return;
            }

            if (programStep.ProgramStepType == ProgramStepType.SchedulableStep ||
                programStep.ProgramStepType == ProgramStepType.ExplicitReceiveComplete)
            {
                switch (programStep.OpType)
                {
                    case AsyncOperationType.Create:
                        this.MachineIdToCreateStep.Add(programStep.TargetId, programStep);

                        // A harmless hack
                        this.MachineIdToLatestSendTo[programStep.TargetId] = programStep;
                        break;

                    case AsyncOperationType.Send:
                        this.PendingEventToSendStep[programStep.EventInfo] = programStep;
                        if (this.MachineIdToLatestSendTo.ContainsKey(programStep.TargetId))
                        {
                            SetInboxOrderingRelation(this.MachineIdToLatestSendTo[programStep.TargetId], programStep);
                        }

                        this.MachineIdToLatestSendTo[programStep.TargetId] = programStep;
                        break;

                    case AsyncOperationType.Receive:
                        ProgramStep sendStep = this.FindSendStep(programStep);
                        SetCreatedRelation(sendStep, programStep);
                        this.PendingEventToSendStep.Remove(sendStep.EventInfo);

                        // Connect Successive handlers ( Conenct to start step if there were none )
                        ProgramStep prevReceive = this.MachineIdToLatestReceive[programStep.SrcId];
                        SetDequeueOrderRelation(prevReceive, programStep);
                        this.MachineIdToLatestReceive[programStep.SrcId] = programStep;
                        break;

                    case AsyncOperationType.Start:
                        ProgramStep creatorStep = this.MachineIdToCreateStep[programStep.SrcId];
                        SetCreatedRelation(creatorStep, programStep);

                        this.MachineIdToLatestReceive[programStep.SrcId] = programStep;

                        break;
                }
            }
            else if (programStep.ProgramStepType == ProgramStepType.NonDetBoolStep
                || programStep.ProgramStepType == ProgramStepType.NonDetIntStep)
            {
                SetMachineThreadRelation(this.MachineIdToLastStep[programStep.SrcId], programStep);
            }

            if (programStep.OpType != AsyncOperationType.Start &&
                (programStep.OpType != AsyncOperationType.Receive || ConnectSuccessiveHandlers))
            {
                // Process the machine thread relation
                ProgramStep previousMachineStep = this.MachineIdToLastStep[programStep.SrcId];

                SetMachineThreadRelation(previousMachineStep, programStep);
            }

            this.MachineIdToLastStep[programStep.SrcId] = programStep;
            this.AppendStepToTotalOrdering(programStep);

            this.ActiveStep = programStep;

            // This line is only so we don't get an unused parameter error
            stepIndex++;

            if (HashMachines && this.MachineIdToMachine.TryGetValue(programStep.SrcId, out Machine machineInstance))
            {
                programStep.MachineHash = HashMachine(machineInstance);
            }
        }

        internal void RecordReceiveCalled(Machine machine)
        {
            if (MustCreateExplicitReceiveCalledStep)
            {
                ProgramStep lastStep = this.MachineIdToLastStep[machine.Id.Value];
                ProgramStep explicitReceiveCalledStep = ProgramStep.CreateExplicitReceiveCalledStep(machine.Id.Value);
                SetMachineThreadRelation(lastStep, explicitReceiveCalledStep);
                this.MachineIdToLastStep[machine.Id.Value] = explicitReceiveCalledStep;
            }
        }

        internal void RecordMachineCreation(ulong machineId, Machine machine)
        {
            if (!this.IsRecording)
            {
                return;
            }

            this.MachineIdToMachine.Add(machineId, machine);
        }

        internal void RecordMonitorEvent(Type monitorType, AsyncMachine sender)
        {
            if (!this.IsRecording)
            {
                return;
            }

            // Pray that this is right :p
            if (sender == null)
            {
                if (!AllowRuntimeInvokeMonitor)
                {
                    throw new NotImplementedException("Runtime.InvokeMonitor is disabled in ProgramModel");
                }
            }
            else if (sender.Id.Value != this.ActiveStep.SrcId)
            {
                // Known bug - Blocking receives break this. Once we support that, uncomment.
                // throw new NotImplementedException("This is wrongly implemented");
            }

            if (this.MonitorTypeToLatestSendTo.ContainsKey(monitorType))
            {
                SetMonitorCommunicationRelation(this.MonitorTypeToLatestSendTo[monitorType], this.ActiveStep, monitorType);
            }

            this.MonitorTypeToLatestSendTo[monitorType] = this.ActiveStep;
        }

        internal void RecordSchedulingEnded(bool bugFound)
        {
            if (bugFound)
            {
                // TODO: This is just to use the argument
                foreach (KeyValuePair<Monitor, ProgramStep> kv in this.HotMonitors)
                {
                    if (kv.Key.ExceedsLivenessTemperatureLimit())
                    {
                        this.LivenessViolatingMonitor = kv.Key.GetType();
                        this.BugTriggeringStep = kv.Value;
                        break;
                    }
                }

                if (!this.IsLivenessBug)
                {
                    this.BugTriggeringStep = this.OrderedSteps[this.OrderedSteps.Count - 1];
                }
            }
        }

        internal void RecordEventDropped(ProgramStep sendStep)
        {
            if (!this.IsRecording)
            {
                return;
            }

            this.DroppedEventSendSteps.Add(sendStep);
        }

        private ProgramStep FindSendStep(ProgramStep receiveStep)
        {
            if (!this.PendingEventToSendStep.TryGetValue(receiveStep.EventInfo, out ProgramStep sendStep))
            {
                throw new ArgumentException("The specified event was not found in the pending set");
            }

            return sendStep;
        }

        private void AppendStepToTotalOrdering(ProgramStep programStep)
        {
            programStep.TotalOrderingIndex = this.OrderedSteps.Count;
            this.OrderedSteps.Add(programStep);
        }

        private static void SetCreatedRelation(ProgramStep parent, ProgramStep child)
        {
            parent.CreatedStep = child;
            child.CreatorParent = parent;
        }

        private static void SetMachineThreadRelation(ProgramStep parent, ProgramStep child)
        {
            parent.NextMachineStep = child;
            child.PrevMachineStep = parent;
        }

        private static void SetInboxOrderingRelation(ProgramStep parent, ProgramStep child)
        {
            parent.NextInboxOrderingStep = child;
            child.PrevInboxOrderingStep = parent;
        }

        private static void SetDequeueOrderRelation(ProgramStep parent, ProgramStep child)
        {
            parent.NextInboxOrderingStep = child;
            child.PrevInboxOrderingStep = parent;
        }

        internal void RecordMonitorStateChange(Monitor monitor)
        {
            if (monitor.IsInHotState())
            {
                this.HotMonitors[monitor] = this.ActiveStep;
            }
            else
            {
                if (this.HotMonitors.ContainsKey(monitor))
                {
                    this.HotMonitors.Remove(monitor);
                }
            }
        }

        private static void SetMonitorCommunicationRelation(ProgramStep prevStep, ProgramStep currentStep, Type monitorType)
        {
            if (prevStep == currentStep)
            {
                return;
            }

            if (prevStep.NextMonitorSteps == null)
            {
                prevStep.NextMonitorSteps = new Dictionary<Type, ProgramStep>();
            }

            prevStep.NextMonitorSteps.Add(monitorType, currentStep);

            if (currentStep.PrevMonitorSteps == null)
            {
                currentStep.PrevMonitorSteps = new Dictionary<Type, ProgramStep>();
            }

            currentStep.PrevMonitorSteps.Add(monitorType, prevStep);
        }

        private static int HashMachine(Machine machineInstance)
        {
            int hash = UseReflectionToHashMachines ? ReflectionBasedHasher.HashObject(machineInstance, IgnoredMachineHashTypes) : 1;

            int i = 0;
            Type stateAtI = null;
            do
            {
                stateAtI = machineInstance.GetStateTypeAtStackIndex(i);
                hash *= stateAtI?.GetHashCode() ?? 1;
                i++;
            }
            while ( stateAtI != null);

            return hash;
        }

        internal ProgramModelSummary GetProgramSummary()
        {
            return new ProgramModelSummary(this.Rootstep, this.BugTriggeringStep, new List<ProgramStep>(this.DroppedEventSendSteps), this.OrderedSteps.Count, this.LivenessViolatingMonitor);
        }
    }
}
