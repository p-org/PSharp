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
    /// A P# parsing process.
    /// </summary>
    internal sealed class ParsingProcess
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# parsing process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>ParsingProcess</returns>
        public static ParsingProcess Create(CompilationContext context)
        {
            return new ParsingProcess(context);
        }

        /// <summary>
        /// Starts the P# parsing process.
        /// </summary>
        public void Start()
        {
            Output.WriteLine(". Parsing");

            // Creates the P# parsing options.
            ParsingOptions options = ParsingOptions.CreateDefault()
                .EnableExitOnError().DisableThrowParsingException();

            // Creates and runs a P# parsing engine.
            ParsingEngine.Create(this.CompilationContext, options).Run();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private ParsingProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
