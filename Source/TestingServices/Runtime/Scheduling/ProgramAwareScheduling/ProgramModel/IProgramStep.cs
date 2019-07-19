// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    /// <summary>
    /// Tells us what sort of step an IProgramStep represents.
    /// </summary>
    public enum ProgramStepType
    {
        /// <summary>
        /// Reserved for special steps like the start (or a skip maybe?)
        /// </summary>
        SpecialProgramStepType,

        /// <summary>
        /// Your typical ISchedulable step. Those decided by GetNext
        /// </summary>
        SchedulableStep,

        /// <summary>
        /// A non-deterministic boolean choice. Those decided by GetNextBooleanChoice
        /// </summary>
        NonDetBoolStep,

        /// <summary>
        /// A non-deterministic boolean choice. Those decided by GetNextIntegerChoice
        /// </summary>
        NonDetIntStep
    }

    /// <summary>
    /// A (hopefully sufficient) representation of a step in a program.
    /// Meant to be used as vertices in the partial order that represents the program.
    /// With the Next*Step meant to represent the edges.
    /// </summary>
    public interface IProgramStep
    {
        /// <summary>
        /// Tells what type this step is
        /// </summary>
        ProgramStepType ProgramStepType { get; }

        // Main info

        /// <summary>
        /// If SchedulableStep, tells what kind of operationo was performed
        /// </summary>
        AsyncOperationType OpType { get; }

        /// <summary>
        /// The MachineId.Value of the machine performing the step
        /// </summary>
        ulong SrcId { get; }

        /// <summary>
        /// For Send/Create the MachineId.Value of the machine receiving/created.
        /// </summary>
        ulong TargetId { get; }

        /// <summary>
        /// For Send/Receive/Create(?) the EventInfo of the event being sent.
        /// </summary>
        ProgramStepEventInfo EventInfo { get; }

        // Non-det step choices

        /// <summary>
        /// For a non-deterministic boolean choice, the choice taken
        /// </summary>
        bool? BooleanChoice { get; }

        /// <summary>
        /// For a non-deterministic integer choice, the integer chosen
        /// </summary>
        int? IntChoice { get; }

        // // Methods to create steps
        // IProgramStep CreateSchedulableStep(IAsyncOperation op);

        // IProgramStep CreateNonDetBoolChoice(bool boolChoice);

        // IProgramStep CreateNonDetIntChoice(int intChoice);

        // Children

        /// <summary>
        /// The next step performed by the handler.
        /// If desired, Successive handlers are also connected by this step.
        /// </summary>
        IProgramStep NextMachineStep { get; set; }

        /// <summary>
        /// For Send/Create, The corresponding Receive/Start step.
        /// </summary>
        IProgramStep CreatedStep { get; set; }

        /// <summary>
        /// For send steps, the Step which enqueues the message right after the message this step enqueued ( in the same inbox )
        /// </summary>
        IProgramStep NextEnqueuedStep { get; set; } // TODO: Is this needed?

        /// <summary>
        /// For a step which invoked a monitor, the next (in the totally-ordered schedule) step which invoked the same monitor.
        /// </summary>
        IProgramStep NextMonitorStep { get; set; }

        // Parents

        /// <summary>
        /// The reverse of <see cref="NextMachineStep"/>
        /// </summary>
        IProgramStep PrevMachineStep { get; set; }

        /// <summary>
        /// The reverse of <see cref="CreatedStep"/>
        /// </summary>
        IProgramStep CreatorParent { get; set; }

        /// <summary>
        /// The reverse of <see cref="NextEnqueuedStep"/>
        /// </summary>
        IProgramStep PrevEnqueuedStep { get; set; } // TODO: Is this needed?

        /// <summary>
        /// The reverse of <see cref="NextMonitorStep"/>
        /// </summary>
        IProgramStep PrevMonitorStep { get; set; }

        /// <summary>
        /// A Signature which can be compared to other Signatures.
        /// Used to identify corresponding steps across runs.
        /// </summary>
        IProgramStepSignature Signature { get; set; }

        /// <summary>
        /// The step-index in the totally-ordered actual execution of the program.
        /// </summary>
        int TotalOrderingIndex { get; set; }
    }
}
