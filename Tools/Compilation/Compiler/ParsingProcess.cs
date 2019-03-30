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
        /// <summary>
        /// The compilation context.
        /// </summary>
        private readonly CompilationContext CompilationContext;

        /// <summary>
        /// Creates a P# parsing process.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingProcess"/> class.
        /// </summary>
        private ParsingProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }
    }
}
