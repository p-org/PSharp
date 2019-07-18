// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware
{
    internal class ProgramModelReplayerStrategy : ISchedulingStrategy
    {
        private readonly int MaxScheduledSteps;
        private readonly bool TraceIsFair;
        private readonly ISchedulingStrategy SuffixStrategy;
        private readonly ProgramModel ProgramModel;

        private IProgramStep CurrentStep => this.ProgramModel.OrderedSteps[this.CurrentStepIndex];

        private bool ReplayingProgramModel => this.CurrentStepIndex < this.ProgramModel.OrderedSteps.Count;

        // Replay variables
        private int CurrentStepIndex;

        internal ProgramModelReplayerStrategy(ProgramModel programModel, bool traceIsFair, ISchedulingStrategy suffixStrategy, int maxSteps)
        {
            // TODO: Different kinds of input
            this.MaxScheduledSteps = maxSteps;
            this.ProgramModel = programModel;
            this.TraceIsFair = traceIsFair;
            this.SuffixStrategy = suffixStrategy;

            this.ResetReplayVariables();
        }

        private void ResetReplayVariables()
        {
            this.CurrentStepIndex = 0;
        }

        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new NotImplementedException("What even. Let me replay, ok?");
        }

        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException("What even. Let me replay, ok?");
        }

        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException("What even. Let me replay, ok?");
        }

        public string GetDescription()
        {
            return "Replays the program represented by the program model ( A list of IProgramSteps )";
        }

        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            this.CurrentStepIndex++; // Our 0th step is arbitrary in a program model anyway

            if (this.ReplayingProgramModel)
            {
                if (this.CurrentStep.ProgramStepType != ProgramStepType.SchedulableStep)
                {
                    next = null;
                    return false;
                }

                ulong nextSrcId = this.CurrentStep.SrcId;

                next = ops.First(x => x.SourceId == nextSrcId);

                return next != null;
            }
            else
            {
                if (this.SuffixStrategy != null)
                {
                    return this.SuffixStrategy.GetNext(out next, ops, current);
                }
                else
                {
                    next = null;
                    return false;
                }
            }
        }

        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            this.CurrentStepIndex++;
            if (this.ReplayingProgramModel)
            {
                if (this.CurrentStep.ProgramStepType == ProgramStepType.NonDetBoolStep && this.CurrentStep.BooleanChoice != null)
                {
                    next = (bool)this.CurrentStep.BooleanChoice;
                    return true;
                }
                else
                {
                    next = false;
                    return false;
                }
            }
            else
            {
                return this.SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
            }
        }

        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            this.CurrentStepIndex++;
            if (this.ReplayingProgramModel)
            {
                if (this.CurrentStep.ProgramStepType == ProgramStepType.NonDetIntStep && this.CurrentStep.IntChoice != null)
                {
                    next = (int)this.CurrentStep.IntChoice;
                    return true;
                }
                else
                {
                    next = 0;
                    return false;
                }
            }
            else
            {
                return this.SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
            }
        }

        public int GetScheduledSteps()
        {
            // -1 because step 0 is some arbitrary starting point ( iirc )
            return this.CurrentStepIndex - 1 + (this.SuffixStrategy?.GetScheduledSteps() ?? 0);
        }

        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps == 0)
            {
                return false;
            }

            return this.GetScheduledSteps() >= this.MaxScheduledSteps;
        }

        public bool IsFair()
        {
            return this.TraceIsFair;
        }

        public bool PrepareForNextIteration()
        {
            // TODO: Do we want to run more than once?
            return false; // Why even
        }

        public void Reset()
        {
            this.CurrentStepIndex = 0;
        }
    }
}
