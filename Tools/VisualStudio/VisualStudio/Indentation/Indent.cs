//-----------------------------------------------------------------------
// <copyright file="Indent.cs">
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using Microsoft.PSharp.LanguageServices.Parsing;
using System.Linq;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# indentation functionality.
    /// </summary>
    internal sealed class Indent : ISmartIndent
    {
        private readonly ITextView textView;
        private bool isDisposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textView">ITextView</param>
        public Indent(ITextView textView)
        {
            this.textView = textView;
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            return this.GetLineIndentation(line);
        }

        internal int GetLineIndentation(ITextSnapshotLine line)
        {
            var options = this.textView.Options;
            var tabSize = options.GetIndentSize();

            var indent = 0;
            var currentLine = line.LineNumber == 0 ? line : line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
            while (currentLine.LineNumber > 0 && currentLine.GetText().Trim().Length == 0)
            {
                currentLine = line.Snapshot.GetLineFromLineNumber(currentLine.LineNumber - 1);
            }
            if (currentLine.LineNumber == 0)
            {
                return indent;
            }

            bool codeFound = false;
            bool openBracketFound = false;
            var tokens = new PSharpLexer().Tokenize(currentLine.GetText());
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.WhiteSpace && !codeFound)
                {
                    foreach (var c in token.Text)
                    {
                        indent += (c == '\t') ? tabSize : 1;
                    }
                    continue;
                }
                if (token.Type == TokenType.LeftCurlyBracket ||
                    token.Type == TokenType.MachineLeftCurlyBracket ||
                    token.Type == TokenType.StateLeftCurlyBracket)
                {
                    openBracketFound = true;
                    break;
                }
                codeFound = true;
            }

            if (openBracketFound)
            {
                // Don't indent for the openBracket of the preceding line if the first nonblank character
                // on the current line is the corresponding close bracket.
                var token = new PSharpLexer().Tokenize(line.GetText().Trim()).FirstOrDefault();
                if (token == null
                    || (token.Type != TokenType.RightCurlyBracket &&
                        token.Type != TokenType.MachineRightCurlyBracket &&
                        token.Type != TokenType.StateRightCurlyBracket))
                {
                    indent += tabSize;
                }
            }

            return indent < 0 ? 0 : indent;
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
