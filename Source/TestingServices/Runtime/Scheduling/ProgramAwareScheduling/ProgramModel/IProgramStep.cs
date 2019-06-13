// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    internal enum ProgramStepType
    {
        SpecialProgramStepType,

        SchedulableStep,
        NonDetBoolStep,
        NonDetIntStep
    }

    internal interface IProgramStep
    {
        ProgramStepType ProgramStepType { get; }

        // Main info
        AsyncOperationType OpType { get; }

        ulong SrcId { get; }

        ulong TargetId { get; }

        EventInfo EventInfo { get; }

        // Non-det step choices
        bool? BooleanChoice { get; }

        int? IntChoice { get; }

        // // Methods to create steps
        // IProgramStep CreateSchedulableStep(IAsyncOperation op);

        // IProgramStep CreateNonDetBoolChoice(bool boolChoice);

        // IProgramStep CreateNonDetIntChoice(int intChoice);

        // Children
        IProgramStep NextMachineStep { get; set; }

        IProgramStep CreatedStep { get; set; }

        // IProgramStep NextInboxOrderingStep { get; set; } // TODO: Is this needed?

        // Parents
        IProgramStep PrevMachineStep { get; set; }

        IProgramStep CreatorParent { get; set; }

        // IProgramStep PrevInboxOrderingStep { get; set; } // TODO: Is this needed?

        // Step signature
        IProgramStepSignature Signature { get; set; }

        int TotalOrderingIndex { get; set; }
    }
}
