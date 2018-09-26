using Microsoft.PSharp.LanguageServices.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.StateDiagramViewer
{
    class EventInfo
    {
        #region fields and constructor
        public readonly string uniqueName;
        private readonly NamespaceDeclaration namespaceDeclaration;
        private readonly MachineInfo machineInfo;
        private readonly EventDeclaration eventDeclaration;
        

        public EventInfo(EventDeclaration edecl, MachineInfo machineInfo)
        {
            this.eventDeclaration = edecl;
            this.machineInfo = machineInfo;
            this.uniqueName = ResolutionHelper.CreateUniqueNameForEventIdentifier(machineInfo.machineDeclaration, edecl.Identifier.Text);
            this.namespaceDeclaration = null;  // Access through machineInfo instead
        }

        public EventInfo(EventDeclaration edecl, NamespaceDeclaration ns)
        {
            this.eventDeclaration = edecl;
            this.machineInfo = null;
            this.uniqueName = ResolutionHelper.CreateUniqueNameForEventIdentifier(ns.QualifiedName, edecl.Identifier.Text);
            this.namespaceDeclaration = ns;
        }
        #endregion
    }
}
