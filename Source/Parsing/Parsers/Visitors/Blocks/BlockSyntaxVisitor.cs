//-----------------------------------------------------------------------
// <copyright file="BlockSyntaxVisitor.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# block syntax parsing visitor.
    /// </summary>
    internal sealed class BlockSyntaxVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal BlockSyntaxVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(BlockSyntax node)
        {
            node.OpenBraceToken = base.TokenStream.Peek();

            var text = "";

            int counter = 0;
            while (!base.TokenStream.Done)
            {
                text += base.TokenStream.Peek().TextUnit.Text;

                if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightCurlyBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                base.TokenStream.Index++;
            }

            node.Block = CSharpSyntaxTree.ParseText(text);
        }
    }
}
