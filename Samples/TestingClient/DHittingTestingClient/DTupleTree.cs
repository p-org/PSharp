using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;

namespace DHittingTestingClient
{
    public class DTupleTree
    {
        public const int RootIdx = 0;

        public int MaxDToCount { get; }

        private readonly int[] AutoInc;
        internal /*private*/ readonly Dictionary<EdgeKey, int> Edges;
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
