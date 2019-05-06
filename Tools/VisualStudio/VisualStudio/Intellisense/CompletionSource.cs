// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.VisualStudio
{
#if false // TODO: Statement completion requires NotYetImplemented ProjectionTree so we don't try to apply P# operations in C# blocks.
    /// <summary>
    /// The P# completion source.
    /// </summary>
    internal class CompletionSource : ICompletionSource
    {
        private readonly CompletionSourceProvider SourceProvider;
        private readonly ITextBuffer Buffer;

        private List<Completion> CompletionList;
        internal static HashSet<TokenType> DoNotSuggest = new HashSet<TokenType>
        {
            TokenType.NewLine, TokenType.WhiteSpace, TokenType.Identifier,
            TokenType.LeftCurlyBracket,
            TokenType.RightCurlyBracket,
            TokenType.LeftParenthesis,
            TokenType.RightParenthesis,
            TokenType.LeftSquareBracket,
            TokenType.RightSquareBracket,
            TokenType.LeftAngleBracket,
            TokenType.RightAngleBracket,
            TokenType.MachineLeftCurlyBracket,
            TokenType.MachineRightCurlyBracket,
            TokenType.StateLeftCurlyBracket,
            TokenType.StateRightCurlyBracket,
            TokenType.StateGroupLeftCurlyBracket,
            TokenType.StateGroupRightCurlyBracket,
            TokenType.Semicolon,
            TokenType.Colon,
            TokenType.Comma,
            TokenType.Dot,
            TokenType.EqualOp,
            TokenType.AssignOp,
            TokenType.InsertOp,
            TokenType.RemoveOp,
            TokenType.NotEqualOp,
            TokenType.LessOrEqualOp,
            TokenType.GreaterOrEqualOp,
            TokenType.LambdaOp,
            TokenType.PlusOp,
            TokenType.MinusOp,
            TokenType.MulOp,
            TokenType.DivOp,
            TokenType.ModOp,
            TokenType.LogNotOp,
            TokenType.LogAndOp,
            TokenType.LogOrOp,
            TokenType.Using,
            TokenType.This,
            TokenType.Base,
            TokenType.New
        };

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
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var extent = GetExtentOfCurrentWord(session);
            var prefix = this.Buffer.CurrentSnapshot.GetText().Substring(extent.Span.Start.Position, extent.Span.End.Position - extent.Span.Start.Position);

            var availableKeywords = GetAvailableKeywords(extent.Span.Start.Position - 1);
            this.CompletionList = RefineAvailableKeywords(availableKeywords, prefix, usePrefix: true)
                                  .Select(kvp => new Completion(kvp.Key, kvp.Key, kvp.Value, null, null)).ToList();
            if (this.CompletionList.Count > 0)
            {
                var trackSpan = this.Buffer.CurrentSnapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
                completionSets.Add(new CompletionSet("Tokens", "Tokens", trackSpan, this.CompletionList, null));
            }
        }

        private Microsoft.VisualStudio.Text.Operations.TextExtent GetExtentOfCurrentWord(ICompletionSession session)
        {
            var triggerPoint = session.GetTriggerPoint(this.Buffer);
            var currentPoint = triggerPoint.GetPoint(this.Buffer.CurrentSnapshot);
            var line = currentPoint.GetContainingLine();

            // GetExtentOfWord will return whitespace, so we need to back up into the word.
            var lineText = line.GetText();
            while (currentPoint > line.Start
                   && (currentPoint == line.End || char.IsWhiteSpace(lineText[currentPoint.Position - line.Start.Position])))
            {
                currentPoint -= 1;
            }
            var navigator = this.SourceProvider.NavigatorService.GetTextStructureNavigator(this.Buffer);
            var extent = navigator.GetExtentOfWord(currentPoint);
            return extent;
        }

        private IEnumerable<TokenType> GetAvailableKeywords(int precedingLength)
        {
            // Parse preceding words for context to filter suggestions. TODO: Minimize the re-parse span for speed.
            var precedingString = precedingLength <= 0 ? string.Empty : this.Buffer.CurrentSnapshot.GetText().Substring(0, precedingLength).Trim();
            if (precedingString.Length > 0)
            {
                var tokens = new PSharpLexer().Tokenize(precedingString);
                if (tokens.Count > 0)
                {
                    var parser = new PSharpParser(ParsingOptions.CreateForVsLanguageService());
                    try
                    {
                        parser.ParseTokens(tokens);
                    }
                    catch (ParsingException)
                    {
                        // We expect a parsing exception as we are probably in the middle of a word
                    }
                    return parser.GetExpectedTokenTypes();
                }
            }
            return new List<TokenType>();
        }

        /// <summary>
        /// Refines the available keywords
        /// </summary>
        /// <param name="expectedTokenTypes">Expected token types</param>
        /// <param name="keywords">Keywords</param>
        internal static IEnumerable<KeyValuePair<string, string>> RefineAvailableKeywords(IEnumerable<TokenType> expectedTokenTypes, string word, bool usePrefix)
        {
            foreach (var tokenType in expectedTokenTypes.Where(tokType => !DoNotSuggest.Contains(tokType)))
            {
                var tokenString = TokenTypeRegistry.GetText(tokenType);
                if (Keywords.DefinitionMap.TryGetValue(tokenString, out string initialDef) && IsMatch(word, tokenString, usePrefix))
                {
                    // TODO: Consolidate the various lists and dictionaries.
                    var tokenDefinition = PSharpQuickInfoSource.TokenTypeTips.TryGetValue(tokenType, out string betterDef) ? betterDef : initialDef;
                    yield return new KeyValuePair<string, string>(tokenString, tokenDefinition);
                }
            }
        }

        internal static bool IsMatch(string word, string tokenString, bool prefixOnly)
        {
            // No suggestion if they match.
            if (word == tokenString)
            {
                return false;
            }

            // Simplify prefix matches. For CompletionSource, "token starts with word" this is the only check.
            if (tokenString.Length > word.Length && tokenString.StartsWith(word))
            {
                return true;
            }
            if (prefixOnly)
            {
                return false;
            }
            if (word.Length > tokenString.Length && word.StartsWith(tokenString))
            {
                return true;
            }

            // For SuggestedActions. Consider edit distance 1 only: Insertions, Deletions, and Transpositions
            // Insertions: removing one letter from 'word' forms 'tokenString' then seeing if it is equal or prefix.
            // Deletions: removing one letter from 'tokenString' forms 'word' then seeing if it is equal or prefix.
            // Transpositions: transpose two characters in 'word' then seeing if it is equal or prefix.
            int lhsPos = 0, rhsPos = 0;
            void resetPos() { lhsPos = 0; rhsPos = 0; }
            bool isInsertion() { resetPos(); return isOneDeletion(tokenString, word); }
            bool isDeletion() { resetPos(); return isOneDeletion(word, tokenString); }
            bool findNextDiff(string lhs, string rhs)
            {
                while (lhsPos < lhs.Length && rhsPos < rhs.Length)
                {
                    if (lhs[lhsPos] != rhs[rhsPos])
                    {
                        return true;
                    }
                    ++lhsPos;
                    ++rhsPos;
                }
                return false;
            }

            bool isOneDeletion(string lhs, string rhs)
            {
                if (!findNextDiff(lhs, rhs))
                {
                    return false;
                }
                ++rhsPos;
                return !findNextDiff(lhs, rhs);
            }

            // Apply the single edit distance then check for equal or prefix.
            if (isInsertion() || isDeletion())
            {
                return true;
            }

            // Now check for transposition.
            resetPos();
            if (!findNextDiff(word, tokenString)
                    || lhsPos > word.Length - 2 || rhsPos > tokenString.Length - 2
                    || word[lhsPos] != tokenString[rhsPos + 1] || word[lhsPos + 1] != tokenString[rhsPos])
            {
                return false;
            }
            lhsPos += 2;
            rhsPos += 2;
            return !findNextDiff(word, tokenString);
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
#endif
}
