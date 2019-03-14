using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class EventTreeNode
    {
        #region Fields
        internal class EventTreeNodeNonDetChoice
        {
            int? integerChoice;
            bool? booleanChoice;

            public EventTreeNodeNonDetChoice(bool choice)
            {
                this.booleanChoice = choice;
                this.integerChoice = null;
            }

            public EventTreeNodeNonDetChoice(int choice)
            {
                this.booleanChoice = null;
                this.integerChoice = choice;
            }
        }
        // This tree is binary. Often it many have only one child.
        internal EventTreeNode directChild;
        internal EventTreeNode createdChild;

        internal EventTreeNode parent;

        internal OperationType opType;
        internal ulong srcMachineId;
        internal ulong targetMachineId;
        internal ulong otherId; // MatchingSendIndex etc.
        
        //List<int> integerChoices;List<bool> booleanChoices;
        List<EventTreeNodeNonDetChoice> nonDetChoices;
        // An explored flag which you don't have to reset between iterations. 
        // ( explored <- lastExploredIter == thisIt )
        private int lastExploredIteration;
        private int deletedIterationIndex;
        internal int totalOrderingIndex;
        
        #endregion

        public EventTreeNode(OperationType opType, ulong srcMachineId, ulong targetMachineId, ulong otherId)
        {
            this.opType = opType;
            this.srcMachineId = srcMachineId;
            this.targetMachineId = targetMachineId;
            this.otherId = otherId;

            totalOrderingIndex = -1;
            lastExploredIteration = -1;
            deletedIterationIndex = -1;
        }


        internal void addIntegerChoice(int choice)
        {
            if (nonDetChoices == null)
            {
                nonDetChoices = new List<EventTreeNodeNonDetChoice>();
            }
            nonDetChoices.Add(new EventTreeNodeNonDetChoice(choice));
        }

        internal void setChildren(EventTreeNode nextNode, EventTreeNode createdNode)
        {
            nextNode.parent = this;
            createdNode.parent = this;

            this.directChild = nextNode;
            this.createdChild = createdNode;
        }

        internal void addBooleanChoice(bool choice)
        {
            if (nonDetChoices == null)
            {
                nonDetChoices = new List<EventTreeNodeNonDetChoice>();
            }
            nonDetChoices.Add(new EventTreeNodeNonDetChoice(choice));
        }
        

        internal bool checkEquality(ISchedulable sch)
        {
            if (sch.NextOperationType == opType && sch.Id == srcMachineId)
            {
                if (opType == OperationType.Receive)
                {
                    return otherId == sch.NextOperationMatchingSendIndex;
                }
                else
                {
                    if (otherId != sch.NextTargetId)
                    {   // TODO: Remove when we're confident
                        throw new Exception("This isnt a necessary check");
                    }
                    return otherId == sch.NextTargetId;
                }
            }
            else
            {
                return false;
            }
        }

        #region exploration deletion bookkeeping
        internal bool CheckAndPropagateDeleted(int currentIterationIndex)
        {
            this.deletedIterationIndex = parent.deletedIterationIndex;
            return (this.deletedIterationIndex >= currentIterationIndex);
        }
        void markExplored(int iterationIdx)
        {
            lastExploredIteration = iterationIdx;
        }
        bool isExplored(int iterationIdx)
        {
            return lastExploredIteration == iterationIdx;
        }

        internal EventTreeNode getChildEvent()
        {
            return directChild;
        }
        #endregion

    }

}
