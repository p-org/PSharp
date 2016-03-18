//-----------------------------------------------------------------------
// <copyright file="MachineState.cs">
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
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# machine state.
    /// </summary>
    internal sealed class MachineState
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private PSharpAnalysisContext AnalysisContext;

        /// <summary>
        /// The parent state-machine.
        /// </summary>
        private StateMachine Machine;

        /// <summary>
        /// Name of the state.
        /// </summary>
        internal string Name;

        /// <summary>
        /// The underlying declaration.
        /// </summary>
        internal ClassDeclarationSyntax Declaration;

        /// <summary>
        /// List of actions in the state.
        /// </summary>
        internal List<MachineAction> MachineActions;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="classDecl">ClassDeclarationSyntax</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="context">AnalysisContext</param>
        internal MachineState(ClassDeclarationSyntax classDecl, StateMachine machine,
            PSharpAnalysisContext context)
        {
            this.AnalysisContext = context;
            this.Machine = machine;
            this.Name = this.AnalysisContext.GetFullClassName(classDecl);
            this.Declaration = classDecl;
            this.MachineActions = new List<MachineAction>();
            this.FindAllActions();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Finds all state-machine actions for each state-machine in the project.
        /// </summary>
        private void FindAllActions()
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(this.Machine.Declaration.SyntaxTree);

            var attributes = this.Declaration.AttributeLists.SelectMany(var => var.Attributes);
            foreach (var attribute in attributes)
            {
                var type = model.GetTypeInfo(attribute.Name).Type;
                if (type.ToString().Equals(typeof(Microsoft.PSharp.OnEntry).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 1)
                {
                    var arg = attribute.ArgumentList.Arguments[0];
                    var action = this.GetActionFromAttributeArgument(arg);
                    if (action == null)
                    {
                        continue;
                    }
                    
                    this.MachineActions.Add(new OnEntryMachineAction(action, this, this.AnalysisContext));
                }
                else if (type.ToString().Equals(typeof(Microsoft.PSharp.OnExit).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 1)
                {
                    var arg = attribute.ArgumentList.Arguments[0];
                    var action = this.GetActionFromAttributeArgument(arg);
                    if (action == null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnExitMachineAction(action, this, this.AnalysisContext));
                }
                else if (type.ToString().Equals(typeof(Microsoft.PSharp.OnEventDoAction).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 2)
                {
                    var arg = attribute.ArgumentList.Arguments[1];
                    var action = this.GetActionFromAttributeArgument(arg);
                    if (action == null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEventDoMachineAction(action, this, this.AnalysisContext));
                }
                else if (type.ToString().Equals(typeof(Microsoft.PSharp.OnEventGotoState).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 3)
                {
                    var arg = attribute.ArgumentList.Arguments[2];
                    var action = this.GetActionFromAttributeArgument(arg);
                    if (action == null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEventGotoMachineAction(action, this, this.AnalysisContext));
                }
                else if (type.ToString().Equals(typeof(Microsoft.PSharp.OnEventPushState).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 3)
                {
                    var arg = attribute.ArgumentList.Arguments[2];
                    var action = this.GetActionFromAttributeArgument(arg);
                    if (action == null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEventPushMachineAction(action, this, this.AnalysisContext));
                }
            }
        }

        /// <summary>
        /// Returns the action from the given attribute argument.
        /// </summary>
        /// <param name="attribute">AttributeArgumentSyntax</param>
        /// <returns>MethodDeclarationSyntax</returns>
        private MethodDeclarationSyntax GetActionFromAttributeArgument(AttributeArgumentSyntax arg)
        {
            MethodDeclarationSyntax action = null;

            string actionName = "";
            if (arg.Expression is InvocationExpressionSyntax)
            {
                var invocation = arg.Expression as InvocationExpressionSyntax;
                if (!(invocation.Expression is IdentifierNameSyntax) ||
                    !(invocation.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("nameof") ||
                    invocation.ArgumentList.Arguments.Count != 1)
                {
                    return action;
                }

                var param = invocation.ArgumentList.Arguments[0];
                var identifier = this.AnalysisContext.GetIdentifier(param.Expression);
                actionName = identifier.Identifier.ValueText;
            }
            else if (arg.Expression is LiteralExpressionSyntax)
            {
                var literal = arg.Expression as LiteralExpressionSyntax;
                actionName = literal.ToString();
            }

            action = this.Machine.Declaration.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(m => m.Identifier.ValueText.Equals(actionName)).FirstOrDefault();

            return action;
        }

        #endregion
    }
}
