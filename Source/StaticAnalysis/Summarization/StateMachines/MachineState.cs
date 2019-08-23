using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.DataFlowAnalysis;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# machine state.
    /// </summary>
    internal sealed class MachineState
    {
        /// <summary>
        /// The analysis context.
        /// </summary>
        private readonly AnalysisContext AnalysisContext;

        /// <summary>
        /// The parent state-machine.
        /// </summary>
        private readonly StateMachine Machine;

        /// <summary>
        /// Name of the state.
        /// </summary>
        internal string Name;

        /// <summary>
        /// The underlying declaration.
        /// </summary>
        internal ClassDeclarationSyntax Declaration;

        /// <summary>
        /// List of state-transitions in the state.
        /// </summary>
        internal List<StateTransition> StateTransitions;

        /// <summary>
        /// List of actions in the state.
        /// </summary>
        internal List<MachineAction> MachineActions;

        /// <summary>
        /// True if this is the start state.
        /// </summary>
        internal bool IsStart;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineState"/> class.
        /// </summary>
        internal MachineState(ClassDeclarationSyntax classDecl, StateMachine machine, AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.Machine = machine;
            this.Name = AnalysisContext.GetFullClassName(classDecl);
            this.Declaration = classDecl;
            this.StateTransitions = new List<StateTransition>();
            this.MachineActions = new List<MachineAction>();
            this.IsStart = this.IsStartState();
        }

        /// <summary>
        /// Analyzes the state.
        /// </summary>
        internal void Analyze()
        {
            this.FindAllActions();
            this.FindAllTransitions();
        }

        /// <summary>
        /// Returns the successor states.
        /// </summary>
        internal HashSet<MachineState> GetSuccessorStates()
        {
            var successors = new HashSet<MachineState>();

            var queue = new Queue<MachineState>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var state = queue.Dequeue();

                foreach (var transition in state.StateTransitions.Where(
                    val => !successors.Contains(val.TargetState)))
                {
                    successors.Add(transition.TargetState);
                    queue.Enqueue(transition.TargetState);
                }
            }

            return successors;
        }

        /// <summary>
        /// Checks if this is the start state.
        /// </summary>
        private bool IsStartState()
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(this.Machine.Declaration.SyntaxTree);

            var attributes = this.Declaration.AttributeLists.SelectMany(var => var.Attributes);
            foreach (var attribute in attributes)
            {
                var type = model.GetTypeInfo(attribute.Name).Type;
                if (type.ToString().Equals(typeof(StartAttribute).FullName))
                {
                    return true;
                }
            }

            return false;
        }

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
                if (type.ToString().Equals(typeof(OnEntryAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 1)
                {
                    var arg = attribute.ArgumentList.Arguments[0];
                    var action = this.GetActionFromExpression(arg.Expression);
                    if (action is null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEntryMachineAction(action, this));
                }
                else if (type.ToString().Equals(typeof(OnExitAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 1)
                {
                    var arg = attribute.ArgumentList.Arguments[0];
                    var action = this.GetActionFromExpression(arg.Expression);
                    if (action is null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnExitMachineAction(action, this));
                }
                else if (type.ToString().Equals(typeof(OnEventDoActionAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 2)
                {
                    var arg = attribute.ArgumentList.Arguments[1];
                    var action = this.GetActionFromExpression(arg.Expression);
                    if (action is null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEventDoMachineAction(action, this));
                }
                else if (type.ToString().Equals(typeof(OnEventGotoStateAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 3)
                {
                    var arg = attribute.ArgumentList.Arguments[2];
                    var action = this.GetActionFromExpression(arg.Expression);
                    if (action is null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEventGotoMachineAction(action, this));
                }
                else if (type.ToString().Equals(typeof(OnEventPushStateAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 3)
                {
                    var arg = attribute.ArgumentList.Arguments[2];
                    var action = this.GetActionFromExpression(arg.Expression);
                    if (action is null)
                    {
                        continue;
                    }

                    this.MachineActions.Add(new OnEventPushMachineAction(action, this));
                }
            }
        }

        /// <summary>
        /// Finds all state-transitions for each state-machine in the project.
        /// </summary>
        private void FindAllTransitions()
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(this.Machine.Declaration.SyntaxTree);

            var attributes = this.Declaration.AttributeLists.SelectMany(var => var.Attributes);
            foreach (var attribute in attributes)
            {
                var type = model.GetTypeInfo(attribute.Name).Type;
                if (type.ToString().Equals(typeof(OnEventGotoStateAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 2)
                {
                    var arg = attribute.ArgumentList.Arguments[1];
                    var state = this.GetStateFromExpression(arg.Expression, model);
                    if (state is null)
                    {
                        continue;
                    }

                    this.StateTransitions.Add(new StateTransition(state, this, this.AnalysisContext));
                }
                else if (type.ToString().Equals(typeof(OnEventPushStateAttribute).FullName) &&
                    attribute.ArgumentList.Arguments.Count == 2)
                {
                    var arg = attribute.ArgumentList.Arguments[1];
                    var state = this.GetStateFromExpression(arg.Expression, model);
                    if (state is null)
                    {
                        continue;
                    }

                    this.StateTransitions.Add(new StateTransition(state, this, this.AnalysisContext));
                }
            }

            foreach (var action in this.MachineActions)
            {
                if (action.MethodDeclaration.Body is null)
                {
                    continue;
                }

                var invocations = action.MethodDeclaration.Body.DescendantNodesAndSelf(val => true).
                    OfType<InvocationExpressionSyntax>();
                foreach (var invocation in invocations)
                {
                    var callSymbol = model.GetSymbolInfo(invocation).Symbol;
                    if (callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine") &&
                        callSymbol.Name.Equals("Goto"))
                    {
                        var arg = invocation.ArgumentList.Arguments[0];
                        var state = this.GetStateFromExpression(arg.Expression, model);
                        if (state is null)
                        {
                            continue;
                        }

                        this.StateTransitions.Add(new StateTransition(state, this, this.AnalysisContext));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the action from the given expression.
        /// </summary>
        private MethodDeclarationSyntax GetActionFromExpression(ExpressionSyntax expr)
        {
            MethodDeclarationSyntax action = null;

            string actionName = string.Empty;
            if (expr is InvocationExpressionSyntax)
            {
                var invocation = expr as InvocationExpressionSyntax;
                if (!(invocation.Expression is IdentifierNameSyntax) ||
                    !(invocation.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("nameof") ||
                    invocation.ArgumentList.Arguments.Count != 1)
                {
                    return action;
                }

                var param = invocation.ArgumentList.Arguments[0];
                var identifier = AnalysisContext.GetIdentifier(param.Expression);
                actionName = identifier.Identifier.ValueText;
            }
            else if (expr is LiteralExpressionSyntax)
            {
                var literal = expr as LiteralExpressionSyntax;
                actionName = literal.ToString();
            }

            action = this.Machine.Declaration.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(m => m.Identifier.ValueText.Equals(actionName)).FirstOrDefault();

            return action;
        }

        /// <summary>
        /// Returns the state from the given expression.
        /// </summary>
        private MachineState GetStateFromExpression(ExpressionSyntax expr, SemanticModel model)
        {
            MachineState state = null;

            if (expr is TypeOfExpressionSyntax)
            {
                var type = model.GetTypeInfo((expr as TypeOfExpressionSyntax).Type).Type;
                state = this.Machine.MachineStates.FirstOrDefault(val => val.Name.Equals(type.ToString()));
            }

            return state;
        }
    }
}
