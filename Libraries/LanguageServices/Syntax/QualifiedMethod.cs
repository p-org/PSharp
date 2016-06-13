//-----------------------------------------------------------------------
// <copyright file="QualifiedMethod.cs">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Defines a qualified method.
    /// </summary>
    internal sealed class QualifiedMethod
    {
        /// <summary>
        /// Name of the method.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Name of the machine that contains the method.
        /// </summary>
        public readonly string MachineName;

        /// <summary>
        /// Name of the namespace that contains the method.
        /// </summary>
        public readonly string NamespaceName;

        /// <summary>
        /// The tokenized qualified state name of the method.
        /// </summary>
        public List<string> QualifiedStateName;

        /// <summary>
        /// The qualified state names of the machine that
        /// contains the method.
        /// </summary>
        public HashSet<string> MachineQualifiedStateNames;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="machineName">Machine name</param>
        /// <param name="namespaceName">Namespace name</param>
        public QualifiedMethod(string name, string machineName, string namespaceName)
        {
            this.Name = name;
            this.MachineName = machineName;
            this.NamespaceName = namespaceName;

            this.QualifiedStateName = new List<string>();
            this.MachineQualifiedStateNames = new HashSet<string>();
        }
    }
}
