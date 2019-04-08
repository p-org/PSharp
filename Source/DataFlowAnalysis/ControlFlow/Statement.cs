// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a statement.
    /// </summary>
    public class Statement
    {
        /// <summary>
        /// The syntax node of the statement.
        /// </summary>
        public readonly SyntaxNode SyntaxNode;

        /// <summary>
        /// The control-flow graph node that contains
        /// the statement.
        /// </summary>
        public readonly IControlFlowNode ControlFlowNode;

        /// <summary>
        /// Handle to the summary of the method
        /// that contains the statement.
        /// </summary>
        public MethodSummary Summary { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Statement"/> class.
        /// </summary>
        private Statement(SyntaxNode syntaxNode, IControlFlowNode node, MethodSummary summary)
        {
            this.SyntaxNode = syntaxNode;
            this.ControlFlowNode = node;
            this.Summary = summary;
        }

        /// <summary>
        /// Creates a new statement.
        /// </summary>
        public static Statement Create(SyntaxNode syntaxNode, IControlFlowNode node, MethodSummary summary)
        {
            return new Statement(syntaxNode, node, summary);
        }

        /// <summary>
        /// Checks if the statement is in the same method as the specified statement.
        /// </summary>
        public bool IsInSameMethodAs(Statement statement)
        {
            return this.Summary.Id == statement.Summary.Id;
        }

        /// <summary>
        /// Checks if the statement is in the same method as the specified control-flow graph node.
        /// </summary>
        public bool IsInSameMethodAs(IControlFlowNode node)
        {
            return this.Summary.Id == node.Summary.Id;
        }

        /// <summary>
        /// Determines if the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            Statement stmt = obj as Statement;
            if (stmt is null)
            {
                return false;
            }

            if (!this.SyntaxNode.Equals(stmt.SyntaxNode) ||
                !this.ControlFlowNode.Equals(stmt.ControlFlowNode))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 19;

                hash = (hash * 31) + this.SyntaxNode.GetHashCode();
                hash = (hash * 31) + this.ControlFlowNode.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString() => "[" + this.SyntaxNode + "]::[cfg::" + this.ControlFlowNode + "]";
    }
}
