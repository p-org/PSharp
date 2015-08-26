//-----------------------------------------------------------------------
// <copyright file="MachineDeclaration.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Machine declaration syntax node.
    /// </summary>
    internal sealed class MachineDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the machine is the main machine.
        /// </summary>
        internal readonly bool IsMain;

        /// <summary>
        /// True if the machine is a monitor.
        /// </summary>
        internal readonly bool IsMonitor;

        /// <summary>
        /// The machine keyword.
        /// </summary>
        internal Token MachineKeyword;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The inheritance modifier.
        /// </summary>
        internal InheritanceModifier InheritanceModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The colon token.
        /// </summary>
        internal Token ColonToken;

        /// <summary>
        /// The base name tokens.
        /// </summary>
        internal List<Token> BaseNameTokens;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        internal Token LeftCurlyBracketToken;

        /// <summary>
        /// List of field declarations.
        /// </summary>
        internal List<FieldDeclaration> FieldDeclarations;

        /// <summary>
        /// List of state declarations.
        /// </summary>
        internal List<StateDeclaration> StateDeclarations;

        /// <summary>
        /// List of method declarations.
        /// </summary>
        internal List<MethodDeclaration> MethodDeclarations;

        /// <summary>
        /// List of function declarations.
        /// </summary>
        internal List<PFunctionDeclaration> FunctionDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="isPSharp">Is P# machine</param>
        /// <param name="isMain">Is main machine</param>
        /// <param name="isModel">Is a model</param>
        /// <param name="isMonitor">Is a monitor</param>
        internal MachineDeclaration(IPSharpProgram program, bool isMain, bool isModel, bool isMonitor)
            : base(program, isModel)
        {
            this.IsMain = isMain;
            this.IsMonitor = isMonitor;
            this.BaseNameTokens = new List<Token>();
            this.FieldDeclarations = new List<FieldDeclaration>();
            this.StateDeclarations = new List<StateDeclaration>();
            this.MethodDeclarations = new List<MethodDeclaration>();
            this.FunctionDeclarations = new List<PFunctionDeclaration>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            if (this.IsModel || this.IsMonitor)
            {
                base.TextUnit = new TextUnit("", 0);
                return;
            }

            foreach (var node in this.FieldDeclarations)
            {
                node.Rewrite();
            }
            
            foreach (var node in this.StateDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.Rewrite();
            }

            foreach (var node in this.FunctionDeclarations)
            {
                node.Rewrite();
            }

            var text = this.GetRewrittenMachineDeclaration();

            var realMethods = this.MethodDeclarations.FindAll(m => !m.IsModel);
            var realFuctions = this.FunctionDeclarations.FindAll(m => !m.IsModel);

            foreach (var node in realMethods)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in realFuctions)
            {
                text += node.TextUnit.Text;
            }

            text += this.GetRewrittenWithActions();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.MachineKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            foreach (var node in this.FieldDeclarations)
            {
                node.Model();
            }

            foreach (var node in this.StateDeclarations)
            {
                node.Model();
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.Model();
            }

            foreach (var node in this.FunctionDeclarations)
            {
                node.Model();
            }

            var text = this.GetRewrittenMachineDeclaration();

            var realMethods = this.MethodDeclarations.FindAll(m => !m.IsModel);
            var realFuctions = this.FunctionDeclarations.FindAll(m => !m.IsModel);
            var modelMethods = this.MethodDeclarations.FindAll(m => m.IsModel);
            var modelFunctions = this.FunctionDeclarations.FindAll(m => m.IsModel);

            foreach (var node in realMethods)
            {
                if (!modelMethods.Any(m => m.Identifier.TextUnit.Text.Equals(
                    node.Identifier.TextUnit.Text)))
                {
                    text += node.TextUnit.Text;
                }
            }

            foreach (var node in realFuctions)
            {
                if (!modelFunctions.Any(m => m.Identifier.TextUnit.Text.Equals(
                    node.Identifier.TextUnit.Text)))
                {
                    text += node.TextUnit.Text;
                }
            }

            foreach (var node in modelMethods)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in modelFunctions)
            {
                text += node.TextUnit.Text;
            }

            text += this.GetRewrittenWithActions();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            
            base.TextUnit = new TextUnit(text, this.MachineKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten machine declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenMachineDeclaration()
        {
            var text = "";

            if (this.IsMain)
            {
                text += "[Main]\n";
            }

            if (base.IsModel)
            {
                text += "[Model]\n";
            }

            if (this.AccessModifier == AccessModifier.Public)
            {
                text += "public ";
            }
            else if (this.AccessModifier == AccessModifier.Internal)
            {
                text += "internal ";
            }

            if (this.InheritanceModifier == InheritanceModifier.Abstract)
            {
                text += "abstract ";
            }

            text += "class " + this.Identifier.TextUnit.Text + " : ";

            if (this.ColonToken != null)
            {
                foreach (var node in this.BaseNameTokens)
                {
                    text += node.TextUnit.Text;
                }
            }
            else if (!this.IsMonitor)
            {
                text += "Machine";
            }
            else
            {
                text += "Monitor";
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.FieldDeclarations)
            {
                text += node.TextUnit.Text;
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.TextUnit.Text;
            }
            
            return text;
        }

        /// <summary>
        /// Returns the rewritten with actions.
        /// </summary>
        private string GetRewrittenWithActions()
        {
            var text = "";

            foreach (var state in this.StateDeclarations)
            {
                foreach (var withAction in state.TransitionsOnExitActions)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "void psharp_" + state.Identifier.TextUnit.Text + "_" +
                        withAction.Key.TextUnit.Text + "_action()";
                    text += onExitAction.TextUnit.Text;
                }
            }

            foreach (var state in this.StateDeclarations)
            {
                foreach (var withAction in state.ActionHandlers)
                {
                    var onExitAction = withAction.Value;
                    onExitAction.Rewrite();
                    text += "void psharp_" + state.Identifier.TextUnit.Text + "_" +
                        withAction.Key.TextUnit.Text + "_action()";
                    text += onExitAction.TextUnit.Text;
                }
            }

            return text;
        }

        #endregion
    }
}
