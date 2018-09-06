﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        /// <param name="modSet">Modifier set</param>
        internal void Visit(IPSharpProgram program, NamespaceDeclaration parentNode, bool isMonitor, ModifierSet modSet)
        {
            if (isMonitor)
            {
                this.CheckMonitorModifierSet(modSet);
            }
            else
            {
                this.CheckMachineModifierSet(modSet);
            }

            var node = new MachineDeclaration(base.TokenStream.Program, parentNode, isMonitor, modSet);
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

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            var nameVisitor = new NameVisitor(base.TokenStream);
            node.TemplateParameters = nameVisitor.ConsumeTemplateParams();

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

                    var baseNameTokensVisitor = new NameVisitor(base.TokenStream);
                    node.BaseNameTokens = baseNameTokensVisitor.ConsumeGenericName(TokenType.MachineIdentifier);
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

            var stateDeclarations = node.GetAllStateDeclarations();
            if (stateDeclarations.Count == 0 && node.BaseNameTokens.Count == 0)
            {
                throw new ParsingException("A machine must declare at least one state.",
                    new List<TokenType>());
            }

            var startStates = stateDeclarations.FindAll(s => s.IsStart);
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
        /// Visits the next intra-machine declaration.
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

                    case TokenType.ExternDecl:
                        new EventDeclarationVisitor(base.TokenStream).VisitExternDeclaration(node.Namespace, node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.Abstract:
                    case TokenType.StartState:
                    case TokenType.HotState:
                    case TokenType.ColdState:
                    case TokenType.EventDecl:
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
            ModifierSet modSet = ModifierSet.CreateDefault();

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
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
                new ModifierVisitor(base.TokenStream).Visit(modSet);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.EventDecl &&
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
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected event, state, group or method declaration.",
                    new List<TokenType>
                {
                    TokenType.EventDecl,
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

            if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                new EventDeclarationVisitor(base.TokenStream).Visit(parentNode.Namespace, parentNode, modSet);
            }
            else if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                new StateDeclarationVisitor(base.TokenStream).Visit(parentNode, null, modSet);
            }
            else if (base.TokenStream.Peek().Type == TokenType.StateGroupDecl)
            {
                new StateGroupDeclarationVisitor(base.TokenStream).Visit(parentNode, null, modSet);
            }
            else
            {
                new MachineMemberDeclarationVisitor(base.TokenStream).Visit(parentNode, modSet);
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckMachineModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Private)
            {
                throw new ParsingException("A machine cannot be declared as private.",
                    new List<TokenType>());
            }
            else if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("A machine cannot be declared as protected.",
                    new List<TokenType>());
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckMonitorModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Private)
            {
                throw new ParsingException("A monitor cannot be declared as private.",
                    new List<TokenType>());
            }
            else if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("A monitor cannot be declared as protected.",
                    new List<TokenType>());
            }
        }
    }
}
