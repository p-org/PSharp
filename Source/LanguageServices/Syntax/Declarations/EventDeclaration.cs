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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;
using System.Linq;

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

        /// <summary>
        /// The Event subclass that this subclass inherits from, if any.
        /// </summary>
        internal EventDeclaration BaseClassDecl;

        /// <summary>
        /// If true, this is an extern event declaration (not fully defined)
        /// </summary>
        internal bool IsExtern;

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
            string text = String.Empty;

            try
            {
                text = this.GetRewrittenEventDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit("Failed to rewrite event '{0}'.",
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

            string text = String.Empty;

            if ((this.Program as AbstractPSharpProgram).GetProject().CompilationContext.
                Configuration.CompilationTarget == CompilationTarget.Remote)
            {
                text += indent0 + "[System.Runtime.Serialization.DataContract]\n";
            }

            text += indent0;
            if (this.AccessModifier == AccessModifier.None)
            {
                // If the the event was declared in the scope of a machine it is private;
                // otherwise it was declared in the scope of a namespace and is public.
                text += this.Machine != null ? "private " : "public ";
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

            var allDecls = EventDeclarations.EnumerateInheritance(this).ToArray();

            void appendGenericType(EventDeclaration decl)
            {
                // The GenericTypes contains the leading < and > as well as the type identifier(s and comma(s)).
                foreach (var token in decl.GenericType)
                {
                    text += token.TextUnit.Text + (token.Type == TokenType.Comma ? " " : "");
                }
            }

            appendGenericType(this);

            text += " : ";
            if (this.BaseClassDecl != null)
            {
                text += this.BaseClassDecl.Identifier.Text;
                appendGenericType(this.BaseClassDecl);
            }
            else
            {
                text += "Event";
            }

            text += "\n" + indent0 + "{\n";

            var newLine = String.Empty;
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

            var separator = String.Empty;
            foreach (var decl in allDecls)
            {
                for (int i = 0; i < decl.PayloadIdentifiers.Count; i++)
                {
                    text += $"{separator}{decl.PayloadTypes[i].TextUnit.Text} {decl.PayloadIdentifiers[i].TextUnit.Text}";
                    separator = ", ";
                }
            }

            text += ")\n";
            text += indent2 + ": base(";

            void addAssertAssumeParams(bool forDerived)
            {
                if (this.AssertKeyword != null)
                {
                    text += this.AssertValue + ", -1";
                }
                else if (this.AssumeKeyword != null)
                {
                    text += "-1, " + this.AssumeValue;
                }
                else if (forDerived)
                {
                    text += "-1, -1";
                }
            }

            if (allDecls.Length > 1)
            {
                // We don't pass the most-derived decl's params to the base class
                separator = String.Empty;
                foreach (var decl in allDecls.Take(allDecls.Length - 1))
                {
                    for (int i = 0; i < decl.PayloadIdentifiers.Count; i++)
                    {
                        text += $"{separator}{decl.PayloadIdentifiers[i].TextUnit.Text}";
                        separator = ", ";
                    }
                }
            }
            else
            {
                // Assert/Assume are passed as params to ctor overload for classes that derive directly from Event.
                addAssertAssumeParams(forDerived: false);
            }

            text += ")\n";
            text += indent1 + "{\n";

            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                text += indent2 + "this." + this.PayloadIdentifiers[i].TextUnit.Text + " = ";
                text += this.PayloadIdentifiers[i].TextUnit.Text + ";\n";
            }

            if (this.BaseClassDecl != null)
            {
                // Assert/Assume are passed as to a protected method for classes that don't derive directly from Event.
                // Override any base-class declaration; if none are specified on the derived class then this will turn it off.
                text += indent2 + "base.SetCardinalityConstraints(";
                addAssertAssumeParams(forDerived: true);
                text += ");\n";
            }

            text += indent1 + "}\n";
            text += indent0 + "}\n";

            return text;
        }

        #endregion
    }
}
