//-----------------------------------------------------------------------
// <copyright file="MachineDeclarationNode.cs">
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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Machine declaration node.
    /// </summary>
    public sealed class MachineDeclarationNode : PSharpSyntaxNode
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
        /// The modifier token.
        /// </summary>
        public Token Modifier;

        /// <summary>
        /// The abstract modifier token.
        /// </summary>
        public Token AbstractModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The colon token.
        /// </summary>
        public Token ColonToken;

        /// <summary>
        /// The base name tokens.
        /// </summary>
        public List<Token> BaseNameTokens;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        public Token LeftCurlyBracketToken;

        /// <summary>
        /// List of field declarations.
        /// </summary>
        public List<FieldDeclarationNode> FieldDeclarations;

        /// <summary>
        /// List of state declarations.
        /// </summary>
        public List<StateDeclarationNode> StateDeclarations;

        /// <summary>
        /// List of action declarations.
        /// </summary>
        public List<ActionDeclarationNode> ActionDeclarations;

        /// <summary>
        /// List of method declarations.
        /// </summary>
        public List<MethodDeclarationNode> MethodDeclarations;

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
        public MachineDeclarationNode(bool isMain)
        {
            this.IsMain = isMain;
            this.BaseNameTokens = new List<Token>();
            this.FieldDeclarations = new List<FieldDeclarationNode>();
            this.StateDeclarations = new List<StateDeclarationNode>();
            this.ActionDeclarations = new List<ActionDeclarationNode>();
            this.MethodDeclarations = new List<MethodDeclarationNode>();
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

            foreach (var node in this.ActionDeclarations)
            {
                node.Rewrite(ref position);
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.Rewrite(ref position);
            }

            var text = "";

            if (this.IsMain)
            {
                text += "[Main]\n";
            }

            if (this.Modifier != null)
            {
                base.RewrittenTokens.Add(this.Modifier);
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            if (this.AbstractModifier != null)
            {
                base.RewrittenTokens.Add(this.AbstractModifier);
                text += this.AbstractModifier.TextUnit.Text;
                text += " ";
            }

            var classKeyword = "class";
            var classTextUnit = new TextUnit(classKeyword, classKeyword.Length, text.Length);
            base.RewrittenTokens.Add(new Token(classTextUnit, this.MachineKeyword.Line, TokenType.ClassDecl));
            text += classKeyword;
            text += " ";

            text += this.Identifier.TextUnit.Text;
            text += " ";

            base.RewrittenTokens.Add(new Token(new TextUnit(":", 1, text.Length), this.MachineKeyword.Line, TokenType.Colon));
            text += ":";
            text += " ";

            if (this.ColonToken != null)
            {
                foreach (var node in this.BaseNameTokens)
                {
                    base.RewrittenTokens.Add(node);
                    text += node.TextUnit.Text;
                }
            }
            else
            {
                var machineClass = "Machine";
                var machineTextUnit = new TextUnit(machineClass, machineClass.Length, text.Length);
                base.RewrittenTokens.Add(new Token(machineTextUnit, this.MachineKeyword.Line, TokenType.TypeIdentifier));
                text += machineClass;
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.LeftCurlyBracketToken);

            foreach (var node in this.FieldDeclarations)
            {
                text += node.GetRewrittenText();
                base.RewrittenTokens.AddRange(node.RewrittenTokens);
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.GetRewrittenText();
                base.RewrittenTokens.AddRange(node.RewrittenTokens);
            }

            foreach (var node in this.ActionDeclarations)
            {
                text += node.GetRewrittenText();
                base.RewrittenTokens.AddRange(node.RewrittenTokens);
            }

            foreach (var node in this.MethodDeclarations)
            {
                text += node.GetRewrittenText();
                base.RewrittenTokens.AddRange(node.RewrittenTokens);
            }

            text += this.InstrumentStateTransitions();
            text += this.InstrumentActionsBindings();

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            base.RewrittenTokens.Add(this.RightCurlyBracketToken);

            base.RewrittenTextUnit = new TextUnit(text, text.Length, start);
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

            foreach (var node in this.ActionDeclarations)
            {
                node.GenerateTextUnit();
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.GenerateTextUnit();
            }

            var text = "";
            var initToken = this.MachineKeyword;

            if (this.AbstractModifier != null)
            {
                initToken = this.AbstractModifier;
                text += this.AbstractModifier.TextUnit.Text;
                text += " ";
            }

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            text += this.MachineKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            if (this.ColonToken != null)
            {
                text += " ";
                text += this.ColonToken.TextUnit.Text;
                text += " ";

                foreach (var node in this.BaseNameTokens)
                {
                    text += node.TextUnit.Text;
                }
            }

            text += "\n" + this.LeftCurlyBracketToken.TextUnit.Text + "\n";

            foreach (var node in this.FieldDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.ActionDeclarations)
            {
                text += node.GetFullText();
            }

            foreach (var node in this.MethodDeclarations)
            {
                text += node.GetFullText();
            }

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            int length = this.RightCurlyBracketToken.TextUnit.End - initToken.TextUnit.Start + 1;

            base.TextUnit = new TextUnit(text, length, initToken.TextUnit.Start);
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
