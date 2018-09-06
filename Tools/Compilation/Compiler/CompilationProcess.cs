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
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private ILogger Logger;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# compilation process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="logger">ILogger</param>
        /// <returns>CompilationProcess</returns>
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

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="logger">ILogger</param>
        private CompilationProcess(CompilationContext context, ILogger logger)
        {
            this.CompilationContext = context;
            this.Logger = logger;
        }

        #endregion
    }
}
