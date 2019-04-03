// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# compilation process.
    /// </summary>
    internal sealed class CompilationProcess
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        private readonly CompilationContext CompilationContext;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// Creates a P# compilation process.
        /// </summary>
        public static CompilationProcess Create(CompilationContext context, ILogger logger)
        {
            return new CompilationProcess(context, logger);
        }

        /// <summary>
        /// Starts the P# compilation process.
        /// </summary>
        public void Start()
        {
            if (this.CompilationContext.Configuration.CompilationTarget == CompilationTarget.Testing)
            {
                Output.WriteLine($". Compiling ({this.CompilationContext.Configuration.CompilationTarget})");
            }
            else
            {
                Output.WriteLine($". Compiling ({this.CompilationContext.Configuration.CompilationTarget}::" +
                    $"{this.CompilationContext.Configuration.OptimizationTarget})");
            }

            // Creates and runs a P# compilation engine.
            CompilationEngine.Create(this.CompilationContext, this.Logger).Run();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationProcess"/> class.
        /// </summary>
        private CompilationProcess(CompilationContext context, ILogger logger)
        {
            this.CompilationContext = context;
            this.Logger = logger;
        }
    }
}
