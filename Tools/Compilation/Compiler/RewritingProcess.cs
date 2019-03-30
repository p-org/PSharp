// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# rewriting process.
    /// </summary>
    internal sealed class RewritingProcess
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        private readonly CompilationContext CompilationContext;

        /// <summary>
        /// Creates a P# rewriting process.
        /// </summary>
        public static RewritingProcess Create(CompilationContext context)
        {
            return new RewritingProcess(context);
        }

        /// <summary>
        /// Starts the P# parsing process.
        /// </summary>
        public void Start()
        {
            Output.WriteLine(". Rewriting");

            // Creates and runs a P# rewriting engine.
            RewritingEngine.Create(this.CompilationContext).Run();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingProcess"/> class.
        /// </summary>
        private RewritingProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }
    }
}
