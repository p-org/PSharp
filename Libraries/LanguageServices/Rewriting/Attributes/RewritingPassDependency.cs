//-----------------------------------------------------------------------
// <copyright file="RewritingPassDependency.cs">
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

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// Attribute for custom C# rewriting pass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RewritingPassDependency : Attribute
    {
        /// <summary>
        /// Pass dependencies.
        /// </summary>
        internal Type[] Dependencies;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dependencies">Dependencies</param>
        public RewritingPassDependency(params Type[] dependencies)
        {
            this.Dependencies = dependencies;
        }
    }
}
