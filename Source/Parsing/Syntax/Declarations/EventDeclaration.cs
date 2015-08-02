﻿//-----------------------------------------------------------------------
// <copyright file="EventDeclaration.cs">
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
using System.Linq;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Event declaration syntax node.
    /// </summary>
    internal sealed class EventDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The event keyword.
        /// </summary>
        internal Token EventKeyword;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The colon token.
        /// </summary>
        internal Token ColonToken;

        /// <summary>
        /// The payload type.
        /// </summary>
        internal PBaseType PayloadType;

        /// <summary>
        /// The assert keyword.
        /// </summary>
        internal Token AssertKeyword;

        /// <summary>
        /// The assume keyword.
        /// </summary>
        internal Token AssumeKeyword;

        /// <summary>
        /// The assert value.
        /// </summary>
        internal int AssertValue;

        /// <summary>
        /// The assume value.
        /// </summary>
        internal int AssumeValue;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        internal EventDeclaration(IPSharpProgram program)
            : base(program, false)
        {

        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite()
        {
            var text = this.GetRewrittenEventDeclaration();
            base.TextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenEventDeclaration();
            base.TextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten event declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenEventDeclaration()
        {
            var text = "";

            if (Configuration.CompileForDistribution)
            {
                text += "[System.Runtime.Serialization.DataContract]\n";
            }

            if (this.AccessModifier == AccessModifier.Public)
            {
                text += "public ";
            }
            else if (this.AccessModifier == AccessModifier.Internal)
            {
                text += "internal ";
            }

            text += "class " + this.Identifier.TextUnit.Text + " : Event";

            text += "\n";
            text += "{\n";
            text += " internal " + this.Identifier.TextUnit.Text + "()\n";

            text += "  : base(";

            if (this.AssertKeyword != null)
            {
                text += this.AssertValue + ", -1";
            }
            else if (this.AssumeKeyword != null)
            {
                text += "-1, " + this.AssumeValue;
            }
            else
            {
                text += "-1, -1";
            }

            text += ")\n";
            text += " { }\n";
            text += "}\n";

            return text;
        }

        #endregion
    }
}
