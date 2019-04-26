using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class EventTree
    {
        internal EventTreeNode root;
        internal List<EventTreeNode> totalOrdering;
        internal List<int> withHeldSendIndices;

        internal int criticalTransitionStep;
        internal int bugTriggeringStep;

        public static char[] fieldSeparator = new char[] { ':' };
        public static char[] valueSeparator = new char[] { ',' };

        // Caches

        public EventTree()
        {
            root = null;
            totalOrdering = new List<EventTreeNode>();
            withHeldSendIndices = new List<int>();
            allLoopEventIndices = new HashSet<int>();
            reproducedBug = false;

            criticalTransitionStep = -1;
            bugTriggeringStep = -1;
        }

        #region Tree construction

        private EventTreeNode activeNode;
        private bool reproducedBug;
        private ScheduleTrace actualTrace;
        internal HashSet<int> allLoopEventIndices;

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

        internal int CountSteps()
        {
            int stepCount = 0;
            foreach( EventTreeNode etn in totalOrdering)
            {
                stepCount +=
                    1 +
                    (etn.nonDetBooleanChoices?.Count ?? 0 )+
                    (etn.nonDetIntegerChoices?.Count ?? 0);
            }
            return stepCount;
        }

        internal static EventTree FromTrace(string[] scheduleDump)
        {
            // We just need the withheld-indices from the meta
            EventTree traceTree = new EventTree();
            
            Dictionary<int, Tuple<int, int>> children = new Dictionary<int, Tuple<int, int>>();
            // Parse meta and create total ordering.
            foreach (string line in scheduleDump)
            {
                if (line.StartsWith("--"))
                {
                    // Is meta
                    if (line.StartsWith("--tree-withheldindices"))
                    {
                        string indices = line.Split(fieldSeparator, 2)[1];
                        foreach(string wi in indices.Split(valueSeparator)) {
                            int val = 0;
                            if (wi.Length > 0 && Int32.TryParse(wi, out val))
                            {
                                traceTree.withHeldSendIndices.Add(val);
                            }
                        }

                    }
                }
                else
                {
                    // Is a step

                    int createdChild = 0;
                    int directChild = 0;
                    EventTreeNode etn = EventTreeNode.deserialize(line, out directChild, out createdChild);
                    children.Add(etn.totalOrderingIndex, new Tuple<int, int>(directChild, createdChild));
                    ConsistencyAssert( (etn.totalOrderingIndex==traceTree.totalOrdering.Count), 
                        $"Unexpected stepIndex={etn.totalOrderingIndex} in trace. Expected stepIndex={traceTree.totalOrdering.Count}");
                    traceTree.totalOrdering.Add(etn);
                }
            }
            // Now create the tree;
            traceTree.root = traceTree.totalOrdering[0];
            foreach (EventTreeNode etn in traceTree.totalOrdering)
            {
                etn.setChildren(
                    (children[etn.totalOrderingIndex].Item1==-1)? null : traceTree.totalOrdering[children[etn.totalOrderingIndex].Item1],
                    (children[etn.totalOrderingIndex].Item2==-1)? null : traceTree.totalOrdering[children[etn.totalOrderingIndex].Item2]
                );
            }

            return traceTree;
            
        }

        internal bool checkWithHeld(EventTreeNode etn)
        {
            return withHeldSendIndices.Contains(etn.totalOrderingIndex);
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

        internal bool reproducesBug()
        {
            return reproducedBug;
        }

        private static void ConsistencyAssert(bool conditionResult, string message)
        {
            if (!conditionResult)
            {
                throw new ArgumentException(message);
            }
        }

        internal void setResult(bool bugFound, ScheduleTrace scheduleTrace, int bugTriggerStep)
        {
            this.reproducedBug = bugFound;
            this.actualTrace = scheduleTrace;
            this.bugTriggeringStep = bugTriggerStep;
        }

        internal void setCriticalTransitionStep(int ctStep)
        {
            criticalTransitionStep = ctStep;
        }

        internal ScheduleTrace getActualTrace()
        {
            return this.actualTrace;
        }

        internal string serialize()
        {
            StringBuilder s = new StringBuilder();
            s.Append("--tree-withheldindices:" );
            foreach(int wi in withHeldSendIndices)
            {
                s.Append(wi+",");
            }
            s.Append(Environment.NewLine);

            foreach (EventTreeNode etn in totalOrdering)
            {
                s.Append(etn.serialize()).Append(Environment.NewLine); ;
            }
            return s.ToString();
        }
        #endregion

    }
}