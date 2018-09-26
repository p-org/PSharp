// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.VisualStudio
{
    internal class SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly SuggestedActionsSourceProvider SourceProvider;
        private readonly ITextView TextView;
        private readonly ITextBuffer Buffer;

        private bool IsDisposed;

        public event EventHandler<EventArgs> SuggestedActionsChanged;   // TODO unused

        public SuggestedActionsSource(SuggestedActionsSourceProvider suggestedActionsSourceProvider,
            ITextView textView, ITextBuffer textBuffer)
        {
            this.SourceProvider = suggestedActionsSourceProvider;
            this.TextView = textView;
            this.Buffer = textBuffer;
            this.IsDisposed = false;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                return false;   // TODO short-circuited for now

                TextExtent extent;
                if (!this.TryGetWordUnderCaret(out extent))
                {
                    return false;
                }

                var extentToken = new PSharpLexer().Tokenize(extent.Span.GetText()).FirstOrDefault();
                if (extentToken == null)
                {
                    return false;
                }

                var snapshot = extent.Span.Snapshot;
                var trackSpan = range.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
                var preSpan = new SnapshotSpan(snapshot, new Span(snapshot.GetLineFromLineNumber(0).Start,
                    trackSpan.GetStartPoint(snapshot).Position));

                var tokens = new PSharpLexer().Tokenize(preSpan.GetText());
                var parser = new PSharpParser(ParsingOptions.CreateDefault());
                parser.ParseTokens(tokens);

                var expected = parser.GetExpectedTokenTypes();
                if (this.IsExpectedTokenType(extentToken, expected))
                {
                    return false;
                }

                return extent.IsSignificant;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Enumerable.Empty<SuggestedActionSet>();  // TODO short-circuited for now 

            TextExtent extent;
            if (!this.TryGetWordUnderCaret(out extent) || !extent.IsSignificant)
            {
                return Enumerable.Empty<SuggestedActionSet>();
            }

            var extentToken = new PSharpLexer().Tokenize(extent.Span.GetText()).FirstOrDefault();
            if (extentToken == null)
            {
                return Enumerable.Empty<SuggestedActionSet>();
            }

            var snapshot = extent.Span.Snapshot;
            var trackSpan = range.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
            var preSpan = new SnapshotSpan(snapshot, new Span(snapshot.GetLineFromLineNumber(0).Start,
                trackSpan.GetStartPoint(snapshot).Position));

            var tokens = new PSharpLexer().Tokenize(preSpan.GetText());
            var parser = new PSharpParser(ParsingOptions.CreateDefault());
            parser.ParseTokens(tokens);

            var expected = parser.GetExpectedTokenTypes();
            if (this.IsExpectedTokenType(extentToken, expected))
            {
                return Enumerable.Empty<SuggestedActionSet>();
            }

            var errorFixAction = new ErrorFixSuggestedAction(trackSpan);

            return new SuggestedActionSet[] { new SuggestedActionSet(new ISuggestedAction[] { errorFixAction }) };
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Returns true if the given token is of the expected token type
        /// </summary>
        /// <returns></returns>
        private bool IsExpectedTokenType(Token token, List<TokenType> expectedTokenTypes)
        {
            if (token.Type == TokenType.WhiteSpace ||
                token.Type == TokenType.NewLine ||
                token.Type == TokenType.Comment ||
                token.Type == TokenType.CommentLine ||
                token.Type == TokenType.CommentStart ||
                token.Type == TokenType.CommentEnd ||
                expectedTokenTypes.Contains(token.Type))
            {
                return true;
            }

            return false;
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            var caret = this.TextView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            var navigator = this.SourceProvider.NavigatorService.GetTextStructureNavigator(this.Buffer);
            wordExtent = navigator.GetExtentOfWord(point);

            return true;
        }

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                GC.SuppressFinalize(this);
                this.IsDisposed = true;
            }
        }
    }
}
