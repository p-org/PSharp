//-----------------------------------------------------------------------
// <copyright file="MachineDeclarationVisitor.cs">
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

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# machine declaration parsing visitor.
    /// </summary>
    internal sealed class MachineDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MachineDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="parentNode">Node</param>
        /// <param name="isMonitor">Is a monitor</param>
        /// <param name="isPartial">Is partial</param>
        /// <param name="accMod">Access modifier</param>
        /// <param name="inhMod">Inheritance modifier</param>
        internal void Visit(IPSharpProgram program, NamespaceDeclaration parentNode, bool isMonitor,
            bool isPartial, AccessModifier accMod, InheritanceModifier inhMod)
        {
            var node = new MachineDeclaration(base.TokenStream.Program, isMonitor, isPartial);
            node.AccessModifier = accMod;
            node.InheritanceModifier = inhMod;
            node.MachineKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected machine identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.CurrentMachine = base.TokenStream.Peek().Text;
            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Program is PSharpProgram)
            {
                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Colon &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected \":\" or \"{\".",
                        new List<TokenType>
                    {
                            TokenType.Colon,
                            TokenType.LeftCurlyBracket
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Colon)
                {
                    node.ColonToken = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    if (base.TokenStream.Done ||
                        base.TokenStream.Peek().Type != TokenType.Identifier)
                    {
                        throw new ParsingException("Expected base machine identifier.",
                            new List<TokenType>
                        {
                                TokenType.Identifier
                        });
                    }

                    while (!base.TokenStream.Done &&
                        base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
                    {
                        if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                            base.TokenStream.Peek().Type != TokenType.Dot &&
                            base.TokenStream.Peek().Type != TokenType.NewLine)
                        {
                            throw new ParsingException("Expected base machine identifier.",
                                new List<TokenType>
                            {
                                    TokenType.Identifier,
                                    TokenType.Dot
                            });
                        }
                        else
                        {
                            node.BaseNameTokens.Add(base.TokenStream.Peek());
                        }

                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".",
                    new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextPSharpIntraMachineDeclaration(node);
            parentNode.MachineDeclarations.Add(node);

            if (node.StateDeclarations.Count == 0 && node.BaseNameTokens.Count == 0)
            {
                throw new ParsingException("A machine must declare at least one state.",
                    new List<TokenType>());
            }

            var startStates = node.StateDeclarations.FindAll(s => s.IsStart);
            if (startStates.Count == 0 && node.BaseNameTokens.Count == 0)
            {
                throw new ParsingException("A machine must declare a start state.",
                    new List<TokenType>());
            }
            else if (startStates.Count > 1)
            {
                throw new ParsingException("A machine can declare only a single start state.",
                    new List<TokenType>());
            }
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpIntraMachineDeclaration(MachineDeclaration node)
        {
            bool fixpoint = false;
            while (!fixpoint)
            {
                var token = base.TokenStream.Peek();
                switch (token.Type)
                {
                    case TokenType.WhiteSpace:
                    case TokenType.Comment:
                    case TokenType.NewLine:
                        base.TokenStream.Index++;
                        break;

                    case TokenType.CommentLine:
                    case TokenType.Region:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.CommentStart:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.StartState:
                    case TokenType.HotState:
                    case TokenType.ColdState:
                    case TokenType.StateDecl:
                    case TokenType.StateGroupDecl:
                    case TokenType.Void:
                    case TokenType.MachineDecl:
                    case TokenType.Object:
                    case TokenType.String:
                    case TokenType.Sbyte:
                    case TokenType.Byte:
                    case TokenType.Short:
                    case TokenType.Ushort:
                    case TokenType.Int:
                    case TokenType.Uint:
                    case TokenType.Long:
                    case TokenType.Ulong:
                    case TokenType.Char:
                    case TokenType.Bool:
                    case TokenType.Decimal:
                    case TokenType.Float:
                    case TokenType.Double:
                    case TokenType.Identifier:
                    case TokenType.Private:
                    case TokenType.Protected:
                    case TokenType.Internal:
                    case TokenType.Public:
                    case TokenType.Async:
                    case TokenType.Partial:
                        this.VisitMachineLevelDeclaration(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.LeftSquareBracket:
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        new AttributeListVisitor(base.TokenStream).Visit();
                        base.TokenStream.Index++;
                        break;

                    case TokenType.RightCurlyBracket:
                        base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                            TokenType.MachineRightCurlyBracket));
                        node.RightCurlyBracketToken = base.TokenStream.Peek();
                        base.TokenStream.CurrentMachine = "";
                        fixpoint = true;
                        break;

                    default:
                        throw new ParsingException("Unexpected token '" + base.TokenStream.Peek().TextUnit.Text + "'.",
                            new List<TokenType>());
                }

                if (base.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".",
                        new List<TokenType>
                    {
                            TokenType.Private,
                            TokenType.Protected,
                            TokenType.StartState,
                            TokenType.HotState,
                            TokenType.ColdState,
                            TokenType.StateDecl,
                            TokenType.StateGroupDecl,
                            TokenType.LeftSquareBracket,
                            TokenType.RightCurlyBracket
                    });
                }
            }
        }

        /// <summary>
        /// Visits a machine level declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMachineLevelDeclaration(MachineDeclaration parentNode)
        {
            AccessModifier am = AccessModifier.None;
            InheritanceModifier im = InheritanceModifier.None;
            bool isStart = false;
            bool isHot = false;
            bool isCold = false;
            bool isAsync = false;
            bool isPartial = false;

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.StateGroupDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Void &&
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
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                if (am != AccessModifier.None &&
                    (base.TokenStream.Peek().Type == TokenType.Public ||
                    base.TokenStream.Peek().Type == TokenType.Private ||
                    base.TokenStream.Peek().Type == TokenType.Protected ||
                    base.TokenStream.Peek().Type == TokenType.Internal))
                {
                    throw new ParsingException("More than one protection modifier.",
                        new List<TokenType>());
                }
                else if (im != InheritanceModifier.None &&
                    base.TokenStream.Peek().Type == TokenType.Abstract)
                {
                    throw new ParsingException("Duplicate abstract modifier.",
                        new List<TokenType>());
                }
                else if (isStart &&
                    base.TokenStream.Peek().Type == TokenType.StartState)
                {
                    throw new ParsingException("Duplicate start state modifier.",
                        new List<TokenType>());
                }
                else if (isHot &&
                    base.TokenStream.Peek().Type == TokenType.HotState)
                {
                    throw new ParsingException("Duplicate hot state modifier.",
                        new List<TokenType>());
                }
                else if (isCold &&
                    base.TokenStream.Peek().Type == TokenType.ColdState)
                {
                    throw new ParsingException("Duplicate cold state modifier.",
                        new List<TokenType>());
                }
                else if ((isCold &&
                    base.TokenStream.Peek().Type == TokenType.HotState) ||
                    (isHot &&
                    base.TokenStream.Peek().Type == TokenType.ColdState))
                {
                    throw new ParsingException("State cannot be both hot and cold.",
                        new List<TokenType>());
                }
                else if (isAsync &&
                    base.TokenStream.Peek().Type == TokenType.Async)
                {
                    throw new ParsingException("Duplicate async method modifier.",
                        new List<TokenType>());
                }
                else if (isPartial &&
                    base.TokenStream.Peek().Type == TokenType.Partial)
                {
                    throw new ParsingException("Duplicate partial method modifier.",
                        new List<TokenType>());
                }

                if (base.TokenStream.Peek().Type == TokenType.Public)
                {
                    am = AccessModifier.Public;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Private)
                {
                    am = AccessModifier.Private;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Protected)
                {
                    am = AccessModifier.Protected;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Internal)
                {
                    am = AccessModifier.Internal;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Abstract)
                {
                    im = InheritanceModifier.Abstract;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Virtual)
                {
                    im = InheritanceModifier.Virtual;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Override)
                {
                    im = InheritanceModifier.Override;
                }
                else if (base.TokenStream.Peek().Type == TokenType.StartState)
                {
                    isStart = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.HotState)
                {
                    isHot = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.ColdState)
                {
                    isCold = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Async)
                {
                    isAsync = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Partial)
                {
                    isPartial = true;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.StateGroupDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Void &&
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
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected state or method declaration.",
                    new List<TokenType>
                {
                    TokenType.StateDecl,
                    TokenType.StateGroupDecl,
                    TokenType.MachineDecl,
                    TokenType.Void,
                    TokenType.Object,
                    TokenType.String,
                    TokenType.Sbyte,
                    TokenType.Byte,
                    TokenType.Short,
                    TokenType.Ushort,
                    TokenType.Int,
                    TokenType.Uint,
                    TokenType.Long,
                    TokenType.Ulong,
                    TokenType.Char,
                    TokenType.Bool,
                    TokenType.Decimal,
                    TokenType.Float,
                    TokenType.Double,
                    TokenType.Identifier
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                if (am == AccessModifier.Public)
                {
                    throw new ParsingException("A state cannot be public.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Internal)
                {
                    throw new ParsingException("A state cannot be internal.",
                        new List<TokenType>());
                }

                if (im == InheritanceModifier.Abstract)
                {
                    throw new ParsingException("A state cannot be abstract.",
                        new List<TokenType>());
                }
                else if (im == InheritanceModifier.Virtual)
                {
                    throw new ParsingException("A state cannot be virtual.",
                        new List<TokenType>());
                }
                else if (im == InheritanceModifier.Override)
                {
                    throw new ParsingException("A state cannot be overriden.",
                        new List<TokenType>());
                }

                if (isAsync)
                {
                    throw new ParsingException("A state cannot be async.",
                        new List<TokenType>());
                }

                if (isPartial)
                {
                    throw new ParsingException("A state cannot be partial.",
                        new List<TokenType>());
                }

                new StateDeclarationVisitor(base.TokenStream).Visit(parentNode, isStart, isHot, isCold, am);
            }
            else
            {
                if (am == AccessModifier.Public)
                {
                    throw new ParsingException("A field or method cannot be public.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Internal)
                {
                    throw new ParsingException("A field or method cannot be internal.",
                        new List<TokenType>());
                }

                new FieldOrMethodDeclarationVisitor(base.TokenStream).Visit(parentNode, am, im, isAsync, isPartial);
            }
        }
    }
}
