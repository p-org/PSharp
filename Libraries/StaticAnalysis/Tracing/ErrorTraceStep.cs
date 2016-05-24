//-----------------------------------------------------------------------
// <copyright file="ErrorTraceStep.cs">
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
