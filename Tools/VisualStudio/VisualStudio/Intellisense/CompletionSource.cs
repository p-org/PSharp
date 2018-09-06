﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# completion source.
    /// </summary>
    internal class CompletionSource : ICompletionSource
    {
        private readonly CompletionSourceProvider SourceProvider;
        private readonly ITextBuffer Buffer;

        private List<Completion> CompletionList;

        private bool IsDisposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sourceProvider">SourceProvider</param>
        /// <param name="textBuffer">TextBuffer</param>
        public CompletionSource(CompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            this.SourceProvider = sourceProvider;
            this.Buffer = textBuffer;
            this.IsDisposed = false;
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            return; // TODO short-circuited for now

            var keywords = Keywords.Get();

            var snapshot = this.Buffer.CurrentSnapshot;
            var trackSpan = this.FindTokenSpanAtPosition(session.GetTriggerPoint(this.Buffer), session);
            var preSpan = new SnapshotSpan(snapshot, new Span(snapshot.GetLineFromLineNumber(0).Start,
                trackSpan.GetStartPoint(snapshot).Position));

            var tokens = new PSharpLexer().Tokenize(preSpan.GetText());
            var parser = new PSharpParser(ParsingOptions.CreateDefault());
            parser.ParseTokens(tokens);
            this.RefineAvailableKeywords(parser.GetExpectedTokenTypes(), keywords);

            this.CompletionList = new List<Completion>();

            foreach (var keyword in keywords)
            {
                this.CompletionList.Add(new Completion(keyword.Key, keyword.Key,
                    keyword.Value.Item1, null, null));
            }

            if (keywords.Count == 0)
            {
                return;
            }

            completionSets.Add(new CompletionSet(
                "Tokens",
                "Tokens",
                trackSpan,
                this.CompletionList,
                null));
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            var currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            var navigator = this.SourceProvider.NavigatorService.GetTextStructureNavigator(this.Buffer);
            var extent = navigator.GetExtentOfWord(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        /// <summary>
        /// Refines the available keywords
        /// </summary>
        /// <param name="expectedTokenTypes">Expected token types</param>
        /// <param name="keywords">Keywords</param>
        private void RefineAvailableKeywords(List<TokenType> expectedTokens,
            Dictionary<string, Tuple<string>> keywords)
        {
            var tokens = expectedTokens.Select(val => TokenTypeRegistry.GetText(val));
            foreach (var key in keywords.Keys.Where(val => !tokens.Contains(val)).ToList())
            {
                keywords.Remove(key);
            }
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
