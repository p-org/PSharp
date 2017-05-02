//-----------------------------------------------------------------------
// <copyright file="ModifierVisitor.cs">
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
    /// The P# modifier parsing visitor.
    /// </summary>
    internal sealed class ModifierVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal ModifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="modSet">Modifier set</param>
        internal void Visit(ModifierSet modSet)
        {
            if (base.TokenStream.Peek().Type == TokenType.Public)
            {
                modSet.AccessModifier = AccessModifier.Public;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Private)
            {
                modSet.AccessModifier = AccessModifier.Private;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Protected)
            {
                modSet.AccessModifier = AccessModifier.Protected;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Internal)
            {
                modSet.AccessModifier = AccessModifier.Internal;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Abstract)
            {
                modSet.InheritanceModifier = InheritanceModifier.Abstract;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Virtual)
            {
                modSet.InheritanceModifier = InheritanceModifier.Virtual;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Override)
            {
                modSet.InheritanceModifier = InheritanceModifier.Override;
            }
            else if (base.TokenStream.Peek().Type == TokenType.StartState)
            {
                modSet.IsStart = true;
            }
            else if (base.TokenStream.Peek().Type == TokenType.HotState)
            {
                modSet.IsHot = true;
            }
            else if (base.TokenStream.Peek().Type == TokenType.ColdState)
            {
                modSet.IsCold = true;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Async)
            {
                modSet.IsAsync = true;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Partial)
            {
                modSet.IsPartial = true;
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier != AccessModifier.None &&
                    (base.TokenStream.Peek().Type == TokenType.Public ||
                    base.TokenStream.Peek().Type == TokenType.Private ||
                    base.TokenStream.Peek().Type == TokenType.Protected ||
                    base.TokenStream.Peek().Type == TokenType.Internal))
            {
                throw new ParsingException("More than one protection modifier.",
                    new List<TokenType>());
            }
            else if (modSet.InheritanceModifier != InheritanceModifier.None &&
                base.TokenStream.Peek().Type == TokenType.Abstract)
            {
                throw new ParsingException("Duplicate abstract modifier.",
                    new List<TokenType>());
            }
            else if (modSet.IsStart && base.TokenStream.Peek().Type == TokenType.StartState)
            {
                throw new ParsingException("Duplicate start state modifier.",
                    new List<TokenType>());
            }
            else if (modSet.IsHot && base.TokenStream.Peek().Type == TokenType.HotState)
            {
                throw new ParsingException("Duplicate hot state liveness modifier.",
                    new List<TokenType>());
            }
            else if (modSet.IsCold && base.TokenStream.Peek().Type == TokenType.ColdState)
            {
                throw new ParsingException("Duplicate cold state liveness modifier.",
                    new List<TokenType>());
            }
            else if ((modSet.IsCold && base.TokenStream.Peek().Type == TokenType.HotState) ||
                (modSet.IsHot && base.TokenStream.Peek().Type == TokenType.ColdState))
            {
                throw new ParsingException("More than one state liveness modifier.",
                    new List<TokenType>());
            }
            else if (modSet.IsAsync && base.TokenStream.Peek().Type == TokenType.Async)
            {
                throw new ParsingException("Duplicate async modifier.",
                    new List<TokenType>());
            }
            else if (modSet.IsPartial && base.TokenStream.Peek().Type == TokenType.Partial)
            {
                throw new ParsingException("Duplicate partial modifier.",
                    new List<TokenType>());
            }
        }
    }
}
