// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    internal class ProgramModel
    {
        private const bool ConnectSuccessiveHandlers = false;

        internal ProgramStep Rootstep;
        internal ProgramStep ActiveStep;
        internal List<IProgramStep> OrderedSteps;

        private readonly Dictionary<ulong, IProgramStep> MachineIdToCreateStep;
        private readonly Dictionary<ulong, IProgramStep> MachineIdToLastStep;
        private readonly Dictionary<int, IProgramStep> SendIndexToSendStep;
        private readonly Dictionary<ulong, IProgramStep> MachineIdToLatestSendTo;

        internal ProgramModel()
        {
            this.MachineIdToCreateStep = new Dictionary<ulong, IProgramStep>();
            this.MachineIdToLastStep = new Dictionary<ulong, IProgramStep>();
            this.SendIndexToSendStep = new Dictionary<int, IProgramStep>();
            this.MachineIdToLatestSendTo = new Dictionary<ulong, IProgramStep>();

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
                        this.SendIndexToSendStep[programStep.EventInfo.SendStep] = programStep;
                        if (this.MachineIdToLatestSendTo.ContainsKey(programStep.TargetId))
                        {
                            SetInboxOrderingRelation( this.MachineIdToLatestSendTo[programStep.TargetId], programStep);
                        }

                        this.MachineIdToLatestSendTo[programStep.TargetId] = programStep;
                        break;

                    case AsyncOperationType.Receive:
                        IProgramStep sendStep = this.SendIndexToSendStep[programStep.EventInfo.SendStep];
                        SetCreatedRelation(sendStep, programStep);
                        break;
                }
            }
            else if (programStep.ProgramStepType == ProgramStepType.NonDetBoolStep
                || programStep.ProgramStepType == ProgramStepType.NonDetIntStep)
            {
                // Do nothing special
            }

            // Process the machine thread relation
            IProgramStep previousMachineStep = this.MachineIdToLastStep[programStep.SrcId];

            if (programStep.OpType != AsyncOperationType.Receive || ConnectSuccessiveHandlers)
            {
                SetMachineThreadRelation(previousMachineStep, programStep);
            }

            this.MachineIdToLastStep[programStep.SrcId] = programStep;

            this.AppendStepToTotalOrdering(programStep);

            if (programStep.OpType == AsyncOperationType.Create )
            {
                ProgramStep startStep = new ProgramStep(AsyncOperationType.Start, programStep.TargetId, programStep.TargetId, null);
                this.MachineIdToLastStep.Add(programStep.TargetId, startStep);
                SetCreatedRelation(programStep, startStep);
                this.AppendStepToTotalOrdering(startStep);
            }

            // This line is only so we don't get an unused parameter error
            stepIndex++;
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
