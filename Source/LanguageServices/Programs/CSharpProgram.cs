// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Rewriting.CSharp;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A C# program.
    /// </summary>
    public sealed class CSharpProgram : AbstractPSharpProgram
    {
        /// <summary>
        /// List of event identifiers.
        /// </summary>
        internal List<ClassDeclarationSyntax> EventIdentifiers;

        /// <summary>
        /// List of machine identifiers.
        /// </summary>
        internal List<ClassDeclarationSyntax> MachineIdentifiers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpProgram"/> class.
        /// </summary>
        public CSharpProgram(PSharpProject project, SyntaxTree tree)
            : base(project, tree)
        {
            this.EventIdentifiers = new List<ClassDeclarationSyntax>();
            this.MachineIdentifiers = new List<ClassDeclarationSyntax>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public override void Rewrite()
        {
            this.RewriteStatements();
            this.PerformCustomRewriting();

            if (Debug.IsEnabled)
            {
                CompilationContext.PrintSyntaxTree(this.GetSyntaxTree());
            }
        }

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        private void RewriteStatements()
        {
            // new RaiseRewriter(this).Rewrite();
            // new GotoStateRewriter(this).Rewrite();
            // new PopRewriter(this).Rewrite();
        }

        /// <summary>
        /// Performs custom rewriting.
        /// </summary>
        private void PerformCustomRewriting()
        {
            Queue<Type> rewritingPasses = new Queue<Type>();
            Dictionary<Type, List<Type>> passDependencies = new Dictionary<Type, List<Type>>();
            HashSet<Queue<Type>> snapshot = new HashSet<Queue<Type>>();

            foreach (var assembly in this.Project.CompilationContext.CustomCompilerPassAssemblies)
            {
                foreach (var pass in FindCustomRewritingPasses(assembly, typeof(CustomCSharpRewritingPassAttribute)))
                {
                    rewritingPasses.Enqueue(pass);
                    passDependencies.Add(pass, FindDependenciesOfPass(pass));
                }
            }

            while (rewritingPasses.Count > 0)
            {
                Type nextPass = rewritingPasses.Dequeue();

                bool allDependenciesDone = true;
                foreach (var dependency in passDependencies[nextPass])
                {
                    if (rewritingPasses.Contains(dependency))
                    {
                        allDependenciesDone = false;
                        break;
                    }
                }

                if (!allDependenciesDone)
                {
                    rewritingPasses.Enqueue(nextPass);

                    if (snapshot.Any(item => item.SequenceEqual(rewritingPasses)))
                    {
                        Error.ReportAndExit("Possible cycle in the rewriting " +
                            "pass dependencies, or dependency missing.");
                    }

                    snapshot.Add(rewritingPasses);
                    continue;
                }

                CSharpRewriter rewriter = null;

                try
                {
                    rewriter = Activator.CreateInstance(nextPass, this) as CSharpRewriter;
                }
                catch (MissingMethodException)
                {
                    Error.ReportAndExit($"Public constructor of {nextPass} not found.");
                }

                rewriter.Rewrite();
            }
        }

        /// <summary>
        /// Finds the custom rewriting passes with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        private static List<Type> FindCustomRewritingPasses(Assembly assembly, Type attribute)
        {
            List<Type> passes = null;

            try
            {
                passes = assembly.GetTypes().Where(m => m.GetCustomAttributes(attribute, false).Length > 0).ToList();
            }
            catch (Exception ex)
            {
                throw new RewritingException($"Failed to load assembly '{assembly.FullName}'", ex);
            }

            return passes;
        }

        /// <summary>
        /// Finds the dependencies of the specified pass.
        /// </summary>
        private static List<Type> FindDependenciesOfPass(Type pass)
        {
            var result = new List<Type>();

            if (pass.IsDefined(typeof(RewritingPassDependencyAttribute), false))
            {
                var dependencyAttribute = pass.GetCustomAttribute(typeof(RewritingPassDependencyAttribute), false)
                    as RewritingPassDependencyAttribute;
                result.AddRange(dependencyAttribute.Dependencies);
            }

            return result;
        }
    }
}
