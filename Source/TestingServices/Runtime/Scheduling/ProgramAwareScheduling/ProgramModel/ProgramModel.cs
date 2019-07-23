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
        internal IProgramStep ActiveStep;
        internal List<IProgramStep> OrderedSteps;
        private readonly Dictionary<Event, IProgramStep> PendingEventToSendStep;
        private readonly Dictionary<ulong, IProgramStep> MachineIdToCreateStep;
        private readonly Dictionary<ulong, IProgramStep> MachineIdToLastStep;
        // private readonly Dictionary<int, IProgramStep> SendIndexToSendStep; // TODO: Chuck this.
        private readonly Dictionary<ulong, IProgramStep> MachineIdToLatestSendTo;

        private readonly Dictionary<Type, IProgramStep> MonitorTypeToLatestSendTo;

        internal ProgramModel()
        {
            this.MachineIdToCreateStep = new Dictionary<ulong, IProgramStep>();
            this.MachineIdToLastStep = new Dictionary<ulong, IProgramStep>();
            // this.SendIndexToSendStep = new Dictionary<int, IProgramStep>();
            this.PendingEventToSendStep = new Dictionary<Event, IProgramStep>();
            this.MachineIdToLatestSendTo = new Dictionary<ulong, IProgramStep>();
            this.MonitorTypeToLatestSendTo = new Dictionary<Type, IProgramStep>();

            this.OrderedSteps = new List<IProgramStep>();

            this.Initialize(0);
        }

        public void Initialize(ulong testHarnessMachineId)
        {
            // TODO: Come up with a logical, consistent, common first step.
            ProgramStep firstStep = ProgramStep.CreateSpecialProgramStep();

            this.Rootstep = firstStep;
            this.ActiveStep = this.Rootstep;
            this.MachineIdToLastStep[testHarnessMachineId] = this.Rootstep;

            this.OrderedSteps.Add(this.Rootstep);
        }

        public void RecordStep(IProgramStep programStep, int stepIndex)
        {
            if (programStep.ProgramStepType == ProgramStepType.SchedulableStep)
            {
                switch (programStep.OpType)
                {
                    case AsyncOperationType.Create:
                        this.MachineIdToCreateStep.Add(programStep.TargetId, programStep);
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
                        IProgramStep sendStep = this.FindSendStep(programStep);
                        SetCreatedRelation(sendStep, programStep);
                        this.PendingEventToSendStep.Remove(sendStep.EventInfo.Event);
                        break;

                    case AsyncOperationType.Start:
                        IProgramStep creatorStep = this.MachineIdToCreateStep[programStep.SrcId];
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
                IProgramStep previousMachineStep = this.MachineIdToLastStep[programStep.SrcId];

                SetMachineThreadRelation(previousMachineStep, programStep);
            }

            this.MachineIdToLastStep[programStep.SrcId] = programStep;
            this.AppendStepToTotalOrdering(programStep);

            this.ActiveStep = programStep;

            // This line is only so we don't get an unused parameter error
            stepIndex++;
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
                SetMonitorCommunicationRelation(this.MonitorTypeToLatestSendTo[monitorType], this.ActiveStep);
            }

            this.MonitorTypeToLatestSendTo[monitorType] = this.ActiveStep;
        }

        private IProgramStep FindSendStep(IProgramStep receiveStep)
        {
            if (!this.PendingEventToSendStep.TryGetValue(receiveStep.EventInfo.Event, out IProgramStep sendStep))
            {
                throw new ArgumentException("The specified event was not found in the pending set");
            }

            return sendStep;
        }

        private void AppendStepToTotalOrdering(IProgramStep programStep)
        {
            programStep.TotalOrderingIndex = this.OrderedSteps.Count;
            this.OrderedSteps.Add(programStep);
        }

        private static void SetCreatedRelation(IProgramStep parent, IProgramStep child)
        {
            parent.CreatedStep = child;
            child.CreatorParent = parent;
        }

        private static void SetMachineThreadRelation(IProgramStep parent, IProgramStep child)
        {
            parent.NextMachineStep = child;
            child.PrevMachineStep = parent;
        }

        private static void SetInboxOrderingRelation(IProgramStep parent, IProgramStep child)
        {
            parent.NextEnqueuedStep = child;
            child.PrevEnqueuedStep = parent;
        }

        private static void SetMonitorCommunicationRelation(IProgramStep prevStep, IProgramStep currentStep)
        {
            if (prevStep == currentStep)
            {
                return;
            }

            if (prevStep.NextMonitorSteps == null)
            {
                prevStep.NextMonitorSteps = new List<IProgramStep>();
            }

            prevStep.NextMonitorSteps.Add(currentStep);

            if (currentStep.PrevMonitorSteps == null)
            {
                currentStep.PrevMonitorSteps = new List<IProgramStep>();
            }

            currentStep.PrevMonitorSteps.Add(prevStep);
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
            IProgramStep s = step;
            return $"{s.TotalOrderingIndex}:{s.ProgramStepType}:{s.SrcId}:{s.OpType}:{s.TargetId}:{s.BooleanChoice}:{s.IntChoice}:{s.NextMachineStep?.TotalOrderingIndex ?? -1}:{s.CreatedStep?.TotalOrderingIndex ?? -1}";
        }

        // Prints tree, assuming only PrevMachineStep or CreatorStep can be parents
        internal string HAXGetProgramTreeString()
        {
            StringBuilder sb = new StringBuilder();

            HAXSerializeStepProgramTree(sb, this.Rootstep, 0);

            return sb.ToString();
        }

        private static void HAXSerializeStepProgramTree(StringBuilder sb, IProgramStep atNode, int depth)
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
