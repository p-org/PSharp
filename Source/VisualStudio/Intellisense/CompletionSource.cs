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

            this.CompletionList = new List<Completion>();

            foreach (var keyword in keywords)
            {
                this.CompletionList.Add(new Completion(keyword, keyword, keyword, null, null));
            }

            completionSets.Add(new CompletionSet(
                "Tokens",
                "Tokens",
                this.FindTokenSpanAtPosition(session.GetTriggerPoint(this.Buffer),
                    session),
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
        /// Returns the P# keywords.
        /// </summary>
        /// <returns>List of strings</returns>
        private List<string> GetKeywords()
        {
            var keywords = new List<string>();

            keywords.Add("private");
            keywords.Add("protected");
            keywords.Add("internal");
            keywords.Add("public");
            keywords.Add("abstract");
            keywords.Add("virtual");
            keywords.Add("override");

            keywords.Add("namespace");
            keywords.Add("using");

            keywords.Add("machine");
            keywords.Add("state");
            keywords.Add("event");
            keywords.Add("action");

            keywords.Add("on");
            keywords.Add("do");
            keywords.Add("goto");
            keywords.Add("defer");
            keywords.Add("ignore");
            keywords.Add("to");
            keywords.Add("entry");
            keywords.Add("exit");

            keywords.Add("this");
            keywords.Add("base");

            keywords.Add("new");
            keywords.Add("as");
            keywords.Add("for");
            keywords.Add("while");
            keywords.Add("do");
            keywords.Add("if");
            keywords.Add("else");
            keywords.Add("break");
            keywords.Add("continue");
            keywords.Add("return");

            keywords.Add("create");
            keywords.Add("send");
            keywords.Add("raise");
            keywords.Add("delete");
            keywords.Add("assert");
            keywords.Add("payload");

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
