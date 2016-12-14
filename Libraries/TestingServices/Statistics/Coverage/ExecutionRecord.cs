//-----------------------------------------------------------------------
// <copyright file="ExecutionRecord.cs">
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

namespace Microsoft.PSharp.TestingServices.Coverage
{
    /// <summary>
    /// Record of a specific testing iteration. Records are captured when
    /// the user invokes the runtime method 'RecordExecution'.
    /// </summary>
    internal class ExecutionRecord
    {
        #region fields

        /// <summary>
        /// Caller name who invoked RecordExecution.
        /// </summary>
        internal string MemberName;

        /// <summary>
        /// Source file of invoked RecordExecution.
        /// </summary>
        internal string SourceFile;

        /// <summary>
        /// Line number of invoked RecordExecution.
        /// </summary>
        internal int LineNumber;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="member">Caller name</param>
        /// <param name="file">File name</param>
        /// <param name="line">Line number</param>
        public ExecutionRecord(string member, string file, int line)
        {
            this.MemberName = member;
            this.SourceFile = file;
            this.LineNumber = line;
        }

        #endregion
    }
}