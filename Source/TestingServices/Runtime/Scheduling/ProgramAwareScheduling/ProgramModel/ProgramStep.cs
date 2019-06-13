// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    /// <summary>
    /// One step in the program.
    /// Should capture all the information needed for Program-aware scheduling strategies.
    /// </summary>
    public class ProgramStep : IProgramStep
    {
        // Main info
        internal AsyncOperationType OpType;
        internal ulong SrcId;       // TODO: Should this be Machine instead? For more info
        internal ulong TargetId;    // TODO: Should this be Machine instead? For more info
        internal EventInfo EventInfo;

        internal IProgramStepSignature Signature;

        private readonly bool? BooleanChoice;
        private readonly int? IntChoice;

        // Children
        internal ProgramStep NextMachineStep;
        internal ProgramStep CreatedStep;
        // internal ProgramStep NextInboxOrderingStep;

        private ProgramStep CreatorStep;
        private ProgramStep PrevMachineStep;
        // private ProgramStep PrevInboxOrderingStep;

        // Extra info
        internal readonly ProgramStepType ProgramStepType;

        internal int TotalOrderingIndex;
        // internal readonly IAsyncOperation Operation; // Deprecate

        ProgramStepType IProgramStep.ProgramStepType => this.ProgramStepType;

        AsyncOperationType IProgramStep.OpType => this.OpType;

        ulong IProgramStep.SrcId => this.SrcId;

        ulong IProgramStep.TargetId => this.TargetId;

        EventInfo IProgramStep.EventInfo => this.EventInfo;

        bool? IProgramStep.BooleanChoice => this.BooleanChoice;

        int? IProgramStep.IntChoice => this.IntChoice;

        IProgramStep IProgramStep.NextMachineStep { get => this.NextMachineStep; set => this.NextMachineStep = value as ProgramStep; }

        IProgramStep IProgramStep.CreatedStep { get => this.CreatedStep; set => this.CreatedStep = value as ProgramStep; }

        // IProgramStep IProgramStep.NextInboxOrderingStep { get => this.NextInboxOrderingStep; set => this.NextInboxOrderingStep = value as ProgramStep; }

        IProgramStep IProgramStep.PrevMachineStep { get => this.PrevMachineStep; set => this.PrevMachineStep = value as ProgramStep; }

        IProgramStep IProgramStep.CreatorParent { get => this.CreatorStep; set => this.CreatorStep = value as ProgramStep; }

        // IProgramStep IProgramStep.PrevInboxOrderingStep { get => this.PrevInboxOrderingStep; set => this.PrevInboxOrderingStep = value as ProgramStep; }

        IProgramStepSignature IProgramStep.Signature { get => this.Signature; set => this.Signature = value; }

        int IProgramStep.TotalOrderingIndex { get => this.TotalOrderingIndex; set => this.TotalOrderingIndex = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="opType">Operation type</param>
        /// <param name="srcId">SourceId</param>
        /// <param name="targetId">targetId</param>
        /// <param name="eventInfo">EventInfo in case this is a send or receive</param>
        internal ProgramStep(AsyncOperationType opType, ulong srcId, ulong targetId, EventInfo eventInfo)
        {
            this.ProgramStepType = ProgramStepType.SchedulableStep;
            // this.Operation = op;
            this.BooleanChoice = null;
            this.IntChoice = null;

            this.OpType = opType;
            this.SrcId = srcId;
            this.TargetId = targetId;
            this.EventInfo = eventInfo; // Just incase
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="srcId">The Id of the machine taking the choice</param>
        /// <param name="intChoice">The integer chosen non-deterministically</param>
        internal ProgramStep(ulong srcId, int intChoice)
        {
            this.ProgramStepType = ProgramStepType.NonDetIntStep;
            // this.Operation = null;
            this.BooleanChoice = null;
            this.IntChoice = intChoice;

            this.SrcId = srcId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="srcId">The Id of the machine taking the choice</param>
        /// <param name="boolChoice">The boolean chosen non-deterministically</param>
        internal ProgramStep(ulong srcId, bool boolChoice)
        {
            this.ProgramStepType = ProgramStepType.NonDetBoolStep;
            // this.Operation = null;
            this.BooleanChoice = null;
            this.BooleanChoice = boolChoice;

            this.SrcId = srcId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// For those special steps
        /// </summary>
        private ProgramStep()
        {
            this.ProgramStepType = ProgramStepType.SpecialProgramStepType;
            // this.Operation = null;
            this.BooleanChoice = null;
            this.BooleanChoice = null;
        }

        internal static ProgramStep CreateSpecialProgramStep()
        {
            return new ProgramStep();
        }
    }
}
