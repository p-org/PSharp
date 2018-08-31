using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.LanguageServices.Syntax;
namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    /// <summary>
    /// Class for resolution of tokens
    /// </summary>
    class MachineResolver
    {


        #region fields
        internal Dictionary<string, MachineInfo> machineLookup;
        #endregion

        #region singleton

        private static readonly object singletonLock = new object();
        private static MachineResolver singletonInstance = null;
        public static MachineResolver Instance()
        {
            if (singletonInstance == null)
            {
                lock (singletonLock)
                {
                    if (singletonInstance == null)
                    {
                        singletonInstance = new MachineResolver();
                    }
                }
            }
            return singletonInstance;

        }

        // Prevent multiple instances? 
        private MachineResolver() { machineLookup = new Dictionary<string, MachineInfo>(); }
        #endregion

        #region token utils
        public static List<string> GetActiveNamespacesFromUsingDirectives(PSharpProgram prog)
        {
            return prog.UsingDeclarations.Select( 
                usingDirective => String.Join("", 
                    usingDirective.IdentifierTokens.Select( token => token.Text )
                    ) 
                ).ToList();
        }
        public static string baseTypeTokenListToIdentifier(List<Token> baseTypeTokenList)
        {
            return String.Join("", baseTypeTokenList.Select( x => x.Text) );
        }

        /* TODO: Can these be made private? */
        public static string CreateUniqueName(MachineDeclaration machineDecl)
        {
            return CreateUniqueName(machineDecl.Namespace.QualifiedName, machineDecl.Identifier.Text);
        }

        public static string CreateUniqueName(string namespaceName, string IdentifierName)
        {
            return namespaceName + '.' + IdentifierName;
        }


        public static string CreateUniqueName(EventDeclaration edecl)
        {
            return CreateUniqueName(CreateUniqueName(edecl.Machine), edecl.Identifier.Text);
        }
        #endregion
        

        #region api

        public void PopulateLookup(PSharpProgram prog)
        {
            foreach (var ns in prog.NamespaceDeclarations)
            {
                foreach (var mdecl in ns.MachineDeclarations)
                {
                    MachineInfo machine = new MachineInfo(mdecl, prog);
                    machineLookup.Add(machine.uniqueName, machine);
                }
            }
        }


        /// <summary>
        /// Resolves the name and returns the unique name of the resolved identifier
        /// </summary>
        /// <param name="name">The name to lookup</param>
        /// <param name="currentNamespace">The current namespace</param>
        /// <param name="activeNamespaces">A list of namespaces made active by 'using' declaration</param>
        /// <returns>The unique name of the resolved identifier</returns>
        public MachineInfo LookupMachine(string name, string currentNamespace, List<string> activeNamespaces)
        {
            // Check local scope
            string localScopedName = CreateUniqueName(currentNamespace, name);
            if ( machineLookup.ContainsKey(localScopedName))
            {   
                return machineLookup[localScopedName];
            }
            
            // Check global scope / Fully Qualified
            if (machineLookup.ContainsKey(name))
            {
                return machineLookup[name];
            }

            // Check within active namespaces
            var activeNamespaceResults = activeNamespaces.Where(
                    namespaceName => machineLookup.ContainsKey(namespaceName + '.' + name )
                ).ToList();

            if (activeNamespaceResults.Count == 1) 
            {
                // Found one!
                return machineLookup[CreateUniqueName(activeNamespaceResults.First(), name)];
            }
            else if (activeNamespaceResults.Count > 1)
            {
                // Too many
                throw new Exception("Multiple candidates for name " + name);
            }
            else 
            {   // None
                return null;
            }

        }
        #endregion

        #region private methods

        #endregion

    }
}
