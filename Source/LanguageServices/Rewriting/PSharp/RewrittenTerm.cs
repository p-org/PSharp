// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// This class represents a single instance of rewriting a SyntaxNode from a P# term to a C# replacement.
    /// </summary>
    public class RewrittenTerm : RewrittenSpan
    {
        /// <summary>
        /// Holds the rewritten start position which may be updated by <see cref="AddRewrittenOffset(int)"/>.
        /// </summary>
        private int rewrittenStart;

        /// <summary>
        /// The <see cref="ProjectionNode"/> containing the <see cref="ProjectionNode.CodeChunk"/>
        /// that contains this term.
        /// </summary>
        public ProjectionNode ProjectionNode;

        internal RewrittenTerm(string originalString, int rewrittenStart, string rewrittenString)
            : base(originalString)
        {
            this.GetRewrittenStartFunc = () => this.rewrittenStart;
            this.GetRewrittenStringFunc = () => rewrittenString;
            this.rewrittenStart = rewrittenStart;
            this.RewrittenLength = rewrittenString.Length;
        }

        internal void AddRewrittenOffset(int offset) => this.rewrittenStart += offset;

        /// <summary>
        /// Returns a string representation of the rewritten term.
        /// </summary>
        public override string ToString()
        {
            var change = this.ChangedLength.ToString("+0;-#");
            return $"{this.RewrittenStart}({change}) {this.OriginalString}({this.OriginalLength}) -> {this.RewrittenString}({this.RewrittenLength})";
        }
    }
}
