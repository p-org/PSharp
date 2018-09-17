// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Syntax;
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;
using System.Xml;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P# program.
    /// </summary>
    public sealed class PSharpProgram : AbstractPSharpProgram
    {
        #region fields
        
        /// <summary>
        /// List of using declarations.
        /// </summary>
        internal List<UsingDeclaration> UsingDeclarations;

        /// <summary>
        /// List of namespace declarations.
        /// </summary>
        internal List<NamespaceDeclaration> NamespaceDeclarations;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        public PSharpProgram(PSharpProject project, SyntaxTree tree)
            : base(project, tree)
        {
            this.UsingDeclarations = new List<UsingDeclaration>();
            this.NamespaceDeclarations = new List<NamespaceDeclaration>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public override void Rewrite()
        {
            // Perform sanity checking on the P# program.
            BasicTypeChecking();

            var text = string.Empty;
            const int indentLevel = 0;

            foreach (var node in this.UsingDeclarations)
            {
                node.Rewrite(indentLevel);
                text += node.TextUnit.Text;
            }

            var newLine = string.Empty;
            foreach (var node in this.NamespaceDeclarations)
            {
                text += newLine;
                node.Rewrite(indentLevel);
                text += node.TextUnit.Text;
                newLine = "\n";
            }

            base.UpdateSyntaxTree(text);

            this.RewriteTypes();
            this.RewriteStatements();
            this.RewriteExpressions();

            this.InsertLibraries();

            if (Debug.IsEnabled)
            {
                base.GetProject().CompilationContext.PrintSyntaxTree(base.GetSyntaxTree());
            }
        }

        /// <summary>
        /// Emits dgml representation of the state machine structure
        /// </summary>
        /// <param name="writer">XmlTestWriter</param>
        public override void EmitStateMachineStructure(XmlTextWriter writer)
        {
            // Starts document.
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;

            // Starts DirectedGraph element.
            writer.WriteStartElement("DirectedGraph", @"http://schemas.microsoft.com/vs/2009/dgml");

            // Starts Nodes element.
            writer.WriteStartElement("Nodes");

            // Iterates machines.
            foreach (var ndecl in this.NamespaceDeclarations)
            {
                foreach (var mdecl in ndecl.MachineDeclarations)
                {
                    writer.WriteStartElement("Node");
                    writer.WriteAttributeString("Id", mdecl.Identifier.Text);
                    writer.WriteAttributeString("Group", "Expanded");
                    writer.WriteEndElement();
                }
            }

            // Iterates states.
            foreach (var ndecl in this.NamespaceDeclarations)
            {
                foreach (var mdecl in ndecl.MachineDeclarations)
                {
                    var machine = mdecl.Identifier.Text;
                    foreach (var sdecl in mdecl.GetAllStateDeclarations())
                    {
                        writer.WriteStartElement("Node");
                        writer.WriteAttributeString("Id", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                        writer.WriteAttributeString("Label", sdecl.Identifier.Text);

                        if ( /*TODO*/ true)
                        {
                            writer.WriteAttributeString("Ignores", string.Join(", ", sdecl.IgnoredEvents.Select(s => s.Text)));
                            writer.WriteAttributeString("Defers", string.Join(", ", sdecl.DeferredEvents.Select(s => s.Text)));
                            writer.WriteAttributeString("Handles", string.Join(", ", sdecl.ActionBindings.Keys.Select(s => s.Text)));
                        }

                        writer.WriteEndElement();
                    }
                }
            }

            // Ends Nodes element.
            writer.WriteEndElement();

            // Starts Links element.
            writer.WriteStartElement("Links");

            // Iterates states.
            foreach (var ndecl in this.NamespaceDeclarations)
            {
                foreach (var mdecl in ndecl.MachineDeclarations)
                {
                    var machine = mdecl.Identifier.Text;
                    foreach (var sdecl in mdecl.GetAllStateDeclarations())
                    {
                        writer.WriteStartElement("Link");
                        writer.WriteAttributeString("Source", machine);
                        writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                        writer.WriteAttributeString("Category", "Contains");
                        writer.WriteEndElement();
                    }
                }
            }

            // Iterates state annotations.
            foreach (var ndecl in this.NamespaceDeclarations)
            {
                foreach (var mdecl in ndecl.MachineDeclarations)
                {
                    var machine = mdecl.Identifier.Text;
                    foreach (var sdecl in mdecl.GetAllStateDeclarations())
                    {
                        foreach (var kvp in sdecl.GotoStateTransitions)
                        {
                            string targetState = string.Join(".", kvp.Value.Select(s => s.Text));
                            writer.WriteStartElement("Link");
                            writer.WriteAttributeString("Source", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                            writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, targetState));
                            writer.WriteAttributeString("Category", "GotoTransition");
                            writer.WriteAttributeString("Label", kvp.Key.Text);
                            writer.WriteEndElement();
                        }

                        foreach (var kvp in sdecl.PushStateTransitions)
                        {
                            string targetState = string.Join(".", kvp.Value.Select(s => s.Text));
                            writer.WriteStartElement("Link");
                            writer.WriteAttributeString("Source", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                            writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, targetState));
                            writer.WriteAttributeString("Category", "PushTransition");
                            writer.WriteAttributeString("Label", kvp.Key.Text);
                            writer.WriteEndElement();
                        }
                    }

                }
            }
            // Ends Links element.
            writer.WriteEndElement();

            // Starts Properties element.
            writer.WriteStartElement("Properties");
            // Define custom properties to show Ignored, Deferred and Handled events
            string[] customProperties = {
                "Ignores", "Defers", "Handles",
            };
            foreach ( string propertyName in  customProperties ){
                writer.WriteStartElement("Property");
                writer.WriteAttributeString("Id", propertyName);
                writer.WriteAttributeString("DataType", "System.String");
                writer.WriteEndElement();
            }
            // Ends Properties element.
            writer.WriteEndElement();

            // Starts Categories element
            writer.WriteStartElement("Categories");

            writer.WriteStartElement("Category");
            writer.WriteAttributeString("Id", "GotoTransition");
            writer.WriteEndElement();
            writer.WriteStartElement("Category");
            writer.WriteAttributeString("Id", "PushTransition");
            writer.WriteAttributeString("StrokeDashArray", "2");
            writer.WriteEndElement();

            // Ends Categories element.
            writer.WriteEndElement();

            // Ends DirectedGraph element.
            writer.WriteEndElement();

            // Ends document.
            writer.WriteEndDocument();

        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the P# types to C#.
        /// </summary>
        private void RewriteTypes()
        {
            new MachineTypeRewriter(this).Rewrite();
            new HaltEventRewriter(this).Rewrite();
        }

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        private void RewriteStatements()
        {
            new CreateMachineRewriter(this).Rewrite();
            new CreateRemoteMachineRewriter(this).Rewrite();
            new SendRewriter(this).Rewrite();
            new MonitorRewriter(this).Rewrite();
            new RaiseRewriter(this).Rewrite();
            new GotoStateRewriter(this).Rewrite();
            new PushStateRewriter(this).Rewrite();
            new PopRewriter(this).Rewrite();
            new AssertRewriter(this).Rewrite();

            var qualifiedMethods = this.GetResolvedRewrittenQualifiedMethods();
            new TypeofRewriter(this).Rewrite(qualifiedMethods);
            new GenericTypeRewriter(this).Rewrite(qualifiedMethods);
        }

        /// <summary>
        /// Rewrites the P# expressions to C#.
        /// </summary>
        private void RewriteExpressions()
        {
            new TriggerRewriter(this).Rewrite();
            new CurrentStateRewriter(this).Rewrite();
            new ThisRewriter(this).Rewrite();
            new RandomChoiceRewriter(this).Rewrite();
        }

        /// <summary>
        /// Inserts the P# libraries.
        /// </summary>
        private void InsertLibraries()
        {
            var list = new List<UsingDirectiveSyntax>();
            var otherUsings = base.GetSyntaxTree().GetCompilationUnitRoot().Usings;
            var psharpLib = base.CreateLibrary("Microsoft.PSharp");
            
            list.Add(psharpLib);
            list.AddRange(otherUsings);

            // Add an additional newline to the last 'using' to separate from the namespace.
            list[list.Count - 1] = list.Last().WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("\n\n")));

            var root = base.GetSyntaxTree().GetCompilationUnitRoot()
                .WithUsings(SyntaxFactory.List(list));
            base.UpdateSyntaxTree(root.SyntaxTree.ToString());
        }

        /// <summary>
        /// Resolves and returns the rewritten qualified methods of this program.
        /// </summary>
        /// <returns>QualifiedMethods</returns>
        private HashSet<QualifiedMethod> GetResolvedRewrittenQualifiedMethods()
        {
            var qualifiedMethods = new HashSet<QualifiedMethod>();
            foreach (var ns in NamespaceDeclarations)
            {
                foreach (var machine in ns.MachineDeclarations)
                {
                    var allQualifiedNames = new HashSet<string>();
                    foreach (var state in machine.GetAllStateDeclarations())
                    {
                        allQualifiedNames.Add(state.GetFullyQualifiedName('.'));
                    }

                    foreach (var method in machine.RewrittenMethods)
                    {
                        method.MachineQualifiedStateNames = allQualifiedNames;
                        qualifiedMethods.Add(method);
                    }
                }
            }

            return qualifiedMethods;
        }

        /// <summary>
        /// Perform basic type checking of the P# program.
        /// </summary>
        /// <returns>QualifiedMethods</returns>
        private void BasicTypeChecking()
        {
            foreach (var nspace in NamespaceDeclarations)
            {
                foreach (var machine in nspace.MachineDeclarations)
                {
                    machine.CheckDeclaration();
                }
            }
        }
        #endregion
    }
}
