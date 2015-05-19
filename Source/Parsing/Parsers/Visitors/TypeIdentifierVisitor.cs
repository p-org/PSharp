//-----------------------------------------------------------------------
// <copyright file="TypeIdentifierVisitor.cs">
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
    /// The P# type identifier parsing visitor.
    /// </summary>
    public sealed class TypeIdentifierVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public TypeIdentifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="node">Node</param>
        public void Visit(PTypeNode node)
        {
            if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis))
            {
                throw new ParsingException("Expected type.",
                    new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.Int,
                    TokenType.Bool,
                    TokenType.Seq,
                    TokenType.Map
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Bool)
            {
                node.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.Seq)
            {
                new SeqTypeIdentifierVisitor(base.TokenStream).Visit(node);
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                new TupleTypeIdentifierVisitor(base.TokenStream).Visit(node);
            }

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits the text unit.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        public void Visit(ref TextUnit textUnit)
        {
            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected type identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var position = base.TokenStream.Peek().TextUnit.Start;
            var line = base.TokenStream.Peek().TextUnit.Line;

            bool expectsDot = false;
            while (!base.TokenStream.Done)
            {
                if (!expectsDot &&
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl && 
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && base.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl || 
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    expectsDot = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot)
                {
                    expectsDot = false;
                }

                if (textUnit == null)
                {
                    textUnit = new TextUnit(base.TokenStream.Peek().TextUnit.Text,
                        line, position);
                }
                else
                {
                    textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                        line, position);
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                    line, position);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                int counter = 1;
                while (!base.TokenStream.Done)
                {
                    if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        counter++;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        counter--;
                    }

                    if (counter == 0 ||
                        (base.TokenStream.Peek().Type != TokenType.MachineDecl && 
                        base.TokenStream.Peek().Type != TokenType.Int &&
                        base.TokenStream.Peek().Type != TokenType.Bool &&
                        base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.Dot &&
                        base.TokenStream.Peek().Type != TokenType.Comma &&
                        base.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightAngleBracket))
                    {
                        break;
                    }

                    textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                        line, position);

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket)
                {
                    throw new ParsingException("Expected \">\".",
                        new List<TokenType>
                    {
                        TokenType.RightAngleBracket
                    });
                }

                textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                    line, position);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
        }
    }
}
