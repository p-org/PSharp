using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A P# project.
    /// </summary>
    public sealed class PSharpProject
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        internal CompilationContext CompilationContext;

        /// <summary>
        /// The P# project name.
        /// </summary>
        internal string Name;

        /// <summary>
        /// List of P# programs in the project.
        /// </summary>
        internal List<PSharpProgram> PSharpPrograms;

        /// <summary>
        /// List of C# programs in the project.
        /// </summary>
        internal List<CSharpProgram> CSharpPrograms;

        /// <summary>
        /// Map from P# programs to syntax trees.
        /// </summary>
        internal Dictionary<IPSharpProgram, SyntaxTree> ProgramMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpProject"/> class.
        /// </summary>
        public PSharpProject()
        {
            this.CompilationContext = CompilationContext.Create();
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpProject"/> class.
        /// </summary>
        public PSharpProject(CompilationContext context)
        {
            this.CompilationContext = context;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpProject"/> class.
        /// </summary>
        public PSharpProject(CompilationContext context, string projectName)
        {
            this.CompilationContext = context;
            this.Name = projectName;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Parses the project.
        /// </summary>
        public void Parse(ParsingOptions options)
        {
            var project = this.CompilationContext.GetProjectWithName(this.Name);
            var compilation = project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (CompilationContext.IsPSharpFile(tree))
                {
                    this.ParsePSharpSyntaxTree(tree, options);
                }
                else if (CompilationContext.IsCSharpFile(tree))
                {
                    this.ParseCSharpSyntaxTree(tree, options);
                }
            }
        }

        /// <summary>
        /// Rewrites the P# project to the C#-IR.
        /// </summary>
        public void Rewrite()
        {
            foreach (var program in this.ProgramMap.Keys)
            {
                program.Rewrite();
            }
        }

        /// <summary>
        /// Returns the compilation of the project.
        /// </summary>
        public CodeAnalysis.Compilation GetCompilation()
        {
            var project = this.CompilationContext.GetProjectWithName(this.Name);
            if (project != null)
            {
                return project.GetCompilationAsync().Result;
            }

            return null;
        }

        /// <summary>
        /// Is the identifier a machine type.
        /// </summary>
        internal bool IsMachineType(SyntaxToken identifier)
        {
            var result = this.PSharpPrograms.Any(p => p.NamespaceDeclarations.Any(n => n.MachineDeclarations.Any(
                m => m.Identifier.TextUnit.Text.Equals(identifier.ValueText))));

            return result;
        }

        /// <summary>
        /// Parses a P# syntax tree to C#.
        /// th
        /// </summary>
        private void ParsePSharpSyntaxTree(SyntaxTree tree, ParsingOptions options)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var tokens = new PSharpLexer().Tokenize(root.ToFullString());
            var program = new PSharpParser(this, tree, options).ParseTokens(tokens);

            this.PSharpPrograms.Add(program as PSharpProgram);
            this.ProgramMap.Add(program, tree);
        }

        /// <summary>
        /// Parses a C# syntax tree to C#.
        /// th
        /// </summary>
        private void ParseCSharpSyntaxTree(SyntaxTree tree, ParsingOptions options)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var program = new CSharpParser(this, tree, options).Parse();

            this.CSharpPrograms.Add(program as CSharpProgram);
            this.ProgramMap.Add(program, tree);
        }
    }
}
