// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# event declaration parsing visitor.
    /// </summary>
    internal sealed class EventDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDeclarationVisitor"/> class.
        /// </summary>
        internal EventDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(NamespaceDeclaration namespaceNode, MachineDeclaration machineNode, ModifierSet modSet)
        {
            CheckEventModifierSet(modSet, machineNode != null);
            var node = this.VisitEventDeclaration(namespaceNode, machineNode, modSet, isExtern: false);
        }

        private EventDeclaration VisitEventDeclaration(NamespaceDeclaration namespaceNode, MachineDeclaration machineNode, ModifierSet modSet, bool isExtern)
        {
            // Lookup or Insert into (immediately) containing namespace or machine declaration.
            var declarations = (machineNode != null) ? machineNode.EventDeclarations : namespaceNode.EventDeclarations;
            var node = new EventDeclaration(this.TokenStream.Program, machineNode, modSet)
            {
                EventKeyword = this.TokenStream.Peek()
            };
            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.Identifier &&
                this.TokenStream.Peek().Type != TokenType.HaltEvent &&
                this.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.", TokenType.Identifier, TokenType.HaltEvent, TokenType.DefaultEvent);
            }

            node.Identifier = NameVisitor.VisitSimpleQualifiedName(this.TokenStream, TokenType.EventIdentifier);

            if (declarations.Find(node.Identifier.Text, out var existingDecl))
            {
                var details = existingDecl.IsExtern ? "declared \"extern\"" : "defined";
                throw new ParsingException($"Event {node.Identifier.Text} has already been {details} earlier in this file.");
            }

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.Assert &&
                this.TokenStream.Peek().Type != TokenType.Assume &&
                this.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                this.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                this.TokenStream.Peek().Type != TokenType.Colon &&
                this.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                // TODO: Create an overload of ParsingException ctor that generates the message with a predefined format enum.
                var expectedTokenTypes = new TokenType[]
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

            this.VisitGenericType(node);
            this.VisitBaseEventDeclaration(node, declarations);

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.Assert &&
                this.TokenStream.Peek().Type != TokenType.Assume &&
                this.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                this.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException(
                    "Expected \"(\" or \";\".",
                    TokenType.Assert,
                    TokenType.Assume,
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon);
            }

            if (this.TokenStream.Peek().Type == TokenType.Assert ||
                this.TokenStream.Peek().Type == TokenType.Assume)
            {
                if (isExtern)
                {
                    throw new ParsingException("\"extern\" cannot have an Assert or Assume specification.");
                }

                bool isAssert = true;
                if (this.TokenStream.Peek().Type == TokenType.Assert)
                {
                    node.AssertKeyword = this.TokenStream.Peek();
                }
                else
                {
                    node.AssumeKeyword = this.TokenStream.Peek();
                    isAssert = false;
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (this.TokenStream.Done ||
                    this.TokenStream.Peek().Type != TokenType.Identifier ||
                    !int.TryParse(this.TokenStream.Peek().TextUnit.Text, out int value))
                {
                    throw new ParsingException("Expected integer.", TokenType.Identifier);
                }

                if (isAssert)
                {
                    node.AssertValue = value;
                }
                else
                {
                    node.AssumeValue = value;
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (this.TokenStream.Done ||
                    (this.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    this.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    throw new ParsingException("Expected \"(\" or \";\".", TokenType.LeftParenthesis, TokenType.Semicolon);
                }
            }

            if (this.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesis = this.TokenStream.Peek();

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                bool isType = false;
                while (!this.TokenStream.Done &&
                    this.TokenStream.Peek().Type != TokenType.RightParenthesis)
                {
                    if (isType &&
                        this.TokenStream.Peek().Type != TokenType.Colon &&
                        this.TokenStream.Peek().Type != TokenType.Comma)
                    {
                        TextUnit textUnit = null;
                        new TypeIdentifierVisitor(this.TokenStream).Visit(ref textUnit);
                        var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);
                        node.PayloadTypes.Add(typeIdentifier);
                    }
                    else if (this.TokenStream.Peek().Type != TokenType.Colon &&
                        this.TokenStream.Peek().Type != TokenType.Comma)
                    {
                        node.PayloadIdentifiers.Add(this.TokenStream.Peek());

                        isType = true;
                        this.TokenStream.Index++;
                        this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }

                    if (!this.TokenStream.Done)
                    {
                        if (this.TokenStream.Peek().Type == TokenType.Comma)
                        {
                            isType = false;
                            this.TokenStream.Index++;
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        }
                        else if (this.TokenStream.Peek().Type == TokenType.Colon)
                        {
                            this.TokenStream.Index++;
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        }
                    }
                }

                if (node.PayloadIdentifiers.Count != node.PayloadTypes.Count)
                {
                    string error = $"The payload type of event '{node.Identifier.TextUnit.Text}' was not declared correctly.\n" +
                        "  You must declare both a type and a name identifier, for example:\n\n" +
                        "    event e (a:int, b:bool)\n";
                    throw new ParsingException(error, TokenType.RightParenthesis);
                }

                if (this.TokenStream.Done ||
                    this.TokenStream.Peek().Type != TokenType.RightParenthesis)
                {
                    throw new ParsingException("Expected \")\".", TokenType.RightParenthesis);
                }

                node.RightParenthesis = this.TokenStream.Peek();

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".", TokenType.Semicolon);
            }

            node.SemicolonToken = this.TokenStream.Peek();
            declarations.Add(node, isExtern);
            return node;
        }

        private void VisitBaseEventDeclaration(EventDeclaration node, EventDeclarations declarations)
        {
            if (!this.TokenStream.Done && this.TokenStream.Peek().Type == TokenType.Colon)
            {
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                if (this.TokenStream.Done || this.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected event identifier.", TokenType.Identifier);
                }

                // We only use referencingNode to verify the name exists and the generic type matches.
                var referencingNode = new EventDeclaration(this.TokenStream.Program, null, ModifierSet.CreateDefault())
                {
                    Identifier = NameVisitor.VisitSimpleQualifiedName(this.TokenStream, TokenType.EventIdentifier)
                };

                if (!declarations.Find(referencingNode.Identifier.Text, out var baseEventDecl))
                {
                    throw new ParsingException($"Could not find definition or extern declaration of base event {referencingNode.Identifier.Text}.");
                }

                if (!this.TokenStream.Done)
                {
                    this.VisitGenericType(referencingNode);
                    if (referencingNode.GenericType.Count != baseEventDecl.GenericType.Count)
                    {
                        throw new ParsingException($"Mismatch in number of generic type arguments for base event {referencingNode.Identifier.Text}.");
                    }
                }

                node.BaseClassDecl = baseEventDecl;
            }
        }

        private void VisitGenericType(EventDeclaration node)
        {
            int genericCount = 0;
            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.Assert &&
                this.TokenStream.Peek().Type != TokenType.Assume &&
                this.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                this.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (this.TokenStream.Peek().Type != TokenType.Identifier &&
                    this.TokenStream.Peek().Type != TokenType.Dot &&
                    this.TokenStream.Peek().Type != TokenType.Comma &&
                    this.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                    this.TokenStream.Peek().Type != TokenType.RightAngleBracket &&
                    this.TokenStream.Peek().Type != TokenType.Object &&
                    this.TokenStream.Peek().Type != TokenType.String &&
                    this.TokenStream.Peek().Type != TokenType.Sbyte &&
                    this.TokenStream.Peek().Type != TokenType.Byte &&
                    this.TokenStream.Peek().Type != TokenType.Short &&
                    this.TokenStream.Peek().Type != TokenType.Ushort &&
                    this.TokenStream.Peek().Type != TokenType.Int &&
                    this.TokenStream.Peek().Type != TokenType.Uint &&
                    this.TokenStream.Peek().Type != TokenType.Long &&
                    this.TokenStream.Peek().Type != TokenType.Ulong &&
                    this.TokenStream.Peek().Type != TokenType.Char &&
                    this.TokenStream.Peek().Type != TokenType.Bool &&
                    this.TokenStream.Peek().Type != TokenType.Decimal &&
                    this.TokenStream.Peek().Type != TokenType.Float &&
                    this.TokenStream.Peek().Type != TokenType.Double)
                {
                    break;
                }

                if (genericCount == 0 &&
                    this.TokenStream.Peek().Type == TokenType.Comma)
                {
                    throw new ParsingException("Expected generic type.", TokenType.Identifier);
                }
                else if (this.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                {
                    node.GenericType.Add(this.TokenStream.Peek());
                    genericCount++;
                }
                else if (this.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                {
                    if (genericCount == 0)
                    {
                        throw new ParsingException("Invalid generic expression.", TokenType.Identifier);
                    }

                    node.GenericType.Add(this.TokenStream.Peek());
                    genericCount--;
                }
                else if (this.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    this.TokenStream.Swap(new Token(this.TokenStream.Peek().TextUnit, TokenType.EventIdentifier));
                    node.GenericType.Add(this.TokenStream.Peek());
                }
                else if (this.TokenStream.Peek().Type == TokenType.Dot ||
                    this.TokenStream.Peek().Type == TokenType.Comma ||
                    this.TokenStream.Peek().Type == TokenType.Object ||
                    this.TokenStream.Peek().Type == TokenType.String ||
                    this.TokenStream.Peek().Type == TokenType.Sbyte ||
                    this.TokenStream.Peek().Type == TokenType.Byte ||
                    this.TokenStream.Peek().Type == TokenType.Short ||
                    this.TokenStream.Peek().Type == TokenType.Ushort ||
                    this.TokenStream.Peek().Type == TokenType.Int ||
                    this.TokenStream.Peek().Type == TokenType.Uint ||
                    this.TokenStream.Peek().Type == TokenType.Long ||
                    this.TokenStream.Peek().Type == TokenType.Ulong ||
                    this.TokenStream.Peek().Type == TokenType.Char ||
                    this.TokenStream.Peek().Type == TokenType.Bool ||
                    this.TokenStream.Peek().Type == TokenType.Decimal ||
                    this.TokenStream.Peek().Type == TokenType.Float ||
                    this.TokenStream.Peek().Type == TokenType.Double)
                {
                    node.GenericType.Add(this.TokenStream.Peek());
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (genericCount > 0)
            {
                throw new ParsingException("Invalid generic expression.", TokenType.Identifier);
            }
        }

        internal void VisitExternDeclaration(NamespaceDeclaration namespaceNode, MachineDeclaration machineNode)
        {
            // Skip over "extern".
            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.EventDecl)
            {
                throw new ParsingException("\"extern\" applies only to events and can have no access modifiers.", TokenType.EventDecl);
            }

            this.VisitEventDeclaration(namespaceNode, machineNode, ModifierSet.CreateDefault(), isExtern: true);
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private static void CheckEventModifierSet(ModifierSet modSet, bool isInMachine)
        {
            if (!isInMachine && modSet.AccessModifier == AccessModifier.Private)
            {
                throw new ParsingException("An event declared in the scope of a namespace cannot be private.");
            }

            if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("An event cannot be declared as protected.");
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("An event cannot be declared as abstract.");
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("An event cannot be declared as partial.");
            }
        }
    }
}
