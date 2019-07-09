// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

// #define USEEDGETYPETUPLE
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
#if USEEDGETYPETUPLE
// using EdgeType = System.Tuple<int, Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel.IProgramStepSignature, int>;
// using EdgeType = System.Tuple<int, ulong, int>;

using EdgeType = System.Tuple<int, Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel.IProgramStepSignature>;
using EdgeType = System.Tuple<int, ulong>;
#endif
// (Aspires to be) An efficient way of storing and counting d-tuples. The root node is a 0-tuple,
// The node reached by traversing (from the root ) an edge corresponding to StepSignature s corresponds to the 1-tuple <s>
// Similarly, the node reached by traversing edges s1,s2,...,sd is the d-tuple <s1,s2,...,sd>

// Since the nodes are irrelevant, We simply use ints as nodes.
// For each depth, we have a counter. Any new node added at the depth will be assigned the value:
//      The first 4-bytes represent the depth of the node and the remaining bytes with the counter value.
// This also allows us to efficiently answer how many d-tuples there are - The value of the counter for the depth d.
// Note that 0 is reserved for not-found. AutoInc starts from 1.

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics
{
    internal class DTupleTree
    {
        public const int RootIdx = 0;

        public int MaxDToCount { get; }

        private readonly int[] AutoInc;
        private readonly Dictionary<EdgeKey, int> Edges;
        private readonly EdgeKey QueryEdge;

        internal DTupleTree(int maxDToCount)
        {
            this.MaxDToCount = maxDToCount;
            this.AutoInc = new int[this.MaxDToCount + 1];

            this.Edges = new Dictionary<EdgeKey, int>(1000003);

            this.QueryEdge = new EdgeKey(0, null);
        }

        public int AddOrUpdateChild(int parentNodeIdx, IProgramStepSignature stepSig)
        {
            // We can do this because the Comparer only considers first two
            int childIdx = this.GetChild(parentNodeIdx, stepSig);
            if (childIdx == 0)
            {
                int nodeDepth = (parentNodeIdx >> 28) + 1;
                childIdx = (nodeDepth << 28) + (++this.AutoInc[nodeDepth]);
                EdgeKey edgeKey = new EdgeKey(parentNodeIdx, stepSig);
                this.AddEdge(edgeKey, childIdx);
                // childIdx = this.GenNextNodeValue(GetNodeDepth(parentNodeIdx) + 1);
                // this.Edges.Add(new EdgeKey(parentNodeIdx, (stepSig as StepSignatures.TreeHashStepSignature).Hash), childIdx);
                // this.QueryEdge.Parent = parentNodeIdx;
                // this.QueryEdge.EdgeHash = (stepSig as StepSignatures.TreeHashStepSignature).Hash;
                // this.Edges.Add(this.QueryEdge, childIdx);
            }

            return childIdx;
        }

        private EdgeKey MakeEdgeKey(int parentNodeIdx, IProgramStepSignature stepSig)
        {
            return new EdgeKey(parentNodeIdx, stepSig);
        }

        private void AddEdge(EdgeKey e, int childIdx)
        {
            this.Edges.Add(e, childIdx);
        }

        public static int GetNodeDepth(int nodeIdx)
        {
            return nodeIdx >> 28;
        }

        private int GenNextNodeValue(int depth)
        {
            return (depth << 28) + (++this.AutoInc[depth]);
        }

        private int GetChild(int parentNodeIdx, IProgramStepSignature stepSig)
        {
            int parentDepth = GetNodeDepth(parentNodeIdx);
            this.QueryEdge.Parent = parentNodeIdx;
            this.QueryEdge.EdgeHash = stepSig;

            if (!this.Edges.TryGetValue(this.QueryEdge, out int childIdx))
            {
                childIdx = 0;
            }

            return childIdx;
        }

        public ulong GetDTupleCount(int d)
        {
            return (ulong)this.AutoInc[d];
        }

        internal class EdgeKey
        {
            internal int Parent;
            internal IProgramStepSignature EdgeHash;

            internal EdgeKey(int p, IProgramStepSignature eHash)
            {
                this.Parent = p;
                this.EdgeHash = eHash;
            }

            public override bool Equals(object obj)
            {
                EdgeKey other = obj as EdgeKey;
                if (other != null)
                {
                    return other.Parent == this.Parent && other.EdgeHash.Equals(this.EdgeHash);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return this.Parent + this.EdgeHash.GetHashCode();
            }
        }
    }
}
