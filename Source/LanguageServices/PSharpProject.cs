﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        #region fields

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

        #endregion

        #region API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PSharpProject()
        {
            this.CompilationContext = CompilationContext.Create();
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        public PSharpProject(CompilationContext context)
        {
            this.CompilationContext = context;
            this.PSharpPrograms = new List<PSharpProgram>();
            this.CSharpPrograms = new List<CSharpProgram>();
            this.ProgramMap = new Dictionary<IPSharpProgram, SyntaxTree>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="projectName">Project name</param>
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
        /// <param name="options">ParsingOptions</param>
        public void Parse(ParsingOptions options)
        {
            var project = this.CompilationContext.GetProjectWithName(this.Name);
            var compilation = project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (this.CompilationContext.IsPSharpFile(tree))
                {
                    this.ParsePSharpSyntaxTree(tree, options);
                }
                else if (this.CompilationContext.IsCSharpFile(tree))
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
            foreach (var kvp in this.ProgramMap)
            {
                this.RewriteProgram(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Returns the compilation of the project.
        /// </summary>
        /// <returns>Compilation</returns>
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
        /// <param name="identifier">IdentifierNameSyntax</param>
        /// <returns>Boolean</returns>
        internal bool IsMachineType(SyntaxToken identifier)
        {
            var result = this.PSharpPrograms.Any(p => p.NamespaceDeclarations.Any(n => n.MachineDeclarations.Any(
                m => m.Identifier.TextUnit.Text.Equals(identifier.ValueText))));

            return result;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Parses a P# syntax tree to C#.
        /// th
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
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
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
        private void ParseCSharpSyntaxTree(SyntaxTree tree, ParsingOptions options)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var program = new CSharpParser(this, tree, options).Parse();

            this.CSharpPrograms.Add(program as CSharpProgram);
            this.ProgramMap.Add(program, tree);
        }

        /// <summary>
        /// Rewrites a P# program to C#.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="tree">SyntaxTree</param>
        private void RewriteProgram(IPSharpProgram program, SyntaxTree tree)
        {
            program.Rewrite();
        }

        #endregion
    }
}
