//-----------------------------------------------------------------------
// <copyright file="CompletionSource.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.PSharp.LanguageServices;

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
        private HashSet<TokenType> DoNotSuggest = new HashSet<TokenType>
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
            this.CompletionList = this.RefineAvailableKeywords(availableKeywords, prefix)
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
        private IEnumerable<KeyValuePair<string, string>> RefineAvailableKeywords(IEnumerable<TokenType> expectedTokenTypes, string prefix)
        {
            foreach (var tokenType in expectedTokenTypes.Where(tokType => !DoNotSuggest.Contains(tokType)))
            {
                var tokenString = TokenTypeRegistry.GetText(tokenType);
                if (tokenString.Length > prefix.Length && tokenString.StartsWith(prefix)
                        && Keywords.DefinitionMap.TryGetValue(tokenString, out string initialDef))
                {
                    // TODO: Consolidate the various lists and dictionaries.
                    var tokenDefinition = PSharpQuickInfoSource.TokenTypeTips.TryGetValue(tokenType, out string betterDef) ? betterDef : initialDef;
                    yield return new KeyValuePair<string, string>(tokenString, tokenDefinition);
                }
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
