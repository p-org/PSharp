// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Compilation;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A P# rewriting engine.
    /// </summary>
    public sealed class RewritingEngine
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# rewriting engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>RewritingEngine</returns>
        public static RewritingEngine Create(CompilationContext context)
        {
            return new RewritingEngine(context);
        }

        /// <summary>
        /// Runs the P# rewriting engine.
        /// </summary>
        /// <returns>RewritingEngine</returns>
        public RewritingEngine Run()
        {
            // Rewrite the projects for the active compilation target.
            for (int idx = 0; idx < this.CompilationContext.GetProjects().Count; idx++)
            {
                this.CompilationContext.GetProjects()[idx].Rewrite();
            }

            return this;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private RewritingEngine(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
