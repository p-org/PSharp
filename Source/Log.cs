//-----------------------------------------------------------------------
// <copyright file="Log.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PSharp
{
    internal class Log
    {
        #region fields

        /// <summary>
        /// Error trace.
        /// </summary>
        internal List<Tuple<string, string, int>> ErrorTrace;

        /// <summary>
        /// Call trace.
        /// </summary>
        internal List<Tuple<BaseMethodDeclarationSyntax, ExpressionSyntax>> CallTrace;

        /// <summary>
        /// The method where the log begins.
        /// </summary>
        internal string Method;

        /// <summary>
        /// The machine where the log begins.
        /// </summary>
        internal string Machine;

        /// <summary>
        /// The state where the log begins.
        /// </summary>
        internal string State;

        /// <summary>
        /// The corresponding payload of the log.
        /// </summary>
        internal string Payload;

        #endregion

        #region methods

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal Log()
        {
            this.ErrorTrace = new List<Tuple<string, string, int>>();
            this.CallTrace = new List<Tuple<BaseMethodDeclarationSyntax, ExpressionSyntax>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">State</param>
        /// <param name="payload">Payload</param>
        internal Log(MethodDeclarationSyntax method, ClassDeclarationSyntax machine,
            ClassDeclarationSyntax state, ExpressionSyntax payload)
        {
            this.ErrorTrace = new List<Tuple<string, string, int>>();
            this.CallTrace = new List<Tuple<BaseMethodDeclarationSyntax, ExpressionSyntax>>();

            if (method == null)
            {
                this.Method = null;
            }
            else
            {
                this.Method = method.Identifier.ValueText;
            }

            if (machine == null)
            {
                this.Machine = null;
            }
            else
            {
                this.Machine = machine.Identifier.ValueText;
            }

            if (state == null)
            {
                this.State = null;
            }
            else
            {
                this.State = state.Identifier.ValueText;
            }

            if (payload == null)
            {
                this.Payload = null;
            }
            else
            {
                this.Payload = payload.ToString();
            }
        }

        /// <summary>
        /// Adds new trace to the log's error trace.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="file">File</param>
        /// <param name="line">Line</param>
        internal void AddTrace(string expr, string file, int line)
        {
            this.ErrorTrace.Add(new Tuple<string, string, int>(expr, file, line));
        }

        /// <summary>
        /// Inserts a new call to the log's call trace.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="call">Call</param>
        internal void InsertCall(BaseMethodDeclarationSyntax method, InvocationExpressionSyntax call)
        {
            this.CallTrace.Insert(0, new Tuple<BaseMethodDeclarationSyntax,
                ExpressionSyntax>(method, call));
        }

        /// <summary>
        /// Inserts a new call to the log's call trace.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="call">Call</param>
        internal void InsertCall(BaseMethodDeclarationSyntax method, ObjectCreationExpressionSyntax call)
        {
            this.CallTrace.Insert(0, new Tuple<BaseMethodDeclarationSyntax,
                ExpressionSyntax>(method, call));
        }

        /// <summary>
        /// Merges another log to the current log.
        /// </summary>
        /// <param name="log">Log</param>
        internal void Merge(Log log)
        {
            this.ErrorTrace.AddRange(log.ErrorTrace);
            this.CallTrace.AddRange(log.CallTrace);
            this.Method = log.Method;
            this.Machine = log.Machine;
            this.State = log.State;
            this.Payload = log.Payload;
        }

        #endregion
    }
}
