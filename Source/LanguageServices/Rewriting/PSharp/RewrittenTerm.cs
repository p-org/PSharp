using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// This class represents a single instance of rewriting a SyntaxNode from a P# term to a C# replacement.
    /// </summary>
    public class RewrittenTerm
    {
        #region properties

        /// <summary>
        /// Start position of this node in the rewritten C# text buffer.
        /// </summary>
        public int Start { get; private set; }

        /// <summary>
        /// Original (P#) string at this position in the C# text buffer.
        /// </summary>
        public string OriginalString { get; private set; }

        /// <summary>
        /// Length of the original (P#) string at this position in the C# text buffer.
        /// </summary>
        public int OriginalLength => this.OriginalString.Length;

        /// <summary>
        /// Rewritten (C#) string at this position in the C# text buffer.
        /// </summary>
        public string RewrittenString { get; private set; }

        /// <summary>
        /// Length of the rewritten (C#) string at this position in the C# text buffer.
        /// </summary>
        public int RewrittenLength => this.RewrittenString.Length;

        /// <summary>
        /// Difference in length between original and replacement.
        /// </summary>
        public int ChangedLength => this.RewrittenLength - this.OriginalLength;

        #endregion

        #region internal API

        internal RewrittenTerm(int start, string original, string rewritten)
        {
            this.Start = start;
            this.OriginalString = original;
            this.RewrittenString = rewritten;
        }

        internal void OffsetStart(int offset)
        {
            this.Start += offset;
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the rewritten term.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var change = this.ChangedLength.ToString("+0;-#");
            return $"{this.Start}({change}) {this.OriginalString}({this.OriginalLength}) -> {this.RewrittenString}({this.RewrittenLength})";
        }
    }

    /// <summary>
    /// This class represents a list of instances of rewriting SyntaxNodes from P# terms to a C# replacements.
    /// </summary>
    public class RewrittenTerms : IEnumerable<RewrittenTerm>
    {
        #region fields
        
        /// <summary>
        /// The cumulative list of nodes.
        /// </summary>
        private List<RewrittenTerm> Terms = new List<RewrittenTerm>();

        /// <summary>
        /// Contains the current batch of rewritten nodes.
        /// </summary>
        private List<RewrittenTerm> BatchTerms = new List<RewrittenTerm>();

        #endregion

        #region API

        /// <summary>
        /// Returns the rewritten term at the passed index
        /// </summary>
        /// <param name="index">Index of the rewritten term to return</param>
        /// <returns></returns>
        public RewrittenTerm this[int index] { get { return this.Terms[index]; } }

        #region IEnumerable<RewrittenTerm>

        /// <summary>
        /// Returns an enumerator over the rewritten terms.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<RewrittenTerm> GetEnumerator()
        {
            return ((IEnumerable<RewrittenTerm>)Terms).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<RewrittenTerm>)Terms).GetEnumerator();
        }

        #endregion
        
        #endregion

        #region internal API

        internal void AddToBatch(SyntaxNode node, string rewrittenText)
        {
            this.BatchTerms.Add(new RewrittenTerm(node.Span.Start, node.ToString(), rewrittenText));
        }

        internal void MergeBatch()
        {
            if (this.BatchTerms.Count == 0)
            {
                return;
            }
            AssertBatchSorted();

            // Start positions in a batch are not adjusted for changes in preceding nodes as they are from a single enumeration of
            // the SyntaxTree nodes. However, start positions across batches are adjusted for node changes in previous batches.
            AdjustStartsInBatch();
            this.Terms = MergeAndAdjustStarts().ToList();
            this.BatchTerms.Clear();
        }

        internal void OffsetStarts(int offset)
        {
            foreach (var term in this.Terms)
            {
                term.OffsetStart(offset);
            }
        }

        #endregion

        #region private methods

        [Conditional("DEBUG")]
        private void AssertBatchSorted()
        {
            for (var ii = 1; ii < this.BatchTerms.Count; ++ii)
            {
                Debug.Assert(this.BatchTerms[ii - 1].Start < this.BatchTerms[ii].Start);
            }
        }

        private void AdjustStartsInBatch()
        {
            int offset = 0;
            foreach (var term in this.BatchTerms)
            {
                term.OffsetStart(offset);
                offset += term.ChangedLength;
            }
        }

        private IEnumerable<RewrittenTerm> MergeAndAdjustStarts()
        {
            using (var termEnumerator = this.Terms.GetEnumerator())
            using (var batchEnumerator = this.BatchTerms.GetEnumerator())
            {
                var hasTerm = termEnumerator.MoveNext();
                var hasBatch = batchEnumerator.MoveNext();
                var offset = 0;
                while (hasTerm)
                {
                    if (hasBatch && batchEnumerator.Current.Start < termEnumerator.Current.Start)
                    {
                        offset += batchEnumerator.Current.ChangedLength;
                        yield return batchEnumerator.Current;
                        hasBatch = batchEnumerator.MoveNext();
                        continue;
                    }
                    termEnumerator.Current.OffsetStart(offset);
                    yield return termEnumerator.Current;
                    hasTerm = termEnumerator.MoveNext();
                }
                while (hasBatch)
                {
                    yield return batchEnumerator.Current;
                    hasBatch = batchEnumerator.MoveNext();
                }
            }
        }

        #endregion
    }
}
