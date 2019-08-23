using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Rewriting.PSharp;
using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P# program.
    /// </summary>
    public sealed class PSharpProgram : AbstractPSharpProgram
    {
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

        /// <summary>
        /// The projection buffer information for mapping between the original
        /// P# buffer and the rewritten C# buffer in this program.
        /// </summary>
        public ProjectionTree ProjectionTree { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpProgram"/> class.
        /// </summary>
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

            this.ProjectionTree = new ProjectionTree(this);

            // Convert from P# structural syntax to attributed C# classes and relevant methods.
            this.RewriteStructuresToAttributedClassesAndMethods();
            this.ProjectionTree.OnInitialRewriteComplete();

            // Set up for code term offset adjustments now that we have completed the initial pass.
            this.RewrittenCodeTermBatch = new RewrittenTermBatch(this.ProjectionTree.OrderedCSharpProjectionNodes);

            // Convert from P# keywords etc. to C# syntax and insert additional 'using' references.
            this.RewriteTermsAndInsertLibraries();
            this.ProjectionTree.OnFinalRewriteComplete();

            if (Debug.IsEnabled)
            {
                CompilationContext.PrintSyntaxTree(this.GetSyntaxTree());
            }
        }

        /// <summary>
        /// Add a record of a rewritten term (type, statement, expression) from P# to C#.
        /// </summary>
        public override void AddRewrittenTerm(SyntaxNode node, string rewrittenText) => this.RewrittenCodeTermBatch.AddToBatch(node, rewrittenText);

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
            new PushStateRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new PopRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();
            new AssertRewriter(this).Rewrite();
            this.MergeRewrittenTermBatch();

            var qualifiedMethods = this.GetResolvedRewrittenQualifiedMethods();
            new TypeofRewriter(this).Rewrite(qualifiedMethods);
            this.MergeRewrittenTermBatch();
            new GenericTypeRewriter(this).Rewrite(qualifiedMethods);
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
                this.ProjectionTree.AddRootChild(node.ProjectionNode);
                node.ProjectionNode.SetOffsetInParent(text.Length);
                text += newLine;
                node.Rewrite(indentLevel);
                text += node.TextUnit.Text;
                newLine = "\n";
            }

            this.ProjectionTree.FinalizeInitialOffsets(0, text);
            this.UpdateSyntaxTree(text);
        }

        private void RewriteTermsAndInsertLibraries()
        {
            this.RewriteTypes();
            this.RewriteStatements();
            this.RewriteExpressions();

            // Adding additional libraries will change offsets.
            var originalLength = this.GetSyntaxTree().Length;
            this.InsertLibraries();
            var text = this.GetSyntaxTree().ToString();
            this.ProjectionTree.UpdateRewrittenCSharpText(text);
            this.RewrittenCodeTermBatch.OffsetStarts(text.Length - originalLength);
        }

        private void MergeRewrittenTermBatch()
        {
            this.ProjectionTree.UpdateRewrittenCSharpText(this.GetSyntaxTree().ToString());
            this.RewrittenCodeTermBatch.MergeBatch();
        }

        /// <summary>
        /// Inserts the P# libraries.
        /// </summary>
        private void InsertLibraries()
        {
            var list = new List<UsingDirectiveSyntax>();
            var otherUsings = this.GetSyntaxTree().GetCompilationUnitRoot().Usings;
            var psharpLib = CreateLibrary("Microsoft.PSharp");

            list.Add(psharpLib);
            list.AddRange(otherUsings);

            // Add an additional newline to the last 'using' to separate from the namespace.
            list[list.Count - 1] = list.Last().WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("\n\n")));

            var root = this.GetSyntaxTree().GetCompilationUnitRoot()
                .WithUsings(SyntaxFactory.List(list));
            this.UpdateSyntaxTree(root.SyntaxTree.ToString());
        }

        /// <summary>
        /// Resolves and returns the rewritten qualified methods of this program.
        /// </summary>
        private HashSet<QualifiedMethod> GetResolvedRewrittenQualifiedMethods()
        {
            var qualifiedMethods = new HashSet<QualifiedMethod>();
            foreach (var ns in this.NamespaceDeclarations)
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
        private void BasicTypeChecking()
        {
            foreach (var nspace in this.NamespaceDeclarations)
            {
                foreach (var machine in nspace.MachineDeclarations)
                {
                    machine.CheckDeclaration();
                }
            }
        }
    }
}
