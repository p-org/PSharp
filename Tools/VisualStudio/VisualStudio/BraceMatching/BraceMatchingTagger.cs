// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.PSharp.VisualStudio.Outlining;

namespace Microsoft.PSharp.VisualStudio
{
    internal class BraceMatchingTagger : ITagger<TextMarkerTag>
    {
        ITextView View { get; set; }
        ITextBuffer SourceBuffer { get; set; }
        SnapshotPoint? CurrentChar { get; set; }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private readonly OutliningTagger outliningTagger;
        private RegionParser RegionParser => this.outliningTagger.RegionParser;

        internal BraceMatchingTagger(ITextView view, ITextBuffer sourceBuffer)
        {
            this.View = view;
            this.SourceBuffer = sourceBuffer;
            this.CurrentChar = null;

            // Share the RegionParser
            this.outliningTagger = OutliningTaggerProvider.GetOrCreateOutliningTagger(sourceBuffer);

            this.View.Caret.PositionChanged += CaretPositionChanged;
            this.View.LayoutChanged += ViewLayoutChanged;
        }

        void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewSnapshot != e.OldSnapshot) //make sure that there has really been a change
            {
                UpdateAtCaretPosition(View.Caret.Position);
            }
        }

        void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e) 
            => UpdateAtCaretPosition(e.NewPosition);

        void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            this.CurrentChar = caretPosition.Point.GetPoint(SourceBuffer, caretPosition.Affinity);
            if (this.CurrentChar.HasValue)
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, 
                                                                                     SourceBuffer.CurrentSnapshot.Length)));
            }
        }

        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // Do nothing if there is no content in the buffer, the current SnapshotPoint is not initialized, or we're at the end of the buffer.
            if (spans.Count == 0 || !CurrentChar.HasValue || CurrentChar.Value.Position >= CurrentChar.Value.Snapshot.Length)
            {
                yield break;
            }

            // Hold on to the current character SnapshotPoint, translating to the span snapshot if that is different.
            SnapshotPoint currentCharPoint = spans[0].Snapshot == CurrentChar.Value.Snapshot
                ? CurrentChar.Value
                : CurrentChar.Value.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);

            // If the current char is an opening char, stay on it. Otherwise, back up one to see if the previous char is the close char.
            char currentChar = currentCharPoint.GetChar();
            var currentLine = currentCharPoint.GetContainingLine();

            bool isBoundaryChar(out bool isOpen)
            {
                isOpen = RegionParser.IsOpenChar(currentChar);
                if (isOpen)
                {
                    return true;
                }
                if (currentCharPoint.Position != currentLine.Start.Position)
                {
                    currentCharPoint -= 1;
                    currentChar = currentCharPoint.GetChar();
                    return RegionParser.IsCloseChar(currentChar);
                }
                return false;
            }

            if (!isBoundaryChar(out bool isOpenChar))
            {
                yield break;
            }

            bool findRegion(Region[] regions, out Region region)
            {
                var currentCharOffset = currentCharPoint.Position - currentLine.Start;
                region = regions.FirstOrDefault(reg => isOpenChar ? reg.StartLineNumber == currentLine.LineNumber && reg.StartOffset == currentCharOffset
                                                                  : reg.EndLineNumber == currentLine.LineNumber && reg.EndOffset == currentCharOffset + 1);
                return region != null;
            }

            bool getRegion(out Region region)
            {
                switch (currentChar)
                {
                    case RegionParser.OpenCurlyBrace:
                    case RegionParser.CloseCurlyBrace:
                        return findRegion(this.RegionParser.CurlyBraceRegions, out region);

                    case RegionParser.OpenSquareBrace:
                    case RegionParser.CloseSquareBrace:
                        return findRegion(this.RegionParser.SquareBraceRegions, out region);

                    case RegionParser.OpenParenthesis:
                    case RegionParser.CloseParenthesis:
                        return findRegion(this.RegionParser.ParenthesisRegions, out region);
                }
                region = null;
                return false;
            }

            if (getRegion(out Region foundRegion))
            {
                TagSpan<TextMarkerTag> getTag(int lineNumber, int offset)
                {
                    var line = currentCharPoint.Snapshot.GetLineFromLineNumber(lineNumber);
                    return new TagSpan<TextMarkerTag>(new SnapshotSpan(line.Start + offset, 1), new TextMarkerTag("blue"));
                }

                yield return getTag(foundRegion.StartLineNumber, foundRegion.StartOffset);
                yield return getTag(foundRegion.EndLineNumber, foundRegion.EndOffset - 1); // -1 here because EndOffset is after the close char
            }
        }
    }
}
