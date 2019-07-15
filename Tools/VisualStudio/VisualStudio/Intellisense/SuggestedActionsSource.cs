// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.LanguageServices;
using System.Collections;

namespace Microsoft.PSharp.VisualStudio
{
    internal class SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly SuggestedActionsSourceProvider sourceProvider;
        private readonly ITextView textView;
        private readonly ITextBuffer textBuffer;

        private bool isDisposed;

        public event EventHandler<EventArgs> SuggestedActionsChanged;   // TODO unused; implement Workspace registration change

        public SuggestedActionsSource(SuggestedActionsSourceProvider suggestedActionsSourceProvider,
            ITextView textView, ITextBuffer textBuffer)
        {
            this.sourceProvider = suggestedActionsSourceProvider;
            this.textView = textView;
            this.textBuffer = textBuffer;
            this.isDisposed = false;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                return (!this.TryGetWordUnderCaret(out TextExtent extent)
                        || !ExtentTokenHasSuggestedReplacements(range, ref extent, out _, out _))
                    ? false
                    : extent.IsSignificant;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return !this.TryGetWordUnderCaret(out TextExtent textExtent) || !textExtent.IsSignificant
                    || !ExtentTokenHasSuggestedReplacements(range, ref textExtent, out ITrackingSpan trackingSpan, out PSharpParser parser)
                ? Enumerable.Empty<SuggestedActionSet>()
                : GetSuggestedActions(textExtent, trackingSpan, parser);
        }

        private bool ExtentTokenHasSuggestedReplacements(SnapshotSpan range, ref TextExtent extent, out ITrackingSpan trackSpan, out PSharpParser parser)
        {
            string extentText = extent.Span.GetText();
            var extentToken = string.IsNullOrWhiteSpace(extentText) ? null : new PSharpLexer().Tokenize(extentText).FirstOrDefault();
            if (extentToken == null)
            {
                trackSpan = null;
                parser = null;
                return false;
            }

            // Parse the file up to and including trackSpan. TODO: Minimize the re-parse span for speed.
            var snapshot = extent.Span.Snapshot;
            trackSpan = snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
            var preSpan = new SnapshotSpan(snapshot, new Span(snapshot.GetLineFromLineNumber(0).Start,
                                           trackSpan.GetEndPoint(snapshot).Position));

            var tokens = new PSharpLexer().Tokenize(preSpan.GetText());
            parser = new PSharpParser(ParsingOptions.CreateForVsLanguageService());
            try
            {
                parser.ParseTokens(tokens);
            }
            catch (ParsingException ex)
            {
                // Parsing exception is expected. If FailingToken is null, then the parser's TokenStream is Done,
                // which means there was no exception before the stream ended early. If we're not currently on the
                // FailingToken's (1-based) line, then we don't want to suggest its actions for the current IDE (0-based) line.
                if (ex.FailingToken == null || ex.FailingToken.TextUnit.Line - 1 != extent.Span.Start.GetContainingLine().LineNumber)
                {
                    return false;
                }

                // Make sure the extent token we return is the failing token, because that's what we're making suggestions for.
                if (extent.Span.Start.Position != ex.FailingToken.TextUnit.Start)
                {
                    var line = snapshot.GetLineFromLineNumber(ex.FailingToken.TextUnit.Line - 1);
                    var span = new SnapshotSpan(line.Start + ex.FailingToken.TextUnit.Start /* 0-based */, ex.FailingToken.TextUnit.Length);
                    extent = new TextExtent(span, true);

                    // Later code assumes the token resulted from parsing only the text of the token.
                    extentToken = new Token(new TextUnit(ex.FailingToken.Text, ex.FailingToken.TextUnit.Line, 0), ex.FailingToken.Type);
                    trackSpan = snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
                }
            }

            return !this.IsExpectedTokenType(extentToken, parser.GetExpectedTokenTypes())
                   && GetSuggestions(parser, extentToken.Text).Any();
        }

        private static IEnumerable<KeyValuePair<string, string>> GetSuggestions(PSharpParser parser, string word)
            => CompletionSource.RefineAvailableKeywords(parser.GetExpectedTokenTypes(), word, usePrefix: false);

        private IEnumerable<SuggestedActionSet> GetSuggestedActions(TextExtent textExtent, ITrackingSpan trackingSpan, PSharpParser parser)
        {
            var span = textExtent.Span;
            var word = span.Snapshot.TextBuffer.CurrentSnapshot.GetText().Substring(span.Start.Position, span.End.Position - span.Start.Position);

            // Start with prefix search
            var suggestions = GetSuggestions(parser, word).ToList();
            foreach (var suggestion in suggestions)
            {
                yield return new SuggestedActionSet(new ISuggestedAction[] { new ErrorFixSuggestedAction(word, suggestion, trackingSpan) });
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Returns true if the given token is of the expected token type, or is an ignorable token
        /// </summary>
        /// <returns></returns>
        private bool IsExpectedTokenType(Token token, TokenType[] expectedTokenTypes)
            => PSharpQuickInfoSource.IsIgnoreTokenType(token.Type) || expectedTokenTypes.Contains(token.Type);

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            var caret = this.textView.Caret;
            if (caret.Position.BufferPosition > 0)
            {
                var position = caret.Position.BufferPosition.Position;
                var projBuf = this.textView.BufferGraph.TopBuffer;
                var projSnapshotPoint = new SnapshotPoint(projBuf.CurrentSnapshot, position);
                var editBufferPoint = this.textView.BufferGraph.MapDownToBuffer(projSnapshotPoint, PointTrackingMode.Positive,
                                            this.textBuffer, PositionAffinity.Predecessor);
                if (editBufferPoint.HasValue)
                {
                    var navigator = this.sourceProvider.NavigatorService.GetTextStructureNavigator(this.textBuffer);
                    wordExtent = navigator.GetExtentOfWord(editBufferPoint.Value - 1);
                    return true;
                }
            }

            wordExtent = default(TextExtent);
            return false;
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
            }
        }
    }
}
