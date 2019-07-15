// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# error tagger (red squiggles).
    /// </summary>
    internal sealed class PSharpErrorTagger : ITagger<IErrorTag>
    {
        private ITextBuffer buffer;

        int lastSnapshotVersionNumber = -1;
        SnapshotSpan lastTokenSpan;
        string lastExpected = string.Empty;
        string lastActual = string.Empty;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">ITextBuffer</param>
        internal PSharpErrorTagger(ITextBuffer buffer) => this.buffer = buffer;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            var snapshot = this.buffer.CurrentSnapshot; // TODO reduce the size on this; but do so without getting spurious errors
            if (snapshot.Version.VersionNumber != this.lastSnapshotVersionNumber)
            {
                this.lastSnapshotVersionNumber = snapshot.Version.VersionNumber;
                this.lastExpected = string.Empty;
                this.lastActual = string.Empty;
                if (GetExpectedTokenTypes(snapshot, out Token failingToken, out TokenType[] expectedTokenTypes))
                {
                    var line = snapshot.GetLineFromLineNumber(failingToken.TextUnit.Line - 1);  // Token line is 1-based
                    this.lastTokenSpan = failingToken != null
                        ? new SnapshotSpan(snapshot, new Span(line.Start + failingToken.TextUnit.Start, failingToken.TextUnit.Length))
                        : new SnapshotSpan(snapshot, new Span(snapshot.Length - 1, 1));
                    this.lastActual = failingToken != null
                        ? TokenTypeRegistry.GetText(failingToken.Type)
                        : "EOF";
                    if (string.IsNullOrEmpty(this.lastActual))
                    {
                        this.lastActual = failingToken.Text;
                    }
                    this.lastExpected = string.Join(", ", expectedTokenTypes.Select(tt => TokenTypeRegistry.GetText(tt)));
                }
            }

            if (!string.IsNullOrEmpty(this.lastExpected) && !string.IsNullOrEmpty(this.lastActual))
            {
                yield return new TagSpan<IErrorTag>(this.lastTokenSpan, new ErrorTag(this.lastActual, $"Unexpected token type {this.lastActual}; expected one of {this.lastExpected}"));
            }
        }

        private static bool GetExpectedTokenTypes(ITextSnapshot snapshot, out Token failingToken, out TokenType[] expectedTokenTypes)
        {
            var tokens = new PSharpLexer().Tokenize(snapshot.GetText());
            var parser = new PSharpParser(ParsingOptions.CreateForVsLanguageService());
            failingToken = null;
            expectedTokenTypes = null;
            try
            {
                parser.ParseTokens(tokens);
                return false;
            }
            catch (ParsingException ex)
            {
                // We expect a parsing exception as we are probably in the middle of a word
                failingToken = ex.FailingToken;
                expectedTokenTypes = parser.GetExpectedTokenTypes() ?? Array.Empty<TokenType>();
                return true;
            }
        }
    }
}
