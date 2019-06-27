// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# modifier parsing visitor.
    /// </summary>
    internal sealed class ModifierVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierVisitor"/> class.
        /// </summary>
        internal ModifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(ModifierSet modSet)
        {
            if (this.TokenStream.Peek().Type == TokenType.Public)
            {
                modSet.AccessModifier = AccessModifier.Public;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Private)
            {
                modSet.AccessModifier = AccessModifier.Private;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Protected)
            {
                modSet.AccessModifier = AccessModifier.Protected;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Internal)
            {
                modSet.AccessModifier = AccessModifier.Internal;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Abstract)
            {
                modSet.InheritanceModifier = InheritanceModifier.Abstract;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Virtual)
            {
                modSet.InheritanceModifier = InheritanceModifier.Virtual;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Override)
            {
                modSet.InheritanceModifier = InheritanceModifier.Override;
            }
            else if (this.TokenStream.Peek().Type == TokenType.StartState)
            {
                modSet.IsStart = true;
            }
            else if (this.TokenStream.Peek().Type == TokenType.HotState)
            {
                modSet.IsHot = true;
            }
            else if (this.TokenStream.Peek().Type == TokenType.ColdState)
            {
                modSet.IsCold = true;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Async)
            {
                modSet.IsAsync = true;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Partial)
            {
                modSet.IsPartial = true;
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private void CheckModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier != AccessModifier.None &&
                    (this.TokenStream.Peek().Type == TokenType.Public ||
                    this.TokenStream.Peek().Type == TokenType.Private ||
                    this.TokenStream.Peek().Type == TokenType.Protected ||
                    this.TokenStream.Peek().Type == TokenType.Internal))
            {
                throw new ParsingException("More than one protection modifier.", this.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier != InheritanceModifier.None &&
                this.TokenStream.Peek().Type == TokenType.Abstract)
            {
                throw new ParsingException("Duplicate abstract modifier.", this.TokenStream.Peek());
            }
            else if (modSet.IsStart && this.TokenStream.Peek().Type == TokenType.StartState)
            {
                throw new ParsingException("Duplicate start state modifier.", this.TokenStream.Peek());
            }
            else if (modSet.IsHot && this.TokenStream.Peek().Type == TokenType.HotState)
            {
                throw new ParsingException("Duplicate hot state liveness modifier.", this.TokenStream.Peek());
            }
            else if (modSet.IsCold && this.TokenStream.Peek().Type == TokenType.ColdState)
            {
                throw new ParsingException("Duplicate cold state liveness modifier.", this.TokenStream.Peek());
            }
            else if ((modSet.IsCold && this.TokenStream.Peek().Type == TokenType.HotState) ||
                (modSet.IsHot && this.TokenStream.Peek().Type == TokenType.ColdState))
            {
                throw new ParsingException("More than one state liveness modifier.", this.TokenStream.Peek());
            }
            else if (modSet.IsAsync && this.TokenStream.Peek().Type == TokenType.Async)
            {
                throw new ParsingException("Duplicate async modifier.", this.TokenStream.Peek());
            }
            else if (modSet.IsPartial && this.TokenStream.Peek().Type == TokenType.Partial)
            {
                throw new ParsingException("Duplicate partial modifier.", this.TokenStream.Peek());
            }
        }
    }
}
