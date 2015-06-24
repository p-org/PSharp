//-----------------------------------------------------------------------
// <copyright file="StateExitDeclarationVisitor.cs">
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

using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# state exit declaration parsing visitor.
    /// </summary>
    internal sealed class StateExitDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StateExitDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(StateDeclarationNode parentNode)
        {
            var node = new ExitDeclarationNode(base.TokenStream.Program, parentNode.IsModel);
            node.ExitKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".",
                    new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new StatementBlockNode(base.TokenStream.Program, parentNode.Machine,
                parentNode, node.IsModel);
            new StatementBlockVisitor(base.TokenStream).Visit(blockNode);
            node.StatementBlock = blockNode;

            parentNode.ExitDeclaration = node;
        }
    }
}
