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
        /// Initializes a new instance of the <see cref="QualifiedMethod"/> class.
        /// </summary>
        public QualifiedMethod(string name, string machineName, string namespaceName)
        {
            this.Name = name;
            this.MachineName = machineName;
            this.NamespaceName = namespaceName;
        }
    }
}
