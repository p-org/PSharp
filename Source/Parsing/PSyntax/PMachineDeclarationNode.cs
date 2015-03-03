//-----------------------------------------------------------------------
// <copyright file="PMachineDeclarationNode.cs">
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

namespace Microsoft.PSharp.Parsing.PSyntax
{
    /// <summary>
    /// Machine declaration node.
    /// </summary>
    public sealed class PMachineDeclarationNode : PSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the machine is the main machine.
        /// </summary>
        public readonly bool IsMain;

        /// <summary>
        /// The machine keyword.
        /// </summary>
        public Token MachineKeyword;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        public Token LeftCurlyBracketToken;

        /// <summary>
        /// List of field declarations.
        /// </summary>
        public List<PFieldDeclarationNode> FieldDeclarations;

        /// <summary>
        /// List of state declarations.
        /// </summary>
        public List<PStateDeclarationNode> StateDeclarations;

        /// <summary>
        /// List of function declarations.
        /// </summary>
        public List<PFunctionDeclarationNode> FunctionDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        public Token RightCurlyBracketToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isMain">Is main machine</param>
        public PMachineDeclarationNode(bool isMain)
            : base()
        {
            this.IsMain = isMain;
            this.FieldDeclarations = new List<PFieldDeclarationNode>();
            this.StateDeclarations = new List<PStateDeclarationNode>();
            this.FunctionDeclarations = new List<PFunctionDeclarationNode>();
        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal override void Rewrite(ref int position)
        {
            var start = position;

            foreach (var node in this.FieldDeclarations)
            {
                node.Rewrite(ref position);
            }

            foreach (var node in this.StateDeclarations)
            {
                node.Rewrite(ref position);
            }

            foreach (var node in this.FunctionDeclarations)
            {
                node.Rewrite(ref position);
            }

            var text = "";

            if (this.IsMain)
            {
                text += "[Main]\n";
            }

            text += "class " + this.Identifier.TextUnit.Text + " : Machine";

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.FieldDeclarations)
            {
                text += node.GetRewrittenText();
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.GetRewrittenText();
            }

            foreach (var node in this.FunctionDeclarations)
            {
                text += node.GetRewrittenText();
            }

            text += this.InstrumentStateTransitions();
            text += this.InstrumentActionsBindings();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.RewrittenTextUnit = new TextUnit(text, this.MachineKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            foreach (var node in this.FieldDeclarations)
            {
                node.GenerateTextUnit();
            }

            foreach (var node in this.StateDeclarations)
            {
                node.GenerateTextUnit();
            }

            foreach (var node in this.FunctionDeclarations)
            {
                node.GenerateTextUnit();
            }

            var text = "";

            text += this.MachineKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.FieldDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.FunctionDeclarations)
            {
                text += node.GetFullText();
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.MachineKeyword.TextUnit.Line,
                this.MachineKeyword.TextUnit.Start);
        }

        #endregion

        #region private API

        /// <summary>
        /// Instruments the state transitions.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentStateTransitions()
        {
            if (!this.StateDeclarations.Any(val => val.StateTransitions.Count > 0))
            {
                return "";
            }

            var text = "\n";
            text += "protected override System.Collections.Generic.Dictionary<Type, " +
                "StepStateTransitions> DefineStepStateTransitions()\n";
            text += "{\n";
            text += " var dict = new System.Collections.Generic.Dictionary<Type, " +
                "StepStateTransitions>();\n";
            text += "\n";

            foreach (var state in this.StateDeclarations)
            {
                text += " var " + state.Identifier.TextUnit.Text.ToLower() + "Dict = new StepStateTransitions();\n";

                foreach (var transition in state.StateTransitions)
                {
                    text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                        transition.Key.TextUnit.Text +
                        "), typeof(" + transition.Value.TextUnit.Text + "));\n";
                }

                text += " dict.Add(typeof(" + state.Identifier.TextUnit.Text + "), " +
                    state.Identifier.TextUnit.Text.ToLower() + "Dict);\n";
                text += "\n";
            }

            text += " return dict;\n";
            text += "}\n";

            return text;
        }

        /// <summary>
        /// Instruments the action bindings.
        /// </summary>
        /// <returns>Text</returns>
        private string InstrumentActionsBindings()
        {
            if (!this.StateDeclarations.Any(val => val.ActionBindings.Count > 0))
            {
                return "";
            }

            var text = "\n";
            text += "protected override System.Collections.Generic.Dictionary<Type, " +
                "ActionBindings> DefineActionBindings()\n";
            text += "{\n";
            text += " var dict = new System.Collections.Generic.Dictionary<Type, " +
                "ActionBindings>();\n";
            text += "\n";

            foreach (var state in this.StateDeclarations)
            {
                text += " var " + state.Identifier.TextUnit.Text.ToLower() + "Dict = new ActionBindings();\n";

                foreach (var binding in state.ActionBindings)
                {
                    text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                        binding.Key.TextUnit.Text +
                        "), new Action(" + binding.Value.TextUnit.Text + "));\n";
                }

                text += " dict.Add(typeof(" + state.Identifier.TextUnit.Text + "), " +
                    state.Identifier.TextUnit.Text.ToLower() + "Dict);\n";
                text += "\n";
            }

            text += " return dict;\n";
            text += "}\n";

            return text;
        }

        #endregion
    }
}
