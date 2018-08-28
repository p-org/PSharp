//-----------------------------------------------------------------------
// <copyright file="PSharpProgram.cs">
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

            var text = "";
            const int indentLevel = 0;

            foreach (var node in this.UsingDeclarations)
            {
                node.Rewrite(indentLevel);
                text += node.TextUnit.Text;
            }

            var newLine = "";
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
                        writer.WriteAttributeString("Id", string.Format("{0}::{1}", machine, sdecl.Identifier.Text));
                        writer.WriteAttributeString("Label", sdecl.Identifier.Text);
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
                        writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, sdecl.Identifier.Text));
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
                            writer.WriteStartElement("Link");
                            writer.WriteAttributeString("Source", string.Format("{0}::{1}", machine, sdecl.Identifier.Text));
                            writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, kvp.Value[0].Text));
                            writer.WriteAttributeString("Label", kvp.Key.Text);
                            writer.WriteEndElement();
                        }
                    }
                }
            }
            // Ends Links element.
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
