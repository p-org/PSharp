using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio.Outlining
{
    internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>
    {
        const string ellipsis = "...";              // The characters that are displayed when the region is collapsed

        readonly ITextBuffer textBuffer;
        ITextSnapshot currentSnapshot;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        internal readonly RegionParser RegionParser = new RegionParser(RegionParser.BoundaryChar.All);

        public OutliningTagger(ITextBuffer textBuffer)
        {
            this.textBuffer = textBuffer;
            this.currentSnapshot = textBuffer.CurrentSnapshot;
            this.ReParse();
            this.textBuffer.Changed += BufferChanged;
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot currentSnapshot = this.currentSnapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End)
                                      .TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;
            foreach (var region in this.RegionParser.CurlyBraceRegions)
            {
                if (region.StartLineNumber <= endLineNumber && region.EndLineNumber >= startLineNumber)
                {
                    // If the startLine is whitespace-only before the bracket, then the region should start at the end of the preceding line.
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLineNumber);
                    var startOffset = region.StartOffset;
                    if (string.IsNullOrWhiteSpace(startLine.GetText().Substring(0, region.StartOffset)) && region.StartLineNumber > 0)
                    {
                        startLine = currentSnapshot.GetLineFromLineNumber(region.StartLineNumber - 1);
                        startOffset = startLine.GetText().TrimEnd().Length;
                    }
                    var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLineNumber);
                    var outlineSpan = new SnapshotSpan(startLine.Start + startOffset, endLine.Start + region.EndOffset);

                    // Get the contents of the tooltip for the collapsed span.
                    const int maxLines = 20;
                    string getTruncatedText()
                    {
                        var truncatedEndLine = currentSnapshot.GetLineFromLineNumber(region.StartLineNumber + maxLines);
                        var truncatedSpan = new SnapshotSpan(startLine.Start, truncatedEndLine.Start + truncatedEndLine.Length);
                        return currentSnapshot.GetText(truncatedSpan) + Environment.NewLine + ellipsis;
                    }

                    // The hoverText starts at the beginning of StartLine and ends at the end of EndLine.
                    var hoverText = region.EndLineNumber - region.StartLineNumber <= maxLines
                        ? currentSnapshot.GetText(new SnapshotSpan(startLine.Start, endLine.Start + region.EndOffset))
                        : getTruncatedText();

                    yield return new TagSpan<IOutliningRegionTag>(outlineSpan,
                                                                  new OutliningRegionTag(false, false, ellipsis, hoverText));
                }
            }
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After == textBuffer.CurrentSnapshot)
            {
                this.ReParse();
            }
        }

        void ReParse()
        {
            ITextSnapshot newSnapshot = textBuffer.CurrentSnapshot;

            // Store off the current spans.
            var oldSpans = new List<Span>(this.RegionParser.CurlyBraceRegions.Select(r => AsSnapshotSpan(r, this.currentSnapshot)
                                                      .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span));

            this.RegionParser.Parse(newSnapshot);

            // Determine the changed span, and send a changed event with the new spans
            var newSpans = new List<Span>(this.RegionParser.CurlyBraceRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

            // The changed regions are regions that appear in one set or the other, but not both.
            var oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            var newSpanCollection = new NormalizedSpanCollection(newSpans);
            var removed = NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if (removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if (newSpans.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpans[0].Start);
                changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
            }

            this.currentSnapshot = newSnapshot;

            if (changeStart <= changeEnd)
            {
                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(this.currentSnapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }

        static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLineNumber);
            var endLine = region.StartLineNumber == region.EndLineNumber ? startLine : snapshot.GetLineFromLineNumber(region.EndLineNumber);
            return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.Start + region.EndOffset);
        }
    }
}
