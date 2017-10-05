using System;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Class that holds information about rewritten spans in the P#-to-C# rewrite process.
    /// </summary>
    public class RewrittenSpan
    {
        #region fields

        /// <summary>
        /// Allows the owner to pass in a function that will calculate the rewritten start position
        /// based on offsets (see <see cref="ProjectionInfo"/>).
        /// </summary>
        internal Func<int> GetRewrittenStartFunc = () => -1;

        /// <summary>
        /// Allows the owner to pass in a function that will calculate the rewritten string
        /// based on a dynamic string (see <see cref="ProjectionInfo"/>).
        /// </summary>
        internal Func<string> GetRewrittenStringFunc = () => string.Empty;

        #endregion

        #region properties

        /// <summary>
        /// Start position of this span in the original P# text buffer.
        /// May be constant or dynamically adjusted via <see cref="ProjectionInfo.SetCodeTermOriginalPositions()"/>.
        /// </summary>
        public int OriginalStart { get; internal set; } = -1;

        /// <summary>
        /// Original (P#) string at this position in the C# text buffer.
        /// </summary>
        public string OriginalString { get; internal set; } = string.Empty;

        /// <summary>
        /// Length of the original (P#) string at this position in the C# text buffer.
        /// </summary>
        public int OriginalLength => this.OriginalString.Length;

        /// <summary>
        /// End position of the original (P#) string at this position in the C# text buffer.
        /// </summary>
        public int OriginalEnd => this.OriginalStart + this.OriginalLength;

        /// <summary>
        /// Start position of the corresponding span in the rewritten C# text buffer.
        /// </summary>
        public int RewrittenStart => GetRewrittenStartFunc();

        /// <summary>
        /// Rewritten (C#) string at this position in the C# text buffer.
        /// </summary>
        public string RewrittenString => GetRewrittenStringFunc();

        /// <summary>
        /// Length of the rewritten (C#) string at this position in the C# text buffer.
        /// May be constant or dynamically adjusted due to <see cref="RewrittenTerm"/>s.
        /// </summary>
        public int RewrittenLength { get; internal set; }

        /// <summary>
        /// End position of the rewritten (C#) string at this position in the C# text buffer.
        /// </summary>
        public int RewrittenEnd => this.RewrittenStart + this.RewrittenLength;

        /// <summary>
        /// Difference in length between original and replacement.
        /// </summary>
        public int ChangedLength => this.RewrittenLength - this.OriginalLength;

        #endregion

        #region constructor

        internal RewrittenSpan(string originalString = "")
        {
            this.OriginalString = originalString;
        }

        #endregion
    }
}
