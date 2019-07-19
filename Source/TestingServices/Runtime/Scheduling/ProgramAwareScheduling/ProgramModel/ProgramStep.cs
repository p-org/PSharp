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
        internal ProgramStepEventInfo EventInfo;

        internal IProgramStepSignature Signature;

        private readonly bool? BooleanChoice;
        private readonly int? IntChoice;

        // Children
        private ProgramStep NextMachineStep;
        private ProgramStep CreatedStep;
        private ProgramStep NextEnqeuedStep; // Only set for receive steps
        private ProgramStep NextMonitorStep;

        private ProgramStep CreatorStep;
        private ProgramStep PrevMachineStep;
        private ProgramStep PrevEnqueuedStep; // Only set for receive steps
        private ProgramStep PrevMonitorStep;

        // Extra info
        internal readonly ProgramStepType ProgramStepType;

        internal int TotalOrderingIndex;
        // internal readonly IAsyncOperation Operation; // Deprecate

        ProgramStepType IProgramStep.ProgramStepType => this.ProgramStepType;

        AsyncOperationType IProgramStep.OpType => this.OpType;

        ulong IProgramStep.SrcId => this.SrcId;

        ulong IProgramStep.TargetId => this.TargetId;

        ProgramStepEventInfo IProgramStep.EventInfo => this.EventInfo;

        bool? IProgramStep.BooleanChoice => this.BooleanChoice;

        int? IProgramStep.IntChoice => this.IntChoice;

        IProgramStep IProgramStep.NextMachineStep { get => this.NextMachineStep; set => this.NextMachineStep = value as ProgramStep; }

        IProgramStep IProgramStep.CreatedStep { get => this.CreatedStep; set => this.CreatedStep = value as ProgramStep; }

        IProgramStep IProgramStep.NextEnqueuedStep { get => this.NextEnqeuedStep; set => this.NextEnqeuedStep = value as ProgramStep; }

        IProgramStep IProgramStep.NextMonitorStep { get => this.NextMonitorStep; set => this.NextMonitorStep = value as ProgramStep; }

        IProgramStep IProgramStep.PrevMachineStep { get => this.PrevMachineStep; set => this.PrevMachineStep = value as ProgramStep; }

        IProgramStep IProgramStep.CreatorParent { get => this.CreatorStep; set => this.CreatorStep = value as ProgramStep; }

        IProgramStep IProgramStep.PrevEnqueuedStep { get => this.PrevEnqueuedStep; set => this.PrevEnqueuedStep = value as ProgramStep; }

        IProgramStep IProgramStep.PrevMonitorStep { get => this.PrevMonitorStep; set => this.PrevMonitorStep = value as ProgramStep; }

        IProgramStepSignature IProgramStep.Signature { get => this.Signature; set => this.Signature = value; }

        int IProgramStep.TotalOrderingIndex { get => this.TotalOrderingIndex; set => this.TotalOrderingIndex = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramStep"/> class.
        /// </summary>
        /// <param name="opType">Operation type</param>
        /// <param name="srcId">SourceId</param>
        /// <param name="targetId">targetId</param>
        /// <param name="programEventInfo">EventSignature in case this is a send, receive or create, start</param>
        internal ProgramStep(AsyncOperationType opType, ulong srcId, ulong targetId, ProgramStepEventInfo programEventInfo)
        {
            this.ProgramStepType = ProgramStepType.SchedulableStep;
            // this.Operation = op;
            this.BooleanChoice = null;
            this.IntChoice = null;

            this.OpType = opType;
            this.SrcId = srcId;
            this.TargetId = targetId;
            this.EventInfo = programEventInfo; // Just incase
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

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (this.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{this.SrcId}:{this.OpType}:{this.TargetId}]";
                case ProgramStepType.NonDetBoolStep:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{this.SrcId}:{this.BooleanChoice}]";
                case ProgramStepType.NonDetIntStep:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{this.SrcId}:{this.IntChoice}]";
                default:
                    return $"[{this.TotalOrderingIndex}:{this.ProgramStepType}:{base.ToString()}]";
            }
        }
    }
}
