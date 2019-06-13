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
        internal ProgramStep Rootstep;
        internal ProgramStep ActiveStep;
        internal List<IProgramStep> OrderedSteps;

        private readonly Dictionary<ulong, IProgramStep> MachineIdToCreateStep;
        private readonly Dictionary<ulong, IProgramStep> MachineIdToLastStep;
        private readonly Dictionary<int, IProgramStep> SendIndexToSendStep;

        internal ProgramModel()
        {
            this.MachineIdToCreateStep = new Dictionary<ulong, IProgramStep>();
            this.MachineIdToLastStep = new Dictionary<ulong, IProgramStep>();
            this.SendIndexToSendStep = new Dictionary<int, IProgramStep>();

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
        }

        public void RecordStep(IProgramStep programStep, int stepIndex)
        {
            if ( programStep.ProgramStepType == ProgramStepType.SchedulableStep )
            {
                switch (programStep.OpType)
                {
                    case AsyncOperationType.Create:
                        this.MachineIdToCreateStep.Add(programStep.TargetId, programStep);
                        this.MachineIdToLastStep.Add(programStep.TargetId, programStep);

                        break;

                    case AsyncOperationType.Send:
                        // assert(programStep.EventInfo.SendStep == stepIndex);
                        this.SendIndexToSendStep[programStep.EventInfo.SendStep] = programStep;
                        break;

                    case AsyncOperationType.Receive:
                        IProgramStep sendStep = this.SendIndexToSendStep[programStep.EventInfo.SendStep];
                        SetCreatedRelation(sendStep, programStep);
                        break;
                }
            }
            else if ( programStep.ProgramStepType == ProgramStepType.NonDetBoolStep
                || programStep.ProgramStepType == ProgramStepType.NonDetIntStep)
            {
                // Do nothing special
            }

            // Add thread link
            IProgramStep previousMachineStep = this.MachineIdToLastStep[programStep.SrcId];
            if ( previousMachineStep.OpType == AsyncOperationType.Create && previousMachineStep.TargetId == programStep.SrcId)
            {
                // Make this the created child
                SetCreatedRelation(previousMachineStep, programStep);
            }
            else
            {
                SetMachineThreadRelation(previousMachineStep, programStep);
            }

            this.MachineIdToLastStep[programStep.SrcId] = programStep;

            programStep.TotalOrderingIndex = this.OrderedSteps.Count;
            this.OrderedSteps.Add(programStep);

            // TODO: Remove fake use for stepIndex
            stepIndex++;
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
#if false
        private static void SetInboxOrderingRelation(IProgramStep parent, IProgramStep child)
        {
            parent.NextInboxOrderingStep = child;
            child.PrevInboxOrderingStep = parent;
        }
#endif

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
    }
}
