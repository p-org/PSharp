//-----------------------------------------------------------------------
// <copyright file="CallTraceStep.cs">
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

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing a call trace step.
    /// </summary>
    internal class CallTraceStep
    {
        #region fields

        /// <summary>
        /// The method declaration.
        /// </summary>
        internal readonly BaseMethodDeclarationSyntax Method;

        /// <summary>
        /// The invocation expression.
        /// </summary>
        internal readonly ExpressionSyntax Invocation;

        #endregion

        #region methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="invocation">Invocation</param>
        internal CallTraceStep(BaseMethodDeclarationSyntax method, ExpressionSyntax invocation)
        {
            this.Method = method;
            this.Invocation = invocation;
        }

        #endregion
    }
}
