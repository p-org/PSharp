using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Event declaration syntax node.
    /// </summary>
    internal sealed class EventDeclaration : PSharpSyntaxNode
    {
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDeclaration"/> class.
        /// </summary>
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
            string text = string.Empty;

            try
            {
                text = this.GetRewrittenEventDeclaration(indentLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception was thrown during rewriting:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Error.ReportAndExit("Failed to rewrite event '{0}'.", this.Identifier.TextUnit.Text);
            }

            this.TextUnit = this.EventKeyword.TextUnit.WithText(text);
        }

        /// <summary>
        /// Returns the rewritten event declaration.
        /// </summary>
        private string GetRewrittenEventDeclaration(int indentLevel)
        {
            var indent0 = GetIndent(indentLevel);
            var indent1 = GetIndent(indentLevel + 1);
            var indent2 = GetIndent(indentLevel + 2);

            string text = string.Empty;

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
                    text += token.TextUnit.Text + (token.Type == TokenType.Comma ? " " : string.Empty);
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

            var newLine = string.Empty;
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

            var separator = string.Empty;
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

            if (allDecls.Length > 1)
            {
                // We don't pass the most-derived decl's params to the base class
                separator = string.Empty;
                foreach (var decl in allDecls.Take(allDecls.Length - 1))
                {
                    for (int i = 0; i < decl.PayloadIdentifiers.Count; i++)
                    {
                        text += $"{separator}{decl.PayloadIdentifiers[i].TextUnit.Text}";
                        separator = ", ";
                    }
                }
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
    }
}
