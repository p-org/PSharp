// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        internal readonly AsyncOperationType OpType;
        internal readonly ulong SrcId;       // TODO: Should this be Machine instead? For more info
        internal readonly ulong TargetId;    // TODO: Should this be Machine instead? For more info
        internal ProgramStepEventInfo EventInfo;

        internal IProgramStepSignature Signature;

        private readonly bool? BooleanChoice;
        private readonly int? IntChoice;

        // No need to complicate the edge types
        // Children
        private IProgramStep NextMachineStep;
        private IProgramStep CreatedStep;
        private IProgramStep NextEnqeuedStep; // Only set for receive steps
        private Dictionary<Type, IProgramStep> NextMonitorSteps;

        private IProgramStep CreatorStep;
        private IProgramStep PrevMachineStep;
        private IProgramStep PrevEnqueuedStep; // Only set for receive steps
        private Dictionary<Type, IProgramStep> PrevMonitorSteps;

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

        Dictionary<Type, IProgramStep> IProgramStep.NextMonitorSteps { get => this.NextMonitorSteps; set => this.NextMonitorSteps = value as Dictionary<Type, IProgramStep>; }

        IProgramStep IProgramStep.PrevMachineStep { get => this.PrevMachineStep; set => this.PrevMachineStep = value as ProgramStep; }

        IProgramStep IProgramStep.CreatorParent { get => this.CreatorStep; set => this.CreatorStep = value as ProgramStep; }

        IProgramStep IProgramStep.PrevEnqueuedStep { get => this.PrevEnqueuedStep; set => this.PrevEnqueuedStep = value as ProgramStep; }

        Dictionary<Type, IProgramStep> IProgramStep.PrevMonitorSteps { get => this.PrevMonitorSteps; set => this.PrevMonitorSteps = value as Dictionary<Type, IProgramStep>; }

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
        private ProgramStep(ulong srcId, bool? boolChoice, int? intChoice)
        {
            this.ProgramStepType = ProgramStepType.SpecialProgramStepType;
            // this.Operation = null;
            this.SrcId = srcId;
            this.BooleanChoice = boolChoice;
            this.IntChoice = intChoice;
        }

        internal static ProgramStep CreateSpecialProgramStep()
        {
            return new ProgramStep(0, null, null);
        }

        internal static ProgramStep CreateSpecialProgramStep(ulong srcId, bool? boolChoice, int? intChoice )
        {
            ProgramStep step = new ProgramStep(srcId, boolChoice, intChoice);
            return step;
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

        /// <summary>
        /// Creates a copy of this step, but with no edges set.
        /// </summary>
        /// <returns>A copy of this step</returns>
        public IProgramStep Clone()
        {
            ProgramStep newStep = null;
            switch (this.ProgramStepType)
            {
                case ProgramStepType.SchedulableStep:
                    newStep = new ProgramStep(this.OpType, this.SrcId, this.TargetId, this.EventInfo);
                    break;
                case ProgramStepType.NonDetBoolStep:
                    newStep = new ProgramStep(this.SrcId, (bool)this.BooleanChoice);
                    break;
                case ProgramStepType.NonDetIntStep:
                    newStep = new ProgramStep(this.SrcId, (int)this.IntChoice);
                    break;
                case ProgramStepType.SpecialProgramStepType:
                    newStep = CreateSpecialProgramStep(this.SrcId, this.BooleanChoice, this.IntChoice);
                    break;
            }

            // Fields that may be more complicated
            newStep.Signature = null;
            newStep.TotalOrderingIndex = 0;

            return newStep;
        }
    }
}
