using System;
using Microsoft.PSharp.LanguageServices.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Offset and other information that will be used to create the VS Language Service
    /// Projection Buffers for the PSharp to CSharp rewritten form.
    /// </summary>
    public class ProjectionNode
    {
        #region fields
        
        /// <summary>
        /// The parent of this <see cref="ProjectionNode"/>.
        /// </summary>
        internal ProjectionNode PSharpParent;

        /// <summary>
        /// The C#-rewritten parent of this <see cref="ProjectionNode"/>. This is different from the P# parent
        /// when, for example, a declaration (e.g. a state action) on a contained (e.g. a state) class must be
        /// rewritten as a method in the containaing (e.g. machine) parent class.
        /// </summary>
        internal ProjectionNode CSharpParent;

        /// <summary>
        /// The list of rewritten terms within this <see cref="ProjectionNode"/>'s code block.
        /// </summary>
        internal List<RewrittenTerm> RewrittenCodeTerms = new List<RewrittenTerm>();

        /// <summary>
        /// The children of this Projection Info in the PSharp hierarchy. The P# *Declaration classes maintain
        /// class-specific child lists; here we maintain this in a standard format.
        /// </summary>
        internal List<ProjectionNode> PSharpChildren = new List<ProjectionNode>();

        /// <summary>
        /// The children of this Projection Info in the CSharp hierarchy, as described in <see cref="CSharpParent"/>.
        /// </summary>
        internal List<ProjectionNode> CSharpChildren = new List<ProjectionNode>();

        /// <summary>
        /// The type of the P# node corresponding to this <see cref="ProjectionNode"/>.
        /// </summary>
        private Type NodeType;

        /// <summary>
        /// The offset in the <see cref="ProjectionNode"/>'s substring of the rewritten C# buffer of the start of the header.
        /// </summary>
        private int rewrittenHeaderOffset = -1;

        /// <summary>
        /// The offset in the <see cref="ProjectionNode"/>'s substring of the rewritten C# buffer of the start of the code chunk.
        /// This is the chunk of code for a block, method, or field, initially taken without rewrite from
        /// the original P# file. For a method block this is the offset of the opening left-bracket;
        /// for a field it is the first character of the chunk of code.
        /// </summary>
        private int rewrittenCodeChunkOffset = -1;

        /// <summary>
        /// The container of the <see cref="ProjectionNode"/> hierarchy.
        /// </summary>
        private ProjectionTree projectionTree;

        #endregion

        #region constructor

        internal ProjectionNode(object node, ProjectionTree projectionTree = null)
        {
            this.NodeType = node.GetType();
            this.projectionTree = projectionTree;
        }

        #endregion

        /// <summary>
        /// The (possibly rewritten) "header" of the element, e.g. "machine m1" => "class m1: Machine".
        /// </summary>
        public RewrittenSpan Header { get; private set; } = new RewrittenSpan();

        /// <summary>
        /// Whether this <see cref="ProjectionNode"/> has a rewritten header.
        /// </summary>
        public bool HasRewrittenHeader => this.rewrittenHeaderOffset >= 0;

        /// <summary>
        /// Set the header information.
        /// </summary>
        /// <param name="originalTokenRange">Range of tokens in the original P# buffer</param>
        /// <param name="rewrittenOffset">Offset from <see cref="ProjectionNode"/> start in the rewritten
        ///     C# buffer</param>
        /// <param name="rewrittenString">Rewritten header string (includes any text that will be skipped
        ///     by <paramref name="rewrittenOffset"/>) </param>
        internal void SetHeaderInfo(TokenRange originalTokenRange, int rewrittenOffset, string rewrittenString)
        {
            this.Header.OriginalStart = originalTokenRange.StartPosition;
            this.Header.OriginalString = originalTokenRange.GetString();
            this.rewrittenHeaderOffset = rewrittenOffset;
            this.Header.GetRewrittenStartFunc = () => this.HasRewrittenHeader ? this.rewrittenHeaderOffset + this.RewrittenCumulativeOffset : -1;

            var rewrittenSubstring = rewrittenString.Substring(rewrittenOffset);  // does not change so use a temp
            this.Header.GetRewrittenStringFunc = () => rewrittenSubstring;
            this.Header.RewrittenLength = rewrittenSubstring.Length;
        }

        /// <summary>
        /// The code chunk (possibly with rewritten terms), if any, for this <see cref="ProjectionNode"/>.
        /// </summary>
        public RewrittenSpan CodeChunk { get; private set; } = new RewrittenSpan();

        /// <summary>
        /// Whether this <see cref="ProjectionNode"/>'s code chunk has rewritten aterms.
        /// </summary>
        public bool HasRewrittenCodeTerms => this.RewrittenCodeTerms.Count > 0;

        /// <summary>
        /// Set the code chunk information.
        /// </summary>
        /// <param name="originalStart">Start position in the original P# buffer</param>
        /// <param name="originalString">The string in the original P# buffer; may be updated later by
        ///     <see cref="RewrittenTerm"/>s.</param>
        /// <param name="rewrittenOffset">Offset from <see cref="ProjectionNode"/> start in the rewritten C# buffer</param>
        internal void SetCodeChunkInfo(int originalStart, string originalString, int rewrittenOffset)
        {
            this.CodeChunk.OriginalStart = originalStart;
            this.CodeChunk.OriginalString = originalString;
            this.rewrittenCodeChunkOffset = rewrittenOffset;
            this.CodeChunk.GetRewrittenStartFunc = () => this.rewrittenCodeChunkOffset + this.RewrittenCumulativeOffset;

            // The code chunk may be adjusted based on <see cref="RewrittenTerm"/>s 
            // so we carry the length and dynamically obtain the <see cref="CodeChunk"/> string.
            this.CodeChunk.GetRewrittenStringFunc = () => 
                this.projectionTree.RewrittenCSharpText.Substring(this.CodeChunk.RewrittenStart, this.CodeChunk.RewrittenLength);
            this.CodeChunk.RewrittenLength = originalString.Length;
        }

        /// <summary>
        /// Increment the code chunk length by the passed amount.
        /// </summary>
        /// <param name="increment"></param>
        internal void IncrementCodeChunkLength(int increment)
        {
            this.CodeChunk.RewrittenLength += increment;
        }

        /// <summary>
        /// Whether this <see cref="ProjectionNode"/> has a code chunk.
        /// </summary>
        public bool HasCode => this.rewrittenCodeChunkOffset >= 0;

        /// <summary>
        /// Offset of this <see cref="ProjectionNode"/> in the parent's <see cref="ProjectionNode"/> in the rewritten C# buffer;
        /// this figure is accumulated from parent to children via <see cref="FinalizeInitialOffsets(int)"/>
        /// after the initial structure-rewrite pass is completed.
        /// </summary>
        public int RewrittenCumulativeOffset { get; private set; }

        /// <summary>
        /// Non-root recursive calls to both update the offset and set the rewritten string reference.
        /// </summary>
        /// <param name="offsetAdjustment"></param>
        internal void FinalizeInitialOffsets(int offsetAdjustment)
        {
            this.RewrittenCumulativeOffset += offsetAdjustment;
            foreach (var child in this.CSharpChildren)
            {
                child.FinalizeInitialOffsets(this.RewrittenCumulativeOffset);
            }
        }

        /// <summary>
        /// Set the initial offset in the parent node for this child node.
        /// </summary>
        /// <param name="offsetInParent">The offset within the immediate parent; adjusted by
        ///     <see cref="FinalizeInitialOffsets(int)"/> after the initial structure-rewrite
        ///     pass is completed.</param>
        internal void SetOffsetInParent(int offsetInParent)
        {
            // This is separate from setting the parent because parentage must be established before
            // children are rewritten, which may be done in an initial rewrite iteration before 
            // iteration that accumulates the text offsets (which includes children).
            this.RewrittenCumulativeOffset = offsetInParent;
        }

        /// <summary>
        /// Adds a child <see cref="ProjectionNode"/> (e.g. a state's within a machine).
        /// </summary>
        /// <param name="child">The <see cref="ProjectionNode"/> of the child</param>
        /// <param name="csharpParent">The <see cref="ProjectionNode"/> of the <see cref="CSharpParent"/> of this child, if different from the P# parent</param>
        internal void AddChild(ProjectionNode child, ProjectionNode csharpParent = null)
        {
            child.projectionTree = this.projectionTree;

            child.PSharpParent = this;
            child.PSharpParent.PSharpChildren.Add(child);

            child.CSharpParent = csharpParent ?? this;
            child.CSharpParent.CSharpChildren.Add(child);
        }

        /// <summary>
        /// Add the passed offset to the <see cref="ProjectionNode"/>'s offset in the rewritten C# buffer.
        /// </summary>
        /// <param name="offset"></param>
        internal void AddOffset(int offset)
        {
            this.RewrittenCumulativeOffset += offset;
        }

        /// <summary>
        /// After all term rewrites are complete, set the original term positions. This delayed update
        /// is necessary because there is no mapping of SyntaxNode to Token.
        /// </summary>
        internal void SetCodeTermOriginalPositions()
        {
            var offset = 0;
            foreach (var term in this.RewrittenCodeTerms)
            {
                term.OriginalStart = term.ProjectionNode.CodeChunk.OriginalStart + term.RewrittenStart
                                     - term.ProjectionNode.CodeChunk.RewrittenStart - offset;
                offset += term.ChangedLength;
            }
        }

        /// <summary>
        /// Return a flattened (Depth-First) list of <see cref="ProjectionNode"/>s from the tree starting
        /// at this <see cref="ProjectionNode"/>.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<ProjectionNode> GetFlatList()
        {
            // Return DFS
            yield return this;
            foreach (var child in this.CSharpChildren.Select(child => child.GetFlatList()).SelectMany(list => list))
            {
                yield return child;
            }
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public override string ToString()
        {
            const int rewrittenHeaderMax = 25;
            string getTruncatedString(string value) =>value.Length <= rewrittenHeaderMax ? value : value.Substring(0, rewrittenHeaderMax) + " ...";
            var originalHeaderString = this.Header.OriginalString.Length > 0 ? this.Header.OriginalString : "-0-";
            var rewrittenHeaderString = this.HasRewrittenHeader 
                ? $"[{this.rewrittenHeaderOffset}, {this.Header.RewrittenString.Length}] {getTruncatedString(this.Header.RewrittenString)}]" 
                : "-0-";
            var codeString = this.HasCode 
                ? $"[{this.rewrittenCodeChunkOffset}, {this.CodeChunk.RewrittenLength}] {getTruncatedString(this.CodeChunk.RewrittenString)}]"
                : "-0-";
            return  $"{this.NodeType.Name} offset: [{this.RewrittenCumulativeOffset}] origHdr: [{originalHeaderString}] " +
                    $"rewritnHdr: [{rewrittenHeaderString}] code: [{codeString}]";
        }
    }
}
