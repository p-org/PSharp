//-----------------------------------------------------------------------
// <copyright file="BaseVisitor.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
