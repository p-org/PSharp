using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class EventTree
    {
        internal EventTreeNode root;
        internal List<EventTreeNode> totalOrdering;

        // Caches

        public EventTree()
        {
            root = null;
            totalOrdering = new List<EventTreeNode>();
        }

        #region Tree construction

        private EventTreeNode activeNode;
        private ScheduleTrace actualTrace;

        public EventTreeNode getActiveNode()
        {
            return activeNode;
        }
        public void startScheduleChoice(EventTreeNode node)
        {
            ConsistencyAssert(activeNode == null, "There is a different node active");
            ConsistencyAssert(node.totalOrderingIndex==-1, "This node was already added at position" + node.totalOrderingIndex);

            node.totalOrderingIndex = totalOrdering.Count;
            totalOrdering.Add(node);
            activeNode = node;
            
        }

        public void recordIntegerChoice(EventTreeNode node, int choice)
        {
            ConsistencyAssert(node == activeNode, "Argument node does not match active node");
            node.addIntegerChoice(choice);
        }
        public void recordBooleanChoice(EventTreeNode node, bool choice)
        {
            ConsistencyAssert(node == activeNode, "Argument node does not match active node");
            node.addBooleanChoice(choice);
        }

        public void completeScheduleChoice(EventTreeNode node, EventTreeNode nextNode, EventTreeNode createdNode)
        {
            if (node != activeNode ) {
                throw new ArgumentException("Argument node does not match active node");
            }
            node.setChildren(nextNode, createdNode);
            activeNode = null;
        }

        #endregion

        #region convenience static methods 
        public void initializeWithRoot(EventTreeNode root)
        {
            this.root = root;
        }
        internal static EventTreeNode CreateStartNode(ulong creatorMachineId, ulong createdMachineId)
        {
            return new EventTreeNode(OperationType.Start, createdMachineId, createdMachineId, 0); 
        }

        internal static EventTreeNode CreateReceiveNode(ulong senderMachineId, ulong receiverMachineId, ulong sendIndex)
        {
            return new EventTreeNode(OperationType.Receive, receiverMachineId, receiverMachineId, sendIndex);
        }

        internal static EventTreeNode CreateNodeFromISchedulable(ISchedulable sch)
        { 
            ulong targetMachineId = 0;
            switch (sch.NextOperationType)
            {
                //case OperationType.Receive:
                //    otherId = sch.NextOperationMatchingSendIndex;
                //    break;
                case OperationType.Send:
                case OperationType.Create:
                case OperationType.Start:
                case OperationType.Stop:
                    targetMachineId = sch.NextTargetId;
                    break;
                default:
                    throw new Exception("OperationType not yet supported by EventTree: " + sch.NextOperationType);
            }
            return new EventTreeNode(sch.NextOperationType, sch.Id, targetMachineId, 0);
        }

        private static void ConsistencyAssert(bool conditionResult, string message)
        {
            if (!conditionResult)
            {
                throw new ArgumentException(message);
            }
        }

        internal void setActualTrace(ScheduleTrace scheduleTrace)
        {
            this.actualTrace = scheduleTrace;
        }

        internal ScheduleTrace getActualTrace()
        {
            return this.actualTrace;
        }
        #endregion

    }
}