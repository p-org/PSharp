// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics.StepSignatures;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies
{
    /// <summary>
    /// Counts d-hitting tuples in the execution
    /// </summary>
    internal /*abstract*/ class BasicProgramModelBasedStrategy : IProgramAwareSchedulingStrategy
    {
        // Some handy constants
        protected const ulong TESTHARNESSMACHINEID = 0;
        protected const ulong TESTHARNESSMACHINEHASH = 199999;

        // TODO: Make this class abstract?
        internal ProgramModel ProgramModel;
        private readonly ISchedulingStrategy ChildStrategy;

        protected virtual bool HashEvents
        {
            get { return false; }
        }

        internal BasicProgramModelBasedStrategy(ISchedulingStrategy childStrategy)
        {
            this.ChildStrategy = childStrategy;
            this.ProgramModel = new ProgramModel();
        }

        public string GetDescription()
        {
            return "Wrapper strategy which constructs a model of the program while it runs according to the specified child-strategy:" + this.ChildStrategy.GetType().Name;
        }

        public bool IsFair()
        {
            return this.ChildStrategy.IsFair();
        }

        public void Reset()
        {
            // TODO
            this.ChildStrategy.Reset();
        }

        public bool PrepareForNextIteration()
        {
            this.ProgramModel = new ProgramModel();
            return this.ChildStrategy.PrepareForNextIteration();
        }

        public int GetScheduledSteps()
        {
            return this.ChildStrategy.GetScheduledSteps();
        }

        public bool HasReachedMaxSchedulingSteps()
        {
            return this.ChildStrategy.HasReachedMaxSchedulingSteps();
        }

        // Scheduling Choice(?)s
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
            => this.ChildStrategy.ForceNext(next, ops, current);

        public void ForceNextBooleanChoice(int maxValue, bool next)
            => this.ChildStrategy.ForceNextBooleanChoice(maxValue, next);

        public void ForceNextIntegerChoice(int maxValue, int next)
            => this.ChildStrategy.ForceNextIntegerChoice(maxValue, next);

        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            // TODO
            return this.ChildStrategy.GetNext(out next, ops, current);
        }

        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            return this.ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            return this.ChildStrategy.GetNextIntegerChoice(maxValue, out next);
        }

        // The program-aware part
        public virtual void RecordCreateMachine(Machine createdMachine, Machine creatorMachine)
        {
            ProgramStep createStep = new ProgramStep(AsyncOperationType.Create, creatorMachine?.Id.Value ?? 0, createdMachine.Id.Value, null);
            this.ProgramModel.RecordStep(createStep, this.GetScheduledSteps()); // TODO: Should i do -1?
        }

        public virtual void RecordReceiveEvent(Machine machine, EventInfo eventInfo)
        {
            ProgramStep receiveStep = new ProgramStep(AsyncOperationType.Receive, machine.Id.Value, machine.Id.Value, eventInfo);
            this.ProgramModel.RecordStep(receiveStep, this.GetScheduledSteps());
        }

#if false
        public void RecordSendEvent(AsyncMachine sender, Machine targetMachine, EventInfo eventInfo)
        {
            if ( sender == null || targetMachine == null || eventInfo == null || this.ProgramModel == null)
            {
                throw new AssertionFailureException("Unexpected null");
            }

            ProgramStep sendStep = new ProgramStep(AsyncOperationType.Send, sender.Id.Value, targetMachine.Id.Value, eventInfo);
            this.ProgramModel.RecordStep(sendStep, this.GetScheduledSteps());
        }
#endif
        public virtual void RecordSendEvent(AsyncMachine sender, MachineId targetMachineId, EventInfo eventInfo, Event e)
        {
            if (this.HashEvents)
            {
                eventInfo.HashedState = this.HashEvent(e);
            }

            ProgramStep sendStep = new ProgramStep(AsyncOperationType.Send, sender?.Id.Value ?? 0, targetMachineId.Value, eventInfo);
            this.ProgramModel.RecordStep(sendStep, this.GetScheduledSteps());
        }

#if USE_DATACONTRACT_SERIALIZER_WHICH_DOESNT_WORK_ANYWAY
        private static Dictionary<Type, DataContractSerializer> Serializers = new Dictionary<Type, DataContractSerializer>();

        private int HashEvent(Event e)
        {
            DataContractSerializer serializer;
            Type eventType = e.GetType();
            if (!Serializers.ContainsKey(eventType))
            {
                serializer = new DataContractSerializer(eventType);
                Serializers.Add(eventType, serializer);
            }
            else
            {
                serializer = Serializers[eventType];
            }

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            serializer.WriteObject(ms, e);
            string str = Encoding.ASCII.GetString(ms.ToArray());
            return str.GetHashCode(); // Requires strings to map to unique hashcode
        }
#else
        private int HashEvent(Event e)
        {
            return ReflectionBasedHasher.HashObject(e);
        }
#endif

        public virtual void RecordNonDetBooleanChoice(bool boolChoice)
        {
            ProgramStep ndBoolStep = new ProgramStep(this.ProgramModel.ActiveStep.SrcId, boolChoice);
            this.ProgramModel.RecordStep(ndBoolStep, this.GetScheduledSteps());
        }

        public virtual void RecordNonDetIntegerChoice(int intChoice)
        {
            ProgramStep ndIntStep = new ProgramStep(this.ProgramModel.ActiveStep.SrcId, intChoice);
            this.ProgramModel.RecordStep(ndIntStep, this.GetScheduledSteps());
        }

        public virtual void RecordMonitorEvent(Type monitorType, AsyncMachine sender, Event e)
        {
            // Do Nothing
            this.ProgramModel.RecordMonitorEvent(monitorType, sender);
        }

        public virtual void NotifySchedulingEnded(bool bugFound)
        {
            // Do nothing
        }

        public virtual string GetProgramTrace()
        {
            return this.ProgramModel.SerializeProgramTrace();
        }

        public virtual string GetReport()
        {
            return null;
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }

        public void RecordStartMachine(Machine machine, EventInfo initialEvent)
        {
            ProgramStep startStep = new ProgramStep(AsyncOperationType.Start, machine.Id.Value, machine.Id.Value, initialEvent);
            this.ProgramModel.RecordStep(startStep, this.GetScheduledSteps());
        }
    }
}
