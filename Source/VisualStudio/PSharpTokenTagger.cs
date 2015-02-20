//-----------------------------------------------------------------------
// <copyright file="PSharpTokenTagger.cs">
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using Microsoft.PSharp.Parsing;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# token tagger.
    /// </summary>
    internal sealed class PSharpTokenTagger : ITagger<PSharpTokenTag>
    {
        ITextBuffer Buffer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">ITextBuffer</param>
        internal PSharpTokenTagger(ITextBuffer buffer)
        {
            this.Buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITagSpan<PSharpTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            foreach (var currSpan in spans)
            {
                var containingLine = currSpan.Start.GetContainingLine();
                var currLoc = containingLine.Start.Position;

                var tokens = new PSharpLexer().Tokenize(containingLine.GetText());
                foreach (var token in tokens)
                {
                    var tokenSpan = new SnapshotSpan(currSpan.Snapshot, new Span(currLoc, token.Text.Length));
                    if (tokenSpan.IntersectsWith(currSpan))
                    {
                        yield return new TagSpan<PSharpTokenTag>(tokenSpan, new PSharpTokenTag(token.Type));
                    }

                    currLoc += token.Text.Length;
                }
            }
        }
    }
}
