// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Manages the collection of ProjectionNodes mapping to offsets in the rewritten C# file.
    /// </summary>
    public class ProjectionTree
    {
        private ProjectionNode[] orderedCSharpProjectionNodes;

        /// <summary>
        /// Returns a read-only list of <see cref="ProjectionNode"/>s ordered by offset in the rewritten C# file,
        /// which is the same ordering as by offset in the original P# file.
        /// </summary>
        internal IReadOnlyList<ProjectionNode> OrderedCSharpProjectionNodes => this.orderedCSharpProjectionNodes;

        /// <summary>
        /// The rewritten C# text.
        /// </summary>
        public string RewrittenCSharpText { get; private set; }

        internal ProjectionTree(IPSharpProgram program) => this.Root = new ProjectionNode(program, this);

        /// <summary>
        /// The root of the <see cref="ProjectionNode"/> tree.
        /// </summary>
        internal ProjectionNode Root;

        internal void AddRootChild(ProjectionNode child) => this.Root.AddChild(child);

        /// <summary>
        /// Called when the first rewrite (of structures) is complete.
        /// </summary>
        internal void OnInitialRewriteComplete()
        {
            this.orderedCSharpProjectionNodes = this.CreateCSharpFlatList().ToArray();
            this.AssertSortedByPSharpOffset();
        }

        /// <summary>
        /// Called when the second rewrite (of code terms within structures) is complete.
        /// </summary>
        internal void OnFinalRewriteComplete()
        {
            foreach (var info in this.orderedCSharpProjectionNodes)
            {
                info.SetCodeTermOriginalPositions();
            }
        }

        /// <summary>
        /// Cumulatively adjust all offsets from the root down to all children after the initial
        /// structure-rewrite pass is completed.
        /// </summary>
        /// <param name="offsetAdjustment">Adjustment to add to the offset</param>
        /// <param name="rewrittenCSharpText">The first-pass final form of the rewritten C# buffer</param>
        internal void FinalizeInitialOffsets(int offsetAdjustment, string rewrittenCSharpText)
        {
            this.RewrittenCSharpText = rewrittenCSharpText;
            foreach (var child in this.Root.PSharpChildren)
            {
                child.FinalizeInitialOffsets(offsetAdjustment);
            }
        }

        /// <summary>
        /// When rewriting has been done on the secondary type-rewrite passes, the rewritten C# text must be updated.
        /// </summary>
        /// <param name="rewrittenCSharpText">The first-pass final form of the rewritten C# buffer</param>
        internal void UpdateRewrittenCSharpText(string rewrittenCSharpText)
        {
            this.RewrittenCSharpText = rewrittenCSharpText;
        }

        /// <summary>
        /// Return a flattened (Depth-First) list of <see cref="ProjectionNode"/>s from the tree starting
        /// at the root.
        /// </summary>
        private IEnumerable<ProjectionNode> CreateCSharpFlatList()
        {
            // Return DFS
            foreach (var child in this.Root.CSharpChildren.Select(child => child.GetFlatList()).SelectMany(list => list))
            {
                yield return child;
            }
        }

        /// <summary>
        /// Asserts that the batch start positions are in ascending order.
        /// </summary>
        [Conditional("DEBUG")]
        private void AssertSortedByPSharpOffset()
        {
#if false // TODO ProjectionBuffer: Not yet ready until all the rewriters have their ProjectionNode headers etc. set up.
            for (var ii = 1; ii < this.orderedCSharpProjectionNodes.Length; ++ii)
            {
                Debug.Assert(this.orderedCSharpProjectionNodes[ii - 1].RewrittenCumulativeOffset < this.orderedCSharpProjectionNodes[ii].RewrittenCumulativeOffset,
                             $"{nameof(ProjectionNode.RewrittenCumulativeOffset)}s are out of sequence");
            }
#else
            // avoid a CA suppression
            if (this.orderedCSharpProjectionNodes.Length == 0)
            {
                return;
            }
#endif
        }
    }
}
