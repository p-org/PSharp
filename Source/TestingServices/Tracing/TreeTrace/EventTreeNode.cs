using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class EventTreeNode
    {
        #region Fields
        //internal class EventTreeNodeNonDetChoice
        //{
        //    int? integerChoice;
        //    bool? booleanChoice;

        //    public EventTreeNodeNonDetChoice(bool choice)
        //    {
        //        this.booleanChoice = choice;
        //        this.integerChoice = null;
        //    }

        //    public EventTreeNodeNonDetChoice(int choice)
        //    {
        //        this.booleanChoice = null;
        //        this.integerChoice = choice;
        //    }
        //}
        // This tree is binary. Often it many have only one child.
        internal EventTreeNode directChild;
        internal EventTreeNode createdChild;

        internal EventTreeNode parent;

        internal OperationType opType;
        internal ulong srcMachineId;
        internal ulong targetMachineId;
        internal ulong otherId; // MatchingSendIndex etc.

        //List<int> integerChoices;List<bool> booleanChoices;
        //List<EventTreeNodeNonDetChoice> nonDetChoices;
        List<bool> nonDetBooleanChoices;
        List<int> nonDetIntegerChoices;
        // An explored flag which you don't have to reset between iterations. 
        // ( explored <- lastExploredIter == thisIt )
        internal int totalOrderingIndex;
        
        #endregion

        public EventTreeNode(OperationType opType, ulong srcMachineId, ulong targetMachineId, ulong otherId)
        {
            this.opType = opType;
            this.srcMachineId = srcMachineId;
            this.targetMachineId = targetMachineId;
            this.otherId = otherId;

            totalOrderingIndex = -1;
        }


        internal void addIntegerChoice(int choice)
        {
            //if (nonDetChoices == null)
            //{
            //    nonDetChoices = new List<EventTreeNodeNonDetChoice>();
            //}
            //nonDetChoices.Add(new EventTreeNodeNonDetChoice(choice));
            if (nonDetIntegerChoices == null)
            {
                nonDetIntegerChoices = new List<int>();
            }
            nonDetIntegerChoices.Add(choice);
        }

        internal void setChildren(EventTreeNode nextNode, EventTreeNode createdNode)
        {
            if (nextNode != null) { nextNode.parent = this; }
            if (createdNode != null) { createdNode.parent = this; }

            this.directChild = nextNode;
            this.createdChild = createdNode;
        }

        internal void addBooleanChoice(bool choice)
        {
            //if (nonDetChoices == null)
            //{
            //    nonDetChoices = new List<EventTreeNodeNonDetChoice>();
            //}
            //nonDetChoices.Add(new EventTreeNodeNonDetChoice(choice));
            if (nonDetIntegerChoices == null)
            {
                nonDetBooleanChoices= new List<bool>();
            }
            nonDetBooleanChoices.Add(choice);
        }

        internal bool getBoolean(int booleanChoiceIndex, out bool nextBool)
        {
            if (nonDetBooleanChoices!=null && booleanChoiceIndex < nonDetBooleanChoices.Count )
            {
                nextBool = nonDetBooleanChoices[booleanChoiceIndex];
                return true;
            }
            else
            {
                nextBool = false;
                return false;
            }
        }
        internal bool getInteger(int integerChoiceIndex, out int nextInt)
        {
            if (nonDetIntegerChoices != null && integerChoiceIndex < nonDetIntegerChoices.Count)
            {
                nextInt= nonDetIntegerChoices[integerChoiceIndex];
                return true;
            }
            else
            {
                nextInt = 0;
                return false;
            }
        }

        internal EventTreeNode getChildEvent()
        {
            return directChild;
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
        /// <summary>
        /// Prints a description
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"({srcMachineId}, {opType})";
        }

        internal string serialize()
        {
            StringBuilder sb = new StringBuilder();
            int ccIndex = (createdChild == null || createdChild.totalOrderingIndex == -1) ? -1 : createdChild.totalOrderingIndex;
            int dcIndex = (directChild== null || directChild.totalOrderingIndex == -1) ? -1 : directChild.totalOrderingIndex;
            sb.Append($"{totalOrderingIndex}:{srcMachineId}:{targetMachineId}:{otherId}:{dcIndex}:{ccIndex}");

            sb.Append(":");
            if (nonDetIntegerChoices != null)
            {
                foreach (int i in nonDetIntegerChoices)
                {
                    sb.Append(i + ",");
                }
            }

            sb.Append(":");
            if (nonDetBooleanChoices != null) { 
                foreach (bool b in nonDetBooleanChoices)
                {
                    sb.Append(b?"t":"f");
                }
            }

            return sb.ToString();
        }
        
    }

}
