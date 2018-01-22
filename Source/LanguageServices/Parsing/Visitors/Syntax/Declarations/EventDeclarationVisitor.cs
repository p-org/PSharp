//-----------------------------------------------------------------------
// <copyright file="EventDeclarationVisitor.cs">
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

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# event declaration parsing visitor.
    /// </summary>
    internal sealed class EventDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal EventDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="namespaceNode">Containing namespace</param>
        /// <param name="machineNode">Containing machine</param>
        /// <param name="modSet">Modifier set</param>
        internal void Visit(NamespaceDeclaration namespaceNode, MachineDeclaration machineNode, ModifierSet modSet)
        {
            this.CheckEventModifierSet(modSet, (machineNode != null));

            var node = new EventDeclaration(base.TokenStream.Program, machineNode, modSet);
            node.EventKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.Identifier,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
                    });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(TokenType.EventIdentifier);
            }

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.Assert,
                        TokenType.Assume,
                        TokenType.LeftParenthesis,
                        TokenType.Semicolon
                    });
            }
            
            int genericCount = 0;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.Comma &&
                    base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket &&
                    base.TokenStream.Peek().Type != TokenType.Object &&
                    base.TokenStream.Peek().Type != TokenType.String &&
                    base.TokenStream.Peek().Type != TokenType.Sbyte &&
                    base.TokenStream.Peek().Type != TokenType.Byte &&
                    base.TokenStream.Peek().Type != TokenType.Short &&
                    base.TokenStream.Peek().Type != TokenType.Ushort &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Uint &&
                    base.TokenStream.Peek().Type != TokenType.Long &&
                    base.TokenStream.Peek().Type != TokenType.Ulong &&
                    base.TokenStream.Peek().Type != TokenType.Char &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Decimal &&
                    base.TokenStream.Peek().Type != TokenType.Float &&
                    base.TokenStream.Peek().Type != TokenType.Double)
                {
                    break;
                }

                if (genericCount == 0 &&
                    base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    throw new ParsingException("Expected generic type.", base.TokenStream.Peek(),
                        TokenType.Identifier);
                }
                else if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                {
                    node.GenericType.Add(base.TokenStream.Peek());
                    genericCount++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                {
                    if (genericCount == 0)
                    {
                        throw new ParsingException("Invalid generic expression.", base.TokenStream.Peek(),
                            TokenType.Identifier);
                    }

                    node.GenericType.Add(base.TokenStream.Peek());
                    genericCount--;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(TokenType.EventIdentifier);
                    node.GenericType.Add(base.TokenStream.Peek());
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot ||
                    base.TokenStream.Peek().Type == TokenType.Comma ||
                    base.TokenStream.Peek().Type == TokenType.Object ||
                    base.TokenStream.Peek().Type == TokenType.String ||
                    base.TokenStream.Peek().Type == TokenType.Sbyte ||
                    base.TokenStream.Peek().Type == TokenType.Byte ||
                    base.TokenStream.Peek().Type == TokenType.Short ||
                    base.TokenStream.Peek().Type == TokenType.Ushort ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Uint ||
                    base.TokenStream.Peek().Type == TokenType.Long ||
                    base.TokenStream.Peek().Type == TokenType.Ulong ||
                    base.TokenStream.Peek().Type == TokenType.Char ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Decimal ||
                    base.TokenStream.Peek().Type == TokenType.Float ||
                    base.TokenStream.Peek().Type == TokenType.Double)
                {
                    node.GenericType.Add(base.TokenStream.Peek());
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (genericCount > 0)
            {
                throw new ParsingException("Invalid generic expression.", base.TokenStream.Peek(),
                    TokenType.Identifier);
            }
            
            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.Assert,
                        TokenType.Assume,
                        TokenType.LeftParenthesis,
                        TokenType.Semicolon
                    });
            }

            if (base.TokenStream.Peek().Type == TokenType.Assert ||
                base.TokenStream.Peek().Type == TokenType.Assume)
            {
                bool isAssert = true;
                if (base.TokenStream.Peek().Type == TokenType.Assert)
                {
                    node.AssertKeyword = base.TokenStream.Peek();
                }
                else
                {
                    node.AssumeKeyword = base.TokenStream.Peek();
                    isAssert = false;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected integer.", base.TokenStream.Peek(),
                        TokenType.Identifier);
                }

                int value;
                if (!int.TryParse(base.TokenStream.Peek().TextUnit.Text, out value))
                {
                    throw new ParsingException("Expected integer.", base.TokenStream.Peek(),
                        TokenType.Identifier);
                }

                if (isAssert)
                {
                    node.AssertValue = value;
                }
                else
                {
                    node.AssumeValue = value;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    throw new ParsingException("Expected \"(\" or \";\".", base.TokenStream.Peek(),
                        new []
                        {
                            TokenType.LeftParenthesis,
                            TokenType.Semicolon
                        });
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesis = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                bool isType = false;
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.RightParenthesis)
                {
                    if (isType &&
                        base.TokenStream.Peek().Type != TokenType.Colon &&
                        base.TokenStream.Peek().Type != TokenType.Comma)
                    {
                        TextUnit textUnit = null;
                        new TypeIdentifierVisitor(base.TokenStream).Visit(ref textUnit);
                        var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);
                        node.PayloadTypes.Add(typeIdentifier);
                    }
                    else if (base.TokenStream.Peek().Type != TokenType.Colon &&
                        base.TokenStream.Peek().Type != TokenType.Comma)
                    {
                        node.PayloadIdentifiers.Add(base.TokenStream.Peek());

                        isType = true;
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }

                    if (!base.TokenStream.Done)
                    {
                        if (base.TokenStream.Peek().Type == TokenType.Comma)
                        {
                            isType = false;
                            base.TokenStream.Index++;
                            base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        }
                        else if (base.TokenStream.Peek().Type == TokenType.Colon)
                        {
                            base.TokenStream.Index++;
                            base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        }
                    }
                }

                if (node.PayloadIdentifiers.Count != node.PayloadTypes.Count)
                {
                    throw new ParsingException("The payload type of event '" + node.Identifier.TextUnit.Text +
                        "' was not declared correctly.\n" +
                        "  You must declare both a type and a name identifier, for example:\n\n" +
                        "    event e (a:int, b:bool)\n", base.TokenStream.Peek(),
                        TokenType.RightParenthesis);
                }

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightParenthesis)
                {
                    throw new ParsingException("Expected \")\".", base.TokenStream.Peek(),
                        TokenType.RightParenthesis);
                }

                node.RightParenthesis = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".", base.TokenStream.Peek(),
                    TokenType.Semicolon);
            }

            node.SemicolonToken = base.TokenStream.Peek();

            // Insert into (immediately) containing namespace or machine declaration.
            if (machineNode != null)
            {
                machineNode.EventDeclarations.Add(node);
            }
            else
            {
                namespaceNode.EventDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        /// <param name="isInMachine">Is declared inside a machine</param>
        private void CheckEventModifierSet(ModifierSet modSet, bool isInMachine)
        {
            if (!isInMachine && modSet.AccessModifier == AccessModifier.Private)
            {
                throw new ParsingException("An event declared in the scope of a namespace cannot be private.", base.TokenStream.Peek());
            }

            if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("An event cannot be declared as protected.", base.TokenStream.Peek());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("An event cannot be declared as abstract.", base.TokenStream.Peek());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("An event cannot be declared as partial.", base.TokenStream.Peek());
            }
        }
    }
}
