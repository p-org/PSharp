// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// An abstract P# visitor.
    /// </summary>
    internal abstract class BaseVisitor
    {
        #region fields

        /// <summary>
        /// The P# project.
        /// </summary>
        protected PSharpProject Project;

        /// <summary>
        /// The error log.
        /// </summary>
        protected List<Tuple<SyntaxToken, string>> ErrorLog;

        /// <summary>
        /// The warning log.
        /// </summary>
        protected List<Tuple<SyntaxToken, string>> WarningLog;

        #endregion

        #region protected API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        /// <param name="warningLog">Warning log</param>
        protected BaseVisitor(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
        {
            this.Project = project;
            this.ErrorLog = errorLog;
            this.WarningLog = warningLog;
        }

        #endregion
    }
}
