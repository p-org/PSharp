using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Manages the collection of ProjectionNodes mapping to offsets in the rewritten C# file.
    /// </summary>
    public class ProjectionTree
    {
        #region fields

        private ProjectionNode[] orderedCSharpProjectionNodes;

        #endregion

        #region public API

        /// <summary>
        /// Returns a read-only list of <see cref="ProjectionNode"/>s ordered by offset in the rewritten C# file,
        /// which is the same ordering as by offset in the original P# file.
        /// </summary>
        public IReadOnlyList<ProjectionNode> OrderedCSharpProjectionNodes { get { return this.orderedCSharpProjectionNodes; } }

        /// <summary>
        /// The rewritten C# text.
        /// </summary>
        public string RewrittenCSharpText { get; private set; }

        #endregion

        #region internal API

        internal ProjectionTree(IPSharpProgram program)
        {
            this.Root = new ProjectionNode(program, this);
        }

        /// <summary>
        /// The root of the <see cref="ProjectionNode"/> tree.
        /// </summary>
        internal ProjectionNode Root;

        /// <summary>
        /// Called when the first rewrite (of structures) is complete.
        /// </summary>
        internal void OnInitialRewriteComplete()
        {
            this.orderedCSharpProjectionNodes = this.CreateCSharpFlatList().ToArray();
            AssertSortedByPSharpOffset();
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

        #endregion

        #region private methods

        /// <summary>
        /// Cumulatively adjust all offsets from the root down to all children after the initial
        /// structure-rewrite pass is completed.
        /// </summary>
        /// <param name="offsetAdjustment"></param>
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
        /// <param name="rewrittenCSharpText"></param>
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
            for (var ii = 1; ii < this.orderedCSharpProjectionNodes.Length; ++ii)
            {
                Debug.Assert(this.orderedCSharpProjectionNodes[ii - 1].RewrittenCumulativeOffset < this.orderedCSharpProjectionNodes[ii].RewrittenCumulativeOffset);
            }
        }

        #endregion
    }
}
