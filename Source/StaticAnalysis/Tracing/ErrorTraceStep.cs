// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing an error trace step.
    /// </summary>
    internal class ErrorTraceStep
    {
        #region fields

        /// <summary>
        /// The expression.
        /// </summary>
        internal readonly string Expression;

        /// <summary>
        /// The file name.
        /// </summary>
        internal readonly string File;

        /// <summary>
        /// The line number.
        /// </summary>
        internal readonly int Line;

        #endregion

        #region methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="file">File</param>
        /// <param name="line">Line</param>
        internal ErrorTraceStep(string expr, string file, int line)
        {
            this.Expression = expr;
            this.File = file;
            this.Line = line;
        }

        #endregion
    }
}
