// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Syntax;
using System.Linq;

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
            var node = VisitEventDeclaration(namespaceNode, machineNode, modSet, isExtern:false);
        }

        private EventDeclaration VisitEventDeclaration(NamespaceDeclaration namespaceNode, MachineDeclaration machineNode, ModifierSet modSet, bool isExtern)
        {
            // Lookup or Insert into (immediately) containing namespace or machine declaration.
            var declarations = (machineNode != null) ? machineNode.EventDeclarations : namespaceNode.EventDeclarations;
            var node = new EventDeclaration(base.TokenStream.Program, machineNode, modSet)
            {
                EventKeyword = base.TokenStream.Peek()
            };
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            node.Identifier = NameVisitor.VisitSimpleQualifiedName(base.TokenStream, TokenType.EventIdentifier);

            if (declarations.Find(node.Identifier.Text, out var existingDecl))
            {
                var details = existingDecl.IsExtern ? "declared \"extern\"" : "defined";
                throw new ParsingException($"Event {node.Identifier.Text} has already been {details} earlier in this file.",
                    new List<TokenType> { });
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Colon &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                // TODO: Create an overload of ParsingException ctor that generates the message with a predefined format enum.
                var expectedTokenTypes = new List<TokenType>
                {
                    TokenType.Assert,
                    TokenType.Assume,
                    TokenType.LeftAngleBracket,
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon,
                    TokenType.Colon
                };
                var itemsString = string.Join("\", \"", expectedTokenTypes.Select(l => TokenTypeRegistry.GetText(l)).ToArray());
                throw new ParsingException($"Expected one of: \"{itemsString}\".", expectedTokenTypes);
            }

            VisitGenericType(node);
            VisitBaseEventDeclaration(node, declarations);

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".",
                    new List<TokenType>
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
                if (isExtern)
                {
                    throw new ParsingException("\"extern\" cannot have an Assert or Assume specification.",
                        new List<TokenType> { });
                }
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
                    base.TokenStream.Peek().Type != TokenType.Identifier ||
                    !int.TryParse(base.TokenStream.Peek().TextUnit.Text, out int value))
                {
                    throw new ParsingException("Expected integer.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
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
                    throw new ParsingException("Expected \"(\" or \";\".",
                        new List<TokenType>
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
                        "    event e (a:int, b:bool)\n",
                        new List<TokenType>
                    {
                            TokenType.RightParenthesis
                    });
                }

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightParenthesis)
                {
                    throw new ParsingException("Expected \")\".",
                        new List<TokenType>
                    {
                            TokenType.RightParenthesis
                    });
                }

                node.RightParenthesis = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".",
                    new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            declarations.Add(node, isExtern);
            return node;
        }

        private void VisitBaseEventDeclaration(EventDeclaration node, EventDeclarations declarations)
        {
            if (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.Colon)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                if (base.TokenStream.Done || base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected event identifier.",
                        new List<TokenType> { TokenType.Identifier });
                }

                // We only use referencingNode to verify the name exists and the generic type matches.
                var referencingNode = new EventDeclaration(base.TokenStream.Program, null, ModifierSet.CreateDefault())
                {
                    Identifier = NameVisitor.VisitSimpleQualifiedName(base.TokenStream, TokenType.EventIdentifier)
                };

                if (!declarations.Find(referencingNode.Identifier.Text, out var baseEventDecl))
                {
                    throw new ParsingException($"Could not find definition or extern declaration of base event {referencingNode.Identifier.Text}.",
                        new List<TokenType> { });
                }

                if (!base.TokenStream.Done)
                {
                    VisitGenericType(referencingNode);
                    if (referencingNode.GenericType.Count != baseEventDecl.GenericType.Count)
                    {
                        throw new ParsingException($"Mismatch in number of generic type arguments for base event {referencingNode.Identifier.Text}.",
                            new List<TokenType> { });
                    }
                }
                node.BaseClassDecl = baseEventDecl;
            }
        }

        private void VisitGenericType(EventDeclaration node)
        {
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
                    throw new ParsingException("Expected generic type.",
                        new List<TokenType>
                        {
                            TokenType.Identifier
                        });
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
                        throw new ParsingException("Invalid generic expression.",
                            new List<TokenType>
                            {
                                TokenType.Identifier
                            });
                    }

                    node.GenericType.Add(base.TokenStream.Peek());
                    genericCount--;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.EventIdentifier));
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
                throw new ParsingException("Invalid generic expression.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }
        }

        internal void VisitExternDeclaration(NamespaceDeclaration namespaceNode, MachineDeclaration machineNode)
        {
            // Skip over "extern"
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.EventDecl)
            {
                throw new ParsingException("\"extern\" applies only to events and can have no access modifiers.",
                    new List<TokenType> { TokenType.EventDecl });
            }

            VisitEventDeclaration(namespaceNode, machineNode, ModifierSet.CreateDefault(), isExtern: true);
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
                throw new ParsingException("An event declared in the scope of a namespace cannot be private.",
                    new List<TokenType>());
            }

            if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("An event cannot be declared as protected.",
                    new List<TokenType>());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("An event cannot be declared as abstract.",
                    new List<TokenType>());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("An event cannot be declared as partial.",
                    new List<TokenType>());
            }
        }
    }
}
