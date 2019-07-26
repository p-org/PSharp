// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    internal class ProgramModel
    {
        private const bool ConnectSuccessiveHandlers = false;

        internal ProgramStep Rootstep;
        internal ProgramStep ActiveStep;
        internal List<ProgramStep> OrderedSteps;

        private readonly Dictionary<Event, ProgramStep> PendingEventToSendStep;
        private readonly Dictionary<ulong, ProgramStep> MachineIdToCreateStep;
        private readonly Dictionary<ulong, ProgramStep> MachineIdToLastStep;
        // private readonly Dictionary<int, IProgramStep> SendIndexToSendStep; // TODO: Chuck this.
        private readonly Dictionary<ulong, ProgramStep> MachineIdToLatestSendTo;
        private readonly Dictionary<Type, ProgramStep> MonitorTypeToLatestSendTo;
        internal readonly Dictionary<ulong, Type> MachineIdToType;

        internal ProgramStep BugTriggeringStep;

        internal ProgramModel()
        {
            this.MachineIdToCreateStep = new Dictionary<ulong, ProgramStep>();
            this.MachineIdToLastStep = new Dictionary<ulong, ProgramStep>();
            // this.SendIndexToSendStep = new Dictionary<int, IProgramStep>();
            this.PendingEventToSendStep = new Dictionary<Event, ProgramStep>();
            this.MachineIdToLatestSendTo = new Dictionary<ulong, ProgramStep>();
            this.MonitorTypeToLatestSendTo = new Dictionary<Type, ProgramStep>();
            this.MachineIdToType = new Dictionary<ulong, Type>();

            this.OrderedSteps = new List<ProgramStep>();

            this.Initialize(0);
        }

        public void Initialize(ulong testHarnessMachineId)
        {
            // TODO: Come up with a logical, consistent, common first step.
            ProgramStep firstStep = ProgramStep.CreateSpecialProgramStep();

            this.Rootstep = firstStep;
            this.ActiveStep = this.Rootstep;
            this.MachineIdToLastStep[testHarnessMachineId] = this.Rootstep;
            this.MachineIdToType[testHarnessMachineId] = typeof(TestHarnessMachine);

            this.OrderedSteps.Add(this.Rootstep);
        }

        public void RecordStep(ProgramStep programStep, int stepIndex)
        {
            if (programStep.ProgramStepType == ProgramStepType.SchedulableStep)
            {
                switch (programStep.OpType)
                {
                    case AsyncOperationType.Create:
                        this.MachineIdToCreateStep.Add(programStep.TargetId, programStep);

                        // A harmless hack
                        this.MachineIdToLatestSendTo[programStep.TargetId] = programStep;
                        break;

                    case AsyncOperationType.Send:
                        this.PendingEventToSendStep[programStep.EventInfo.Event] = programStep;
                        if (this.MachineIdToLatestSendTo.ContainsKey(programStep.TargetId))
                        {
                            SetInboxOrderingRelation( this.MachineIdToLatestSendTo[programStep.TargetId], programStep);
                        }

                        this.MachineIdToLatestSendTo[programStep.TargetId] = programStep;
                        break;

                    case AsyncOperationType.Receive:
                        ProgramStep sendStep = this.FindSendStep(programStep);
                        SetCreatedRelation(sendStep, programStep);
                        this.PendingEventToSendStep.Remove(sendStep.EventInfo.Event);
                        break;

                    case AsyncOperationType.Start:
                        ProgramStep creatorStep = this.MachineIdToCreateStep[programStep.SrcId];
                        SetCreatedRelation(creatorStep, programStep);
                        break;
                }
            }
            else if (programStep.ProgramStepType == ProgramStepType.NonDetBoolStep
                || programStep.ProgramStepType == ProgramStepType.NonDetIntStep)
            {
                SetMachineThreadRelation(this.MachineIdToLastStep[programStep.SrcId], programStep);
            }

            if (programStep.OpType != AsyncOperationType.Start &&
                (programStep.OpType != AsyncOperationType.Receive || ConnectSuccessiveHandlers) )
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
        }

        internal void RecordMachineType(ulong machineId, Type type)
        {
            this.MachineIdToType.Add(machineId, type);
        }

        internal void RecordMonitorEvent(Type monitorType, AsyncMachine sender)
        {
            // Pray that this is right :p
            if (sender.Id.Value != this.ActiveStep.SrcId)
            {
               throw new NotImplementedException("This is wrongly implemented");
            }

            if (this.MonitorTypeToLatestSendTo.ContainsKey(monitorType))
            {
                SetMonitorCommunicationRelation(this.MonitorTypeToLatestSendTo[monitorType], this.ActiveStep, monitorType);
            }

            this.MonitorTypeToLatestSendTo[monitorType] = this.ActiveStep;
        }

        internal void RecordSchedulingEnded(bool bugFound, bool isLivenessBug)
        {
            // TODO: This is just to use the argument
            isLivenessBug = false;
            if (bugFound)
            {
                this.BugTriggeringStep = this.OrderedSteps[this.OrderedSteps.Count - 1];
            }
        }

        private ProgramStep FindSendStep(ProgramStep receiveStep)
        {
            if (!this.PendingEventToSendStep.TryGetValue(receiveStep.EventInfo.Event, out ProgramStep sendStep))
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
            parent.NextEnqueuedStep = child;
            child.PrevEnqueuedStep = parent;
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

        internal string SerializeProgramTrace()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ProgramStep step in this.OrderedSteps)
            {
                sb.Append(SerializeStep(step)).Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        private static string SerializeStep(ProgramStep step)
        {
            ProgramStep s = step;
            return $"{s.TotalOrderingIndex}:{s.ProgramStepType}:{s.SrcId}:{s.OpType}:{s.TargetId}:{s.BooleanChoice}:{s.IntChoice}:{s.NextMachineStep?.TotalOrderingIndex ?? -1}:{s.CreatedStep?.TotalOrderingIndex ?? -1}";
        }

        // Prints tree, assuming only PrevMachineStep or CreatorStep can be parents
        internal string HAXGetProgramTreeString()
        {
            StringBuilder sb = new StringBuilder();

            HAXSerializeStepProgramTree(sb, this.Rootstep, 0);

            return sb.ToString();
        }

        private static void HAXSerializeStepProgramTree(StringBuilder sb, ProgramStep atNode, int depth)
        {
            sb.Append(new string('\t', depth) + $"*{atNode}\n");
            if (atNode.NextMachineStep != null)
            {
                HAXSerializeStepProgramTree(sb, atNode.NextMachineStep, depth + 1);
            }

            if (atNode.CreatedStep != null)
            {
                HAXSerializeStepProgramTree(sb, atNode.CreatedStep, depth + 1);
            }
        }
    }
}
