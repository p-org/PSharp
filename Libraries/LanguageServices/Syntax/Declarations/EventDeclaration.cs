//-----------------------------------------------------------------------
// <copyright file="EventDeclaration.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Event declaration syntax node.
    /// </summary>
    internal sealed class EventDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The machine parent node.
        /// </summary>
        internal readonly MachineDeclaration Machine;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal readonly AccessModifier AccessModifier;

        /// <summary>
        /// The event keyword.
        /// </summary>
        internal Token EventKeyword;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The generic type of the event.
        /// </summary>
        internal List<Token> GenericType;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesis;

        /// <summary>
        /// The payload types.
        /// </summary>
        internal List<Token> PayloadTypes;

        /// <summary>
        /// The payload identifiers.
        /// </summary>
        internal List<Token> PayloadIdentifiers;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesis;

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
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="modSet">Modifier set</param>
        internal EventDeclaration(IPSharpProgram program, MachineDeclaration machineNode, ModifierSet modSet)
            : base(program)
        {
            this.Machine = machineNode;
            this.AccessModifier = modSet.AccessModifier;
            this.GenericType = new List<Token>();
            this.PayloadTypes = new List<Token>();
            this.PayloadIdentifiers = new List<Token>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            string text = "";

            try
            {
                text = this.GetRewrittenEventDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                IO.Debug("Exception was thrown during rewriting:");
                IO.Debug(ex.Message);
                IO.Debug(ex.StackTrace);
                IO.Error.ReportAndExit("Failed to rewrite event '{0}'.",
                    this.Identifier.TextUnit.Text);
            }

            base.TextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten event declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenEventDeclaration(int indentLevel)
        {
            var indent0 = GetIndent(indentLevel);
            var indent1 = GetIndent(indentLevel + 1);
            var indent2 = GetIndent(indentLevel + 2);

            string text = "";

            if ((this.Program as AbstractPSharpProgram).GetProject().CompilationContext.
                Configuration.CompilationTarget == CompilationTarget.Remote)
            {
                text += indent0 + "[System.Runtime.Serialization.DataContract]\n";
            }

            text += indent0;
            if (this.AccessModifier == AccessModifier.None)
            {
                // The event was declared in the scope of a machine.
                if (this.Machine != null)
                {
                    text += "private ";
                }
                // The event was declared in the scope of a namespace.
                else
                {
                    text += "public ";
                }
            }
            else if (this.AccessModifier == AccessModifier.Public)
            {
                text += "public ";
            }
            else if (this.AccessModifier == AccessModifier.Internal)
            {
                text += "internal ";
            }

            text += "class " + this.Identifier.TextUnit.Text;

            foreach (var token in this.GenericType)
            {
                text += token.TextUnit.Text;
            }

            text += " : Event\n";
            text += indent0 + "{\n";

            var newLine = "";
            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                text += indent1 + "public ";
                text += this.PayloadTypes[i].TextUnit.Text + " ";
                text += this.PayloadIdentifiers[i].TextUnit.Text + ";\n";
                newLine = "\n";     // Not included in payload lines
            }

            text += newLine;
            text += indent1 + "public ";
            text += this.Identifier.TextUnit.Text + "(";

            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                if (i == this.PayloadIdentifiers.Count - 1)
                {
                    text += this.PayloadTypes[i].TextUnit.Text + " ";
                    text += this.PayloadIdentifiers[i].TextUnit.Text;
                }
                else
                {
                    text += this.PayloadTypes[i].TextUnit.Text + " ";
                    text += this.PayloadIdentifiers[i].TextUnit.Text + ", ";
                }
            }

            text += ")\n";
            text += indent2 + ": base(";

            if (this.AssertKeyword != null)
            {
                text += this.AssertValue + ", -1";
            }
            else if (this.AssumeKeyword != null)
            {
                text += "-1, " + this.AssumeValue;
            }

            text += ")\n";
            text += indent1 + "{\n";

            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                text += indent2 + "this." + this.PayloadIdentifiers[i].TextUnit.Text + " = ";
                text += this.PayloadIdentifiers[i].TextUnit.Text + ";\n";
            }

            text += indent1 + "}\n";
            text += indent0 + "}\n";

            return text;
        }

        #endregion
    }
}
