//-----------------------------------------------------------------------
// <copyright file="PSharpErrorTagger.cs">
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
        ITextBuffer Buffer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">ITextBuffer</param>
        internal PSharpErrorTagger(ITextBuffer buffer)
        {
            this.Buffer = buffer;
        }

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

            var snapshot = this.Buffer.CurrentSnapshot; // TODO reduce the size on this; but do so without getting spurious errors
            if (GetExpectedTokenTypes(snapshot, out Token failingToken, out TokenType[] expectedTokenTypes))
            {
                var tokenSpan = failingToken != null
                    ? new SnapshotSpan(snapshot, new Span(failingToken.TextUnit.Start, failingToken.TextUnit.Length))
                    : new SnapshotSpan(snapshot, new Span(snapshot.Length - 1, 1));
                var actual = failingToken != null
                    ? TokenTypeRegistry.GetText(failingToken.Type)
                    : "EOF";
                var expected = string.Join(", ", expectedTokenTypes.Select(tt => TokenTypeRegistry.GetText(tt)));
                yield return new TagSpan<IErrorTag>(tokenSpan, new ErrorTag(actual, $"Unexpected token type {actual}; expected one of {expected}"));
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
