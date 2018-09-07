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
    class ResolutionHelper
    {


        #region fields
        internal Dictionary<string, MachineInfo> machineLookup;
        internal Dictionary<string, StateInfo> stateLookup;
        internal Dictionary<string, EventInfo> eventLookup;

        #endregion

        #region singleton

        private static readonly object singletonLock = new object();
        private static ResolutionHelper singletonInstance = null;
        public static ResolutionHelper Instance()
        {
            if (singletonInstance == null)
            {
                lock (singletonLock)
                {
                    if (singletonInstance == null)
                    {
                        singletonInstance = new ResolutionHelper();
                    }
                }
            }
            return singletonInstance;

        }

        // Prevent multiple instances? 
        private ResolutionHelper() {
            machineLookup = new Dictionary<string, MachineInfo>();
            stateLookup = new Dictionary<string, StateInfo>();
            eventLookup = new Dictionary<string, EventInfo>();
        }
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
            return CreateUniqueNameForMachineIdentifier(machineDecl.Namespace, machineDecl.Identifier.Text );
            //machineDecl.Namespace.QualifiedName +  machineDecl.Identifier.Text;
        }

        public static string CreateUniqueName(MachineDeclaration machineContext, StateGroupDeclaration stateGroupContext)
        {
            string stateGroupNameFull = "";
            for (var sg = stateGroupContext; sg != null; sg = sg.Group)
            {
                stateGroupNameFull += '.' + sg.Identifier.Text;
            }
            return CreateUniqueName(machineContext) + stateGroupNameFull;
        }

        public static string CreateUniqueName(StateDeclaration sdecl)
        {
            return CreateUniqueNameForStateIdentifier(sdecl.Machine, sdecl.Group, sdecl.Identifier.Text);
            //return CreateUniqueName(sdecl.Machine) + sdecl.GetFullyQualifiedName('.');
        }

        public static string CreateUniqueName(EventDeclaration edecl)
        {
            return CreateUniqueNameForEventIdentifier(edecl.Machine, edecl.Identifier.Text);
            //return CreateUniqueName(edecl.Machine) + edecl.Identifier.Text;
        }


        public static string CreateUniqueNameForMachineIdentifier(NamespaceDeclaration namespaceContext, string identifierText)
        {
            return namespaceContext.QualifiedName + '.' + identifierText;
        }

        public static string CreateUniqueNameForMachineIdentifier(string namespaceName, string identifierText)
        {
            return namespaceName + '.' + identifierText;
        }

        public static string CreateUniqueNameForStateIdentifier(MachineDeclaration machineContext, StateGroupDeclaration stateGroupContext, string identifierText)
        {
            return CreateUniqueName(machineContext, stateGroupContext) + '.' + identifierText;
        }

        public static string CreateUniqueNameForEventIdentifier(MachineDeclaration machineContext, string identifierText)
        {
            return CreateUniqueName(machineContext) + '.' + identifierText;
        }

        public static string CreateUniqueNameForEventIdentifier(string namespaceName, string identifierText)
        {
            return namespaceName + '.' + identifierText;
        }
        #endregion


        #region populate api

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


        internal void populateStates(MachineInfo machineInfo)
        {
            foreach (StateDeclaration sdecl in machineInfo.machineDeclaration.StateDeclarations)
            {
                StateInfo sInfo = new StateInfo(sdecl, machineInfo);
                stateLookup.Add(sInfo.uniqueName, sInfo);
            }
        }

        internal void populateEvents(MachineInfo machineInfo)
        {
            foreach (EventDeclaration edecl in machineInfo.machineDeclaration.EventDeclarations)
            {
                // This may have to be static, since we don't know where the Machine declaration ends and the event declaration starts.
                EventInfo eInfo = new EventInfo(edecl, machineInfo);
                eventLookup.Add(eInfo.uniqueName, eInfo);
            }
        }


        #endregion

        #region lookup api
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
            string localScopedName = CreateUniqueNameForMachineIdentifier(currentNamespace, name);
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
                    namespaceName => machineLookup.ContainsKey( CreateUniqueNameForMachineIdentifier(namespaceName, name) )
                ).ToList();

            if (activeNamespaceResults.Count == 1) 
            {
                // Found one!
                return machineLookup[CreateUniqueNameForMachineIdentifier(activeNamespaceResults.First(), name)];
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

        public StateInfo LookupState(string name, MachineDeclaration machineContext, StateGroupDeclaration stateGroupContext)
        {
            for(StateGroupDeclaration sg = stateGroupContext;  sg != null ; sg = sg.Group)
            {
                string lookupKey = CreateUniqueNameForStateIdentifier(machineContext, sg, name);
                if( stateLookup.ContainsKey(lookupKey) )
                {
                    return stateLookup[lookupKey];
                }
                
            }
            // Check with a null state group
            string lookupKeyNoStateGroup = CreateUniqueNameForStateIdentifier(machineContext, null, name);
            if ( stateLookup.ContainsKey(lookupKeyNoStateGroup) )
            {
                return stateLookup[lookupKeyNoStateGroup];
            }
            else
            {
                return null;
            }
        }


        public EventInfo LookupEvent(string name, MachineDeclaration machineContext)
        {
            string lookupKey = CreateUniqueNameForEventIdentifier(machineContext, name);
            if (eventLookup.ContainsKey(lookupKey))
            {
                return eventLookup[lookupKey];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves the name to an event and returns the unique name of the resolved event
        /// </summary>
        /// <param name="name">The name to lookup</param>
        /// <param name="currentNamespace">The current namespace</param>
        /// <param name="activeNamespaces">A list of namespaces made active by 'using' declaration</param>
        /// <returns>The unique name of the resolved identifier</returns>
        public EventInfo LookupEvent(string name, string currentNamespace, List<string> activeNamespaces)
        {
            // Check local scope
            string localScopedName = CreateUniqueNameForEventIdentifier(currentNamespace, name);
            if (eventLookup.ContainsKey(localScopedName))
            {
                return eventLookup[localScopedName];
            }

            // Check global scope / Fully Qualified
            if (eventLookup.ContainsKey(name))
            {
                return eventLookup[name];
            }

            // Check within active namespaces
            var activeNamespaceResults = activeNamespaces.Where(
                    namespaceName => eventLookup.ContainsKey(CreateUniqueNameForMachineIdentifier(namespaceName, name))
                ).ToList();

            if (activeNamespaceResults.Count == 1)
            {
                // Found one!
                return eventLookup[CreateUniqueNameForMachineIdentifier(activeNamespaceResults.First(), name)];
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
