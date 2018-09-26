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
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# rewriting process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>RewritingProcess</returns>
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

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private RewritingProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
