using System;
using Microsoft.PSharp.LanguageServices.Parsing;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Offset and other information that will be used to create the VS Language Service
    /// Projection Buffers for the PSharp to CSharp rewritten form.
    /// </summary>
    public class ProjectionInfo
    {
        #region fields
        
        /// <summary>
        /// The parent of this <see cref="ProjectionInfo"/>.
        /// </summary>
        internal ProjectionInfo Parent;

        /// <summary>
        /// The list of rewritten terms within this <see cref="ProjectionInfo"/>'s code block.
        /// </summary>
        internal List<RewrittenTerm> RewrittenCodeTerms = new List<RewrittenTerm>();

        /// <summary>
        /// The children of this Projection Info. The P# *Declaration classes maintain class-specific
        /// child lists; here we maintain this in a standard format.
        /// </summary>
        internal List<ProjectionInfo> children = new List<ProjectionInfo>();

        /// <summary>
        /// The type of the P# node corresponding to this <see cref="ProjectionInfo"/>.
        /// </summary>
        private Type NodeType;

        /// <summary>
        /// The offset in the ProjectionInfo's substring of the rewritten C# buffer of the start of the header.
        /// </summary>
        private int rewrittenHeaderOffset = -1;

        /// <summary>
        /// The offset in the ProjectionInfo's substring of the rewritten C# buffer of the start of the code chunk.
        /// This is the chunk of code for a block, method, or field, initially taken without rewrite from
        /// the original P# file. For a method block this is the offset of the opening left-bracket;
        /// for a field it is the first character of the chunk of code.
        /// </summary>
        private int rewrittenCodeChunkOffset = -1;

        /// <summary>
        /// The container of the <see cref="ProjectionInfo"/> hierarchy.
        /// </summary>
        private ProjectionInfos projectionInfos;

        #endregion

        #region constructor

        internal ProjectionInfo(object node)
        {
            this.NodeType = node.GetType();
        }

        #endregion

        /// <summary>
        /// The (possibly rewritten) "header" of the element, e.g. "machine m1" => "class m1: Machine".
        /// </summary>
        public RewrittenSpan Header { get; private set; } = new RewrittenSpan();
        
        /// <summary>
        /// Whether this ProjectionInfo has a rewritten header.
        /// </summary>
        public bool HasRewrittenHeader => this.rewrittenHeaderOffset >= 0;

        /// <summary>
        /// Set the header information.
        /// </summary>
        /// <param name="originalTokenRange">Range of tokens in the original P# buffer</param>
        /// <param name="rewrittenOffset">Offset from <see cref="ProjectionInfo"/> start in the rewritten
        ///     C# buffer</param>
        /// <param name="rewrittenString">Rewritten header string (includes any text that will be skipped
        ///     by <paramref name="rewrittenOffset"/>) </param>
        internal void SetHeaderInfo(TokenRange originalTokenRange, int rewrittenOffset, string rewrittenString)
        {
            this.Header.OriginalStart = originalTokenRange.StartPosition;
            this.Header.OriginalString = originalTokenRange.GetString();
            this.rewrittenHeaderOffset = rewrittenOffset;
            this.Header.GetRewrittenStartFunc = () => this.HasRewrittenHeader ? this.rewrittenHeaderOffset + this.CumulativeOffset : -1;

            var rewrittenSubstring = rewrittenString.Substring(rewrittenOffset);  // does not change so use a temp
            this.Header.GetRewrittenStringFunc = () => rewrittenSubstring;
            this.Header.RewrittenLength = rewrittenSubstring.Length;
        }

        /// <summary>
        /// The code chunk (possibly with rewritten terms), if any, for this <see cref="ProjectionInfo"/>.
        /// </summary>
        public RewrittenSpan CodeChunk { get; private set; } = new RewrittenSpan();

        /// <summary>
        /// Whether this <see cref="ProjectionInfo"/>'s code chunk has rewritten aterms.
        /// </summary>
        public bool HasRewrittenCodeTerms => this.RewrittenCodeTerms.Count > 0;

        /// <summary>
        /// Set the code chunk information.
        /// </summary>
        /// <param name="originalStart">Start position in the original P# buffer</param>
        /// <param name="originalString">The string in the original P# buffer; may be updated later by
        ///     <see cref="RewrittenTerm"/>s.</param>
        /// <param name="rewrittenOffset">Offset from <see cref="ProjectionInfo"/> start in the rewritten C# buffer</param>
        internal void SetCodeChunkInfo(int originalStart, string originalString, int rewrittenOffset)
        {
            this.CodeChunk.OriginalStart = originalStart;
            this.CodeChunk.OriginalString = originalString;
            this.rewrittenCodeChunkOffset = rewrittenOffset;
            this.CodeChunk.GetRewrittenStartFunc = () => this.rewrittenCodeChunkOffset + this.CumulativeOffset;

            // The code chunk may be adjusted based on <see cref="RewrittenTerm"/>s 
            // so we carry the length and dynamically obtain the <see cref="CodeChunk"/> string.
            this.CodeChunk.GetRewrittenStringFunc = () => 
                this.projectionInfos.RewrittenCSharpText.Substring(this.CodeChunk.RewrittenStart, this.CodeChunk.RewrittenLength);
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
        /// Whether this ProjectionInfo has a code chunk.
        /// </summary>
        public bool HasCode => this.rewrittenCodeChunkOffset >= 0;

        /// <summary>
        /// Offset of this ProjectionInfo in the parent's ProjectionInfo in the rewritten C# buffer;
        /// this figure is accumulated from parent to children via <see cref="FinalizeInitialOffsets(int)"/>
        /// after the initial structure-rewrite pass is completed.
        /// </summary>
        public int CumulativeOffset { get; private set; }

        /// <summary>
        /// Non-root recursive calls to both update the offset and set the rewritten string reference.
        /// </summary>
        /// <param name="offsetAdjustment"></param>
        internal void FinalizeInitialOffsets(int offsetAdjustment)
        {
            this.CumulativeOffset += offsetAdjustment;
            foreach (var child in this.children)
            {
                child.FinalizeInitialOffsets(this.CumulativeOffset);
            }
        }

        /// <summary>
        /// Adds a child projectionInfo (e.g. a state's within a machine).
        /// </summary>
        /// <param name="child">The ProjectionInfo of the child</param>
        /// <param name="offsetInParent">The offset within the immediate parent; adjusted by
        ///     <see cref="FinalizeInitialOffsets(int)"/> after the initial structure-rewrite
        ///     pass is completed.</param>
        internal void AddChild(ProjectionInfo child, int offsetInParent)
        {
            child.Parent = this;
            child.projectionInfos = this.projectionInfos;
            child.CumulativeOffset = offsetInParent;
            this.children.Add(child);
        }

        /// <summary>
        /// Add the passed offset to the ProjectionInfo's offset in the rewritten C# buffer.
        /// </summary>
        /// <param name="offset"></param>
        internal void AddOffset(int offset)
        {
            this.CumulativeOffset += offset;
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
                term.OriginalStart = term.ProjectionInfo.CodeChunk.OriginalStart + term.RewrittenStart
                                     - term.ProjectionInfo.CodeChunk.RewrittenStart - offset;
                offset += term.ChangedLength;
            }
        }

        /// <summary>
        /// Return a flattened (Depth-First) list of <see cref="ProjectionInfo"/>s from the tree starting
        /// at this <see cref="ProjectionInfo"/>.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<ProjectionInfo> GetFlatList()
        {
            // Return DFS
            yield return this;
            foreach (var child in this.children.Select(child => child.GetFlatList()).SelectMany(list => list))
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
            return  $"{this.NodeType.Name} offset: [{this.CumulativeOffset}] origHdr: [{originalHeaderString}] " +
                    $"rewritnHdr: [{rewrittenHeaderString}] code: [{codeString}]";
        }
    }

    /// <summary>
    /// Wraps the collection of ProjectionInfos mapping to offsets in the rewritten C# file.
    /// </summary>
    public class ProjectionInfos
    {
        #region fields

        private ProjectionInfo[] orderedProjectionInfos;

        #endregion

        #region public API

        /// <summary>
        /// Returns a read-only list of <see cref="ProjectionInfo"/>s ordered by offset in the rewritten C# file,
        /// which is the same ordering as by offset in the original P# file.
        /// </summary>
        public IReadOnlyList<ProjectionInfo> OrderedProjectionInfos { get { return this.orderedProjectionInfos; } }

        /// <summary>
        /// The rewritten C# text.
        /// </summary>
        public string RewrittenCSharpText { get; private set; }

        #endregion

        #region internal API

        internal ProjectionInfos(IPSharpProgram program)
        {
            this.Root = new ProjectionInfo(program);
        }

        /// <summary>
        /// The root of the <see cref="ProjectionInfo"/> tree.
        /// </summary>
        internal ProjectionInfo Root;

        /// <summary>
        /// Called when the first rewrite (of structures) is complete.
        /// </summary>
        internal void OnInitialRewriteComplete()
        {
            this.orderedProjectionInfos = this.CreateFlatList().ToArray();
            AssertSortedByPSharpOffset();
        }

        /// <summary>
        /// Called when the second rewrite (of code terms within structures) is complete.
        /// </summary>
        internal void OnFinalRewriteComplete()
        {
            foreach (var info in this.orderedProjectionInfos)
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
            foreach (var child in this.Root.children)
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
        /// Return a flattened (Depth-First) list of <see cref="ProjectionInfo"/>s from the tree starting
        /// at the root.
        /// </summary>
        private IEnumerable<ProjectionInfo> CreateFlatList()
        {
            // Return DFS
            foreach (var child in this.Root.children.Select(child => child.GetFlatList()).SelectMany(list => list))
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
            for (var ii = 1; ii < this.orderedProjectionInfos.Length; ++ii)
            {
                Debug.Assert(this.orderedProjectionInfos[ii - 1].CumulativeOffset < this.orderedProjectionInfos[ii].CumulativeOffset);
            }
        }

        #endregion
    }
}
