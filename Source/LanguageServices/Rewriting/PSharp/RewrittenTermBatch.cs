﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// This class represents a list of instances of rewriting SyntaxNodes from P# terms to C# replacements.
    /// </summary>
    public class RewrittenTermBatch
    {
        /// <summary>
        /// Contains the current batch of rewritten nodes.
        /// </summary>
        private readonly List<RewrittenTerm> BatchTerms = new List<RewrittenTerm>();

        /// <summary>
        /// The list of projection buffer infos, ordered by <see cref="ProjectionNode.RewrittenCumulativeOffset"/>.
        /// </summary>
        private readonly ProjectionNode[] OrderedCSharpProjectionNodes;

        internal RewrittenTermBatch(IEnumerable<ProjectionNode> orderedCSharpProjectionNodes)
        {
            this.OrderedCSharpProjectionNodes = orderedCSharpProjectionNodes.ToArray();
        }

        internal void AddToBatch(SyntaxNode node, string rewrittenText)
        {
            this.BatchTerms.Add(new RewrittenTerm(node.ToString(), node.Span.Start, rewrittenText));
        }

        internal void MergeBatch()
        {
            if (this.BatchTerms.Count == 0)
            {
                return;
            }

            this.AssertBatchSorted();

            // Start positions within a batch are not adjusted by Roslyn for changes in preceding nodes because
            // they are from a single enumeration of the SyntaxTree nodes. However, start positions across
            // batches are adjusted for node changes in previous batches.
            this.AdjustStartsInBatch();

            this.AdjustExistingTermStarts();
            this.InsertBatchTermsIntoProjectionNodes();
            this.AdjustProjectionNodeOffsets();
            this.AdjustProjectionNodeCodeChunks();
            this.BatchTerms.Clear();
        }

        internal void OffsetStarts(int offset)
        {
            foreach (var term in this.ExistingRewrittenCodeTerms)
            {
                term.AddRewrittenOffset(offset);
            }

            foreach (var projInfo in this.OrderedCSharpProjectionNodes)
            {
                projInfo.AddOffset(offset);
            }
        }

        /// <summary>
        /// Asserts that the batch start positions are in ascending order.
        /// </summary>
        [Conditional("DEBUG")]
        private void AssertBatchSorted()
        {
            for (var ii = 1; ii < this.BatchTerms.Count; ++ii)
            {
                Debug.Assert(this.BatchTerms[ii - 1].RewrittenStart < this.BatchTerms[ii].RewrittenStart,
                             $"{nameof(RewrittenSpan.RewrittenStart)} is out of sequence");
            }
        }

        /// <summary>
        /// Cumulative adjustment for all starts in the batch, prior to merging the batch.
        /// </summary>
        private void AdjustStartsInBatch()
        {
            int offset = 0;
            foreach (var term in this.BatchTerms)
            {
                term.AddRewrittenOffset(offset);
                offset += term.ChangedLength;
            }
        }

        private IEnumerable<RewrittenTerm> ExistingRewrittenCodeTerms => this.OrderedCSharpProjectionNodes.Select(info => info.RewrittenCodeTerms).SelectMany(list => list);

        /// <summary>
        /// Merge the batch of terms with the existing term list, returning a new term list enumeration.
        /// </summary>
        private void AdjustExistingTermStarts()
        {
            var existingTerms = this.ExistingRewrittenCodeTerms;
            using (var existingTermEnumerator = existingTerms.GetEnumerator())
            using (var batchEnumerator = this.BatchTerms.GetEnumerator())
            {
                var hasTerm = existingTermEnumerator.MoveNext();
                var hasBatch = batchEnumerator.MoveNext();
                var offset = 0;
                while (hasTerm)
                {
                    if (hasBatch && batchEnumerator.Current.RewrittenStart < existingTermEnumerator.Current.RewrittenStart)
                    {
                        offset += batchEnumerator.Current.ChangedLength;
                        hasBatch = batchEnumerator.MoveNext();
                        continue;
                    }

                    existingTermEnumerator.Current.AddRewrittenOffset(offset);
                    hasTerm = existingTermEnumerator.MoveNext();
                }
            }
        }

        /// <summary>
        /// Insert new <see cref="RewrittenTerm"/>s from the current batch into the appropriate
        /// <see cref="ProjectionNode"/>s.
        /// </summary>
        private void InsertBatchTermsIntoProjectionNodes()
        {
            using (var batchEnumerator = this.BatchTerms.GetEnumerator())
            {
                var curProjIndex = 0;
                bool getNextProjOffset(out int offset)
                {
                    var endOfList = curProjIndex >= this.OrderedCSharpProjectionNodes.Length - 1;
                    offset = endOfList ? -1 : this.OrderedCSharpProjectionNodes[curProjIndex + 1].RewrittenCumulativeOffset;
                    return !endOfList;
                }

                void advanceToInnermostContainingProjInfo()
                {
                    while (getNextProjOffset(out int nextProjOffset) && nextProjOffset < batchEnumerator.Current.RewrittenStart)
                    {
                        ++curProjIndex;
                    }
                }

                void insertBatchTerm()
                {
                    var curProjInfo = this.OrderedCSharpProjectionNodes[curProjIndex];
                    var rewrittenIndex = curProjInfo.RewrittenCodeTerms.Count == 0
                        ? -1
                        : curProjInfo.RewrittenCodeTerms.FindLastIndex(term => term.RewrittenStart < batchEnumerator.Current.RewrittenStart);
                    curProjInfo.RewrittenCodeTerms.Insert(rewrittenIndex + 1, batchEnumerator.Current);
                    batchEnumerator.Current.ProjectionNode = curProjInfo;
                }

                var hasBatch = batchEnumerator.MoveNext();
                while (hasBatch)
                {
                    advanceToInnermostContainingProjInfo();
                    insertBatchTerm();
                    hasBatch = batchEnumerator.MoveNext();
                }
            }
        }

        /// <summary>
        /// Adjust offsets for the new <see cref="ProjectionNode"/>s based on the <see cref="RewrittenTerm"/>s
        /// in the current batch.
        /// </summary>
        private void AdjustProjectionNodeOffsets()
        {
            var projEnumerator = this.OrderedCSharpProjectionNodes.GetEnumerator();
            using (var batchEnumerator = this.BatchTerms.GetEnumerator())
            {
                var hasProj = projEnumerator.MoveNext();
                var hasTerm = batchEnumerator.MoveNext();
                var offset = 0;
                void updateProjInfoAndMoveNext()
                {
                    var projInfo = (ProjectionNode)projEnumerator.Current;
                    projInfo.AddOffset(offset);
                    hasProj = projEnumerator.MoveNext();
                }

                // Note that the last ProjectionNode may contain some of the batch terms, but they won't affect
                // the offset of that ProjectionNode.
                while (hasTerm && hasProj)
                {
                    var curProjInfo = (ProjectionNode)projEnumerator.Current;
                    if (batchEnumerator.Current.RewrittenStart < curProjInfo.RewrittenCumulativeOffset)
                    {
                        offset += batchEnumerator.Current.ChangedLength;
                        hasTerm = batchEnumerator.MoveNext();
                        continue;
                    }

                    updateProjInfoAndMoveNext();
                }

                // Update the remaining ProjectionNodes that may be after the final term.
                while (hasProj)
                {
                    updateProjInfoAndMoveNext();
                }
            }
        }

        /// <summary>
        /// Adjust <see cref="ProjectionNode.CodeChunk"/>s for the current term batch.
        /// </summary>
        private void AdjustProjectionNodeCodeChunks()
        {
            foreach (var term in this.BatchTerms)
            {
                term.ProjectionNode.IncrementCodeChunkLength(term.ChangedLength);
            }
        }
    }
}
