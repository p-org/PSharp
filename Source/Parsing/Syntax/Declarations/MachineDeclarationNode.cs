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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Machine declaration node.
    /// </summary>
    internal sealed class MachineDeclarationNode : PSharpSyntaxNode
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
        /// The modifier token.
        /// </summary>
        internal Token Modifier;

        /// <summary>
        /// The abstract modifier token.
        /// </summary>
        internal Token AbstractModifier;

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
        internal List<FieldDeclarationNode> FieldDeclarations;

        /// <summary>
        /// List of state declarations.
        /// </summary>
        internal List<StateDeclarationNode> StateDeclarations;

        /// <summary>
        /// List of method declarations.
        /// </summary>
        internal List<MethodDeclarationNode> MethodDeclarations;

        /// <summary>
        /// List of function declarations.
        /// </summary>
        internal List<PFunctionDeclarationNode> FunctionDeclarations;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        internal Token RightCurlyBracketToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isPSharp">Is P# machine</param>
        /// <param name="isMain">Is main machine</param>
        /// <param name="isModel">Is a model</param>
        /// <param name="isMonitor">Is a monitor</param>
        internal MachineDeclarationNode(bool isMain, bool isModel, bool isMonitor)
            : base(isModel)
        {
            this.IsMain = isMain;
            this.IsMonitor = isMonitor;
            this.BaseNameTokens = new List<Token>();
            this.FieldDeclarations = new List<FieldDeclarationNode>();
            this.StateDeclarations = new List<StateDeclarationNode>();
            this.MethodDeclarations = new List<MethodDeclarationNode>();
            this.FunctionDeclarations = new List<PFunctionDeclarationNode>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            foreach (var node in this.FieldDeclarations)
            {
                node.Rewrite(program);
            }

            foreach (var node in this.StateDeclarations)
            {
                node.Rewrite(program);
            }

            foreach (var node in this.MethodDeclarations)
            {
                node.Rewrite(program);
            }

            foreach (var node in this.FunctionDeclarations)
            {
                node.Rewrite(program);
            }

            var text = "";
            var initToken = this.MachineKeyword;

            if (this.IsMain)
            {
                text += "[Main]\n";
            }

            if (base.IsModel)
            {
                text += "[Model]\n";
            }

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            if (this.AbstractModifier != null)
            {
                initToken = this.AbstractModifier;
                text += this.AbstractModifier.TextUnit.Text;
                text += " ";
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
                text += node.GetRewrittenText();
            }

            foreach (var node in this.StateDeclarations)
            {
                text += node.GetRewrittenText();
            }

            foreach (var node in this.MethodDeclarations)
            {
                text += node.GetRewrittenText();
            }

            foreach (var node in this.FunctionDeclarations)
            {
                text += node.GetRewrittenText();
            }

            text += this.InstrumentGotoStateTransitions(program);

            if (!this.IsMonitor)
            {
                text += this.InstrumentPushStateTransitions(program);
            }

            text += this.InstrumentActionsBindings(program);

            text += this.RightCurlyBracketToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, initToken.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Instruments the goto state transitions.
        /// </summary>
        /// <returns>Text</returns>
        /// <param name="program">Program</param>
        private string InstrumentGotoStateTransitions(IPSharpProgram program)
        {
            if (!this.StateDeclarations.Any(val => val.GotoStateTransitions.Count > 0))
            {
                return "";
            }

            var text = "\n";
            text += "protected override System.Collections.Generic.Dictionary<Type, " +
                "GotoStateTransitions> DefineGotoStateTransitions()\n";
            text += "{\n";
            text += " var dict = new System.Collections.Generic.Dictionary<Type, " +
                "GotoStateTransitions>();\n";
            text += "\n";

            foreach (var state in this.StateDeclarations)
            {
                text += " var " + state.Identifier.TextUnit.Text.ToLower() + "Dict = new GotoStateTransitions();\n";

                foreach (var transition in state.GotoStateTransitions)
                {
                    var onExitText = "";
                    if (state.TransitionsOnExitActions.ContainsKey(transition.Key))
                    {
                        var onExitAction = state.TransitionsOnExitActions[transition.Key];
                        onExitAction.Rewrite(program);
                        onExitText = onExitAction.GetRewrittenText();
                    }

                    string eventId = "";
                    if (transition.Key.Type == TokenType.HaltEvent)
                    {
                        eventId = "Microsoft.PSharp.Halt";
                    }
                    else if (transition.Key.Type == TokenType.DefaultEvent)
                    {
                        eventId = "Microsoft.PSharp.Default";
                    }
                    else
                    {
                        eventId = transition.Key.TextUnit.Text;
                    }

                    if (onExitText.Length > 0)
                    {
                        text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                            eventId + "), typeof(" + transition.Value.TextUnit.Text + "), () => " +
                            onExitText + ");\n";
                    }
                    else
                    {
                        text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                            eventId + "), typeof(" + transition.Value.TextUnit.Text + "));\n";
                    }
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
        /// Instruments the push state transitions.
        /// </summary>
        /// <returns>Text</returns>
        /// <param name="program">Program</param>
        private string InstrumentPushStateTransitions(IPSharpProgram program)
        {
            if (!this.StateDeclarations.Any(val => val.PushStateTransitions.Count > 0))
            {
                return "";
            }

            var text = "\n";
            text += "protected override System.Collections.Generic.Dictionary<Type, " +
                "PushStateTransitions> DefinePushStateTransitions()\n";
            text += "{\n";
            text += " var dict = new System.Collections.Generic.Dictionary<Type, " +
                "PushStateTransitions>();\n";
            text += "\n";

            foreach (var state in this.StateDeclarations)
            {
                text += " var " + state.Identifier.TextUnit.Text.ToLower() + "Dict = new PushStateTransitions();\n";

                foreach (var transition in state.PushStateTransitions)
                {
                    var onExitText = "";
                    if (state.TransitionsOnExitActions.ContainsKey(transition.Key))
                    {
                        var onExitAction = state.TransitionsOnExitActions[transition.Key];
                        onExitAction.Rewrite(program);
                        onExitText = onExitAction.GetRewrittenText();
                    }

                    string eventId = "";
                    if (transition.Key.Type == TokenType.HaltEvent)
                    {
                        eventId = "Microsoft.PSharp.Halt";
                    }
                    else if (transition.Key.Type == TokenType.DefaultEvent)
                    {
                        eventId = "Microsoft.PSharp.Default";
                    }
                    else
                    {
                        eventId = transition.Key.TextUnit.Text;
                    }

                    if (onExitText.Length > 0)
                    {
                        text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                            eventId + "), typeof(" + transition.Value.TextUnit.Text + "), () => " +
                            onExitText + ");\n";
                    }
                    else
                    {
                        text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                            eventId + "), typeof(" + transition.Value.TextUnit.Text + "));\n";
                    }
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
        /// <param name="program">Program</param>
        private string InstrumentActionsBindings(IPSharpProgram program)
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
                    var actionText = "";
                    if (state.ActionHandlers.ContainsKey(binding.Key))
                    {
                        var action = state.ActionHandlers[binding.Key];
                        action.Rewrite(program);
                        actionText = action.GetRewrittenText();
                    }

                    string eventId = "";
                    if (binding.Key.Type == TokenType.HaltEvent)
                    {
                        eventId = "Microsoft.PSharp.Halt";
                    }
                    else if (binding.Key.Type == TokenType.DefaultEvent)
                    {
                        eventId = "Microsoft.PSharp.Default";
                    }
                    else
                    {
                        eventId = binding.Key.TextUnit.Text;
                    }

                    if (actionText.Length > 0)
                    {
                        text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                            eventId + "), () => " + actionText + ");\n";
                    }
                    else
                    {
                        text += " " + state.Identifier.TextUnit.Text.ToLower() + "Dict.Add(typeof(" +
                            eventId + "), new Action(" + binding.Value.TextUnit.Text + "));\n";
                    }
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
