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

using Microsoft.PSharp.Parsing;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# indentation functionality.
    /// </summary>
    internal sealed class Indent : ISmartIndent
    {
        private readonly ITextView TextView;
        private bool IsDisposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textView">ITextView</param>
        public Indent(ITextView textView)
        {
            this.TextView = textView;
            this.IsDisposed = false;
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            return this.GetLineIndentation(line);
        }

        internal int GetLineIndentation(ITextSnapshotLine line)
        {
            var options = this.TextView.Options;
            var tabSize = options.GetIndentSize();

            var indent = 0;
            if (line.LineNumber == 0)
            {
                return indent;
            }

            var currentLine = line;

            do
            {
                currentLine = line.Snapshot.GetLineFromLineNumber(currentLine.LineNumber - 1);
            }
            while (currentLine.LineNumber > 0 && currentLine.Length == 0);
            if (line.LineNumber == 0)
            {
                return indent;
            }

            var tokens = new PSharpLexer().Tokenize(currentLine.GetText());

            bool codeFound = false;
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.WhiteSpace && !codeFound)
                {
                    foreach (var c in token.Text)
                    {
                        if (c == '\t')
                        {
                            indent += tabSize;
                        }
                        else
                        {
                            indent++;
                        }
                    }
                }
                else if (token.Type == TokenType.LeftCurlyBracket ||
                    token.Type == TokenType.MachineLeftCurlyBracket ||
                    token.Type == TokenType.StateLeftCurlyBracket)
                {
                    indent += tabSize;
                    break;
                }
                else if (!codeFound)
                {
                    codeFound = true;
                }
            }

            if (indent < 0)
            {
                indent = 0;
            }

            return indent;
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
