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

using Microsoft.PSharp.Parsing;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# completion source.
    /// </summary>
    internal class CompletionSource : ICompletionSource
    {
        private CompletionSourceProvider SourceProvider;
        private ITextBuffer Buffer;
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
            var keywords = this.GetKeywords();

            var snapshot = this.Buffer.CurrentSnapshot;
            var trackSpan = this.FindTokenSpanAtPosition(session.GetTriggerPoint(this.Buffer), session);
            var preSpan = new SnapshotSpan(snapshot, new Span(snapshot.GetLineFromLineNumber(0).Start,
                trackSpan.GetStartPoint(snapshot).Position));

            var tokens = new PSharpLexer().Tokenize(preSpan.GetText());
            var parser = new PSharpErrorParser();
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

        /// <summary>
        /// Returns the P# keywords.
        /// </summary>
        /// <returns>Dictionary of keywords</returns>
        private Dictionary<string, Tuple<string>> GetKeywords()
        {
            var keywords = new Dictionary<string, Tuple<string>>();
            
            keywords.Add("private", new Tuple<string>("private Keyword"));
            keywords.Add("protected", new Tuple<string>("protected Keyword"));
            keywords.Add("internal", new Tuple<string>("internal Keyword"));
            keywords.Add("public", new Tuple<string>("public Keyword"));
            keywords.Add("abstract", new Tuple<string>("abstract Keyword"));
            keywords.Add("virtual", new Tuple<string>("virtual Keyword"));
            keywords.Add("override", new Tuple<string>("override Keyword"));

            keywords.Add("namespace", new Tuple<string>("namespace Keyword"));
            keywords.Add("using", new Tuple<string>("using Keyword"));

            keywords.Add("machine", new Tuple<string>("machine Keyword"));
            keywords.Add("state", new Tuple<string>("state Keyword"));
            keywords.Add("event", new Tuple<string>("event Keyword"));
            keywords.Add("action", new Tuple<string>("action Keyword"));
            
            keywords.Add("on", new Tuple<string>("on Keyword"));
            keywords.Add("do", new Tuple<string>("do Keyword"));
            keywords.Add("goto", new Tuple<string>("goto Keyword"));
            keywords.Add("defer", new Tuple<string>("defer Keyword"));
            keywords.Add("ignore", new Tuple<string>("ignore Keyword"));
            keywords.Add("to", new Tuple<string>("to Keyword"));
            keywords.Add("entry", new Tuple<string>("entry Keyword"));
            keywords.Add("exit", new Tuple<string>("exit Keyword"));

            keywords.Add("this", new Tuple<string>("this Keyword"));
            keywords.Add("base", new Tuple<string>("base Keyword"));

            keywords.Add("new", new Tuple<string>("new Keyword"));
            keywords.Add("as", new Tuple<string>("as Keyword"));
            keywords.Add("for", new Tuple<string>("for Keyword"));
            keywords.Add("while", new Tuple<string>("while Keyword"));
            keywords.Add("if", new Tuple<string>("if Keyword"));
            keywords.Add("else", new Tuple<string>("else Keyword"));
            keywords.Add("break", new Tuple<string>("break Keyword"));
            keywords.Add("continue", new Tuple<string>("continue Keyword"));
            keywords.Add("return", new Tuple<string>("return Keyword"));

            keywords.Add("create", new Tuple<string>("create Keyword"));
            keywords.Add("send", new Tuple<string>("send Keyword"));
            keywords.Add("raise", new Tuple<string>("raise Keyword"));
            keywords.Add("delete", new Tuple<string>("delete Keyword"));
            keywords.Add("assert", new Tuple<string>("assert Keyword"));
            keywords.Add("payload", new Tuple<string>("payload Keyword"));

            return keywords;
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
