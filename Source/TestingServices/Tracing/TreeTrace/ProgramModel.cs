using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class ProgramModel
    {

        internal EventTree constructedTree;

        // Program model
        private EventTreeNode currentHandler;
        private ulong highestKnownId;
        private int currentIterationIndex;

        internal int totalOrderingIndex; // Index of next step to be executed
        internal ulong actualStepsExecuted; // Count of how many steps we've actually executed ( is this even needed here? Let the strategy do it? )

        private Dictionary<ulong, EventTreeNode> sendIndexToReceiveEvent;
        private Dictionary<ulong, EventTreeNode> machineIdToStartEvent;
        private Dictionary<ulong, EventTreeNode> machineIdToRunningEvent;

        public ProgramModel()
        {
            sendIndexToReceiveEvent = new Dictionary<ulong, EventTreeNode>();
            machineIdToStartEvent = new Dictionary<ulong, EventTreeNode>();
        }


        public void ResetProgramModel(int currentIterationIndex)
        {
            this.currentIterationIndex = currentIterationIndex;

            sendIndexToReceiveEvent.Clear();
            machineIdToStartEvent.Clear();
            currentHandler = null;

            totalOrderingIndex = 0;
            actualStepsExecuted = 0;
        }

        #region updates
        public void initializeWithTestHarnessMachine(ulong testHarnessMachineId)
        {
            // TODO: I guess TestHarnessMachine is always 0 ?
            EventTreeNode root = EventTree.CreateStartNode(0, testHarnessMachineId);
            currentHandler = root;
            highestKnownId = testHarnessMachineId;
        }

        public void recordSchedulingChoiceStart(ISchedulable choice, ulong stepIndex)
        {
            if (currentHandler.opType == OperationType.Send)
            {
                currentHandler.otherId = stepIndex;
            }
            constructedTree.startScheduleChoice(currentHandler);

        }

        public void recordSchedulingChoiceResult(ISchedulable current, Dictionary<ulong, ISchedulable> machineToChoices, ulong endStepIndex)
        {
            if (!currentHandler.checkEquality(current))
            {
                throw new ArgumentException("Current did not match CurrentHandler");
            }

            EventTreeNode createdNode = null;

            if (currentHandler.opType == OperationType.Create)
            {
                // A start operation has to be added to the chains
                //ulong expectedMachineId = machinesCreatedSoFar++;
                ulong expectedMachineId = machineToChoices.Where(x => x.Key > highestKnownId).Select(x => x.Key).Max();
                createdNode = EventTree.CreateStartNode(currentHandler.srcMachineId, currentHandler.targetMachineId);

                highestKnownId = expectedMachineId;
                machineIdToStartEvent.Add(createdNode.targetMachineId, createdNode);

            }
            else if (currentHandler.opType == OperationType.Send)
            {
                createdNode = EventTree.CreateReceiveNode(currentHandler.srcMachineId, currentHandler.targetMachineId, currentHandler.otherId);
                sendIndexToReceiveEvent.Add(createdNode.targetMachineId, createdNode);
            }

            ISchedulable nextStepOfCurrentSchedulable = null;
            if (machineToChoices.TryGetValue(currentHandler.srcMachineId, out nextStepOfCurrentSchedulable))
            {
                if (!IsContinuation(currentHandler, nextStepOfCurrentSchedulable))
                {
                    nextStepOfCurrentSchedulable = null;
                }
            }

            // Mark EventHandler as complete if the next step is not a continuation.
            EventTreeNode nextNode = null;
            if (nextStepOfCurrentSchedulable != null)
            {
                nextNode = EventTree.CreateNodeFromISchedulable(nextStepOfCurrentSchedulable);
            }

            constructedTree.completeScheduleChoice(currentHandler, nextNode, createdNode);

        }

        internal void RecordIntegerChoice(int choice)
        {
            currentHandler.addIntegerChoice(choice);
        }

        internal void RecordBooleanChoice(bool choice)
        {
            currentHandler.addBooleanChoice(choice);
        }
        #endregion

        #region ischedulable matching
        internal bool IsContinuation(EventTreeNode treeNode, ISchedulable sch)
        {
            if (treeNode.srcMachineId != sch.Id)
            {
                return false;
            }
            else
            {
                switch (sch.NextOperationType)
                {
                    case OperationType.Create:
                    case OperationType.Send:
                    case OperationType.Stop:
                        return true;
                    default:
                        return false;

                }
            }
        }

        internal bool getTreeNodeFromISchedulable(ISchedulable sch, out EventTreeNode treeNode)
        {
            bool matched = false;
            treeNode = null;
            switch (sch.NextOperationType)
            {
                case OperationType.Receive:
                    matched = sendIndexToReceiveEvent.TryGetValue(sch.NextOperationMatchingSendIndex, out treeNode);
                    break;

                case OperationType.Start:
                    matched = machineIdToStartEvent.TryGetValue(sch.Id, out treeNode);
                    break;

                case OperationType.Create:
                case OperationType.Stop:
                case OperationType.Send:
                    {
                        EventTreeNode tempNode = null;
                        matched = machineIdToRunningEvent.TryGetValue(sch.Id, out tempNode);

                        if (matched && tempNode != null)
                        {
                            treeNode = tempNode.getChildEvent();
                            matched = (treeNode != null);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Program model does not yet support OperationType " + sch.NextOperationType );
            }
            
            return matched;
        }

        #endregion
    }
}
