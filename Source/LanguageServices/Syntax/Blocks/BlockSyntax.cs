//-----------------------------------------------------------------------
// <copyright file="BlockSyntax.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Block syntax node.
    /// </summary>
    internal sealed class BlockSyntax : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

        /// <summary>
        /// The state parent node.
        /// </summary>
        internal readonly StateDeclaration State;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal SyntaxTree Block;

        /// <summary>
        /// The open brace token.
        /// </summary>
        internal Token OpenBraceToken;

        /// <summary>
        /// The close brace token.
        /// </summary>
        internal Token CloseBraceToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="stateNode">StateDeclarationNode</param>
        internal BlockSyntax(IPSharpProgram program, MachineDeclaration machineNode,
            StateDeclaration stateNode)
            : base(program)
        {
            this.Machine = machineNode;
            this.State = stateNode;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            if (base.Configuration.ForVsLanguageService)
            {
                // Do not change formatting
                base.TextUnit = this.OpenBraceToken.TextUnit.WithText(this.Block.ToString());
                return;
            }

            // Adjust the indent of lines in the block to match the surrounding indentation, according to
            // the line in the block with the minimum indentation.
            var lines = this.Block.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var splitLines = SplitAndNormalizeLeadingWhitespace(lines).ToArray();

            // Ignore the open and close braces as they are indented one level higher and the parsing leaves
            // the first one with no indent. 'indentLevel' is the level of the block's open/close braces.
            // This has to handle code being on the same line as the open and/or close brackets; this is
            // preserved, so in that case the result would also be on a single line (e.g. "{ }", as opposed
            // to an empty machine or state definition which would put the empty braces on separate lines).
            var skipFirst = splitLines.First().Item2.StartsWith("{") ? 1 : 0;
            var skipLast = splitLines.Last().Item2.StartsWith("}") ? 1 : 0;
            var midLines = splitLines.Skip(skipFirst).Take(splitLines.Length - skipFirst - skipLast).ToArray();

            // If there are no lines between {} or they are all empty or whitespace, generate empty brackets.
            var minLeadingWsLen = (skipFirst + skipLast == splitLines.Length)
                ? 0
                : midLines.Where(s => s.Item2.Length > 0).Select(s => s.Item1.Length).DefaultIfEmpty(0).Min();

            // Adjust line indents to the proper level.
            var numIndentSpaces = (indentLevel + 1) * SpacesPerIndent - minLeadingWsLen;
            var indent = GetIndent(indentLevel);
            var sb = new StringBuilder();
            if (skipFirst > 0)
            {
                sb.Append(indent).Append(splitLines.First().Item2);
            }
            if (midLines.Length > 0)
            {
                sb.Append("\n").Append(string.Join("\n", ComposeLines(numIndentSpaces, midLines)));
            }
            if (skipLast > 0)
            {
                sb.Append("\n").Append(indent).Append(splitLines.Last().Item2);
            }
            base.TextUnit = this.OpenBraceToken.TextUnit.WithText(sb.ToString());
        }

        #endregion

        #region private methods

        private IEnumerable<Tuple<string, string>> SplitAndNormalizeLeadingWhitespace(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                // All-blank lines may have a misleadingly small number of spaces (if not 0)
                if (string.IsNullOrWhiteSpace(line))
                {
                    yield return Tuple.Create(string.Empty, string.Empty);
                }
                else
                {
                    // Normalize leading whitespace to spaces instead of tabs. This won't be perfect if there is an uneven mix of the two.
                    var leadingWsLen = line.TakeWhile(char.IsWhiteSpace).Count();
                    yield return Tuple.Create(line.Substring(0, leadingWsLen).Replace("\t", OneIndent), line.Substring(leadingWsLen).Trim());
                }
            }
        }

        private IEnumerable<string> ComposeLines(int numIndentSpaces, IEnumerable<Tuple<string, string>> splitLines)
        {
            Func<string, string> adjustIndent;
            if (numIndentSpaces < 0)
            {
                adjustIndent = ws => ws.Substring(-numIndentSpaces);
            }
            else
            {
                var indent = new string(' ', numIndentSpaces);
                adjustIndent = ws => indent + ws;
            }
            return splitLines.Select(line => line.Item2.Length == 0 ? string.Empty : adjustIndent(line.Item1) + line.Item2);
        }

        #endregion
    }
}
