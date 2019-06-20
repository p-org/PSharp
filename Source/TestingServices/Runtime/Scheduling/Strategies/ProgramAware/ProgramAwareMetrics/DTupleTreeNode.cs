// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
    /// <summary>
    /// The number of nodes at depth d ( root being depth 0 ) gives you the number of d-tuples hit.
    /// </summary>
    internal class DTupleTreeNode
    {
        internal IProgramStepSignature StepSig;
        internal Dictionary<IProgramStepSignature, DTupleTreeNode> Children;
        internal int LastInsertedIteration;

        internal int Depth;

        internal DTupleTreeNode(IProgramStepSignature step)
        {
            this.StepSig = step;
            this.Children = new Dictionary<IProgramStepSignature, DTupleTreeNode>();
            this.LastInsertedIteration = 0;

            this.Depth = 0; // Will be set in an AddOrUpdateChild call
        }

        public bool Equals(DTupleTreeNode other)
        {
            return this.StepSig == other.StepSig;
        }

        internal DTupleTreeNode AddOrUpdateChild(IProgramStepSignature stepSig, int currentIterationNumber)
        {
            if (!this.Children.ContainsKey(stepSig))
            {
                this.Children.Add(stepSig, new DTupleTreeNode(stepSig));
            }

            this.Children[stepSig].LastInsertedIteration = currentIterationNumber;
            this.Children[stepSig].Depth = this.Depth + 1;

            return this.Children[stepSig];
        }

        internal ulong GetDTupleCount(int d)
        {
            if (d == 1)
            {
                return (ulong)this.Children.Count;
            }
            else
            {
                ulong cnt = 0;
                foreach (KeyValuePair<IProgramStepSignature, DTupleTreeNode> kvp in this.Children)
                {
                    cnt += kvp.Value.GetDTupleCount(d - 1);
                }

                return cnt;
            }
        }

        internal static IProgramStepSignature CreateRootNodeSignature()
        {
            return new RootStepSignature();
        }

        internal static void PrintDTupleTree(DTupleTreeNode dtr, int depth)
        {
            Console.WriteLine(new string('\t', depth) + $"*{dtr.StepSig}");
            foreach (KeyValuePair<IProgramStepSignature, DTupleTreeNode> kvp in dtr.Children)
            {
                PrintDTupleTree(kvp.Value, depth + 1);
            }
        }

        internal class RootStepSignature : IProgramStepSignature
        {
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override bool Equals(object other)
            {
                return false;
            }

            public override string ToString()
            {
                return "RootStep";
            }
        }
    }
}
