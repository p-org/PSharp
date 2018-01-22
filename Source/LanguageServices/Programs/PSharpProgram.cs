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

        /// <summary>
        /// List of rewritten code terms (terms within a code chunk).
        /// </summary>
        private RewrittenTermBatch RewrittenCodeTermBatch;

        #endregion

        #region properties

        /// <summary>
        /// The Projection Buffer information for mapping between the original P# buffer and the rewritten
        /// C# buffer in this program.
        /// </summary>
        public ProjectionInfos ProjectionInfos { get; private set; }

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
            this.BasicTypeChecking();

            this.ProjectionInfos = new ProjectionInfos(this);

            // Convert from P# structural syntax to attributed C# classes and relevant methods.
            this.RewriteStructuresToAttributedClassesAndMethods();
            this.ProjectionInfos.OnInitialRewriteComplete();

            // Set up for code term offset adjustments now that we have completed the initial pass.
            this.RewrittenCodeTermBatch = new RewrittenTermBatch(this.ProjectionInfos.OrderedProjectionInfos);

            // Convert from P# keywords etc. to C# syntax and insert additional 'using' references.
            RewriteTermsAndInsertLibraries();
            this.ProjectionInfos.OnFinalRewriteComplete();

            if (Debug.IsEnabled)
            {
                base.GetProject().CompilationContext.PrintSyntaxTree(base.GetSyntaxTree());
            }
        }

        /// <summary>
        /// Add a record of a rewritten term (type, statement, expression) from P# to C#.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="rewrittenText"></param>
        public override void AddRewrittenTerm(SyntaxNode node, string rewrittenText)
        {
            this.RewrittenCodeTermBatch.AddToBatch(node, rewrittenText);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the P# types to C#.
        /// </summary>
        private void RewriteTypes()
        {
            new MachineTypeRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new HaltEventRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
        }

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        private void RewriteStatements()
        {
            new CreateMachineRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new CreateRemoteMachineRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new SendRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new MonitorRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new RaiseRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new GotoStateRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new PopRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new AssertRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();

            var qualifiedMethods = this.GetResolvedRewrittenQualifiedMethods();
            new TypeofRewriter(this).Rewrite(qualifiedMethods);
            this.MergeRewrittenTermBatch();
        }

        /// <summary>
        /// Rewrites the P# expressions to C#.
        /// </summary>
        private void RewriteExpressions()
        {
            new TriggerRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new CurrentStateRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new ThisRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new RandomChoiceRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
        }

        private void RewriteStructuresToAttributedClassesAndMethods()
        {
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
                this.ProjectionInfos.Root.AddChild(node.ProjectionInfo);
                node.ProjectionInfo.SetOffsetInParent(text.Length);
                text += newLine;
                node.Rewrite(indentLevel);
                text += node.TextUnit.Text;
                newLine = "\n";
            }
            this.ProjectionInfos.FinalizeInitialOffsets(0, text);

            base.UpdateSyntaxTree(text);
        }

        private void RewriteTermsAndInsertLibraries()
        {
            this.RewriteTypes();
            this.RewriteStatements();
            this.RewriteExpressions();

            // Adding additional libraries will change offsets.
            var originalLength = base.GetSyntaxTree().Length;
            this.InsertLibraries();
            var text = base.GetSyntaxTree().ToString();
            this.ProjectionInfos.UpdateRewrittenCSharpText(text);
            this.RewrittenCodeTermBatch.OffsetStarts(text.Length - originalLength);
        }

        private void MergeRewrittenTermBatch()
        {
            this.ProjectionInfos.UpdateRewrittenCSharpText(base.GetSyntaxTree().ToString());
            this.RewrittenCodeTermBatch.MergeBatch();
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
