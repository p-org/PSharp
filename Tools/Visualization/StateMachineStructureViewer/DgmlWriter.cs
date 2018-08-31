using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    class DgmlWriter
    {

        public static void WriteMachines(NamespaceDeclaration ndecl, XmlTextWriter writer)
        {
            foreach (var mdecl in ndecl.MachineDeclarations)
            {
                writer.WriteStartElement("Node");
                writer.WriteAttributeString("Id", mdecl.Identifier.Text);
                writer.WriteAttributeString("Group", "Expanded");
                writer.WriteEndElement();
            }
        }
        public static void WriteMachineStates(MachineDeclaration mdecl, XmlTextWriter writer)
        {
            var machine = mdecl.Identifier.Text;
            foreach (var sdecl in mdecl.GetAllStateDeclarations())
            {
                writer.WriteStartElement("Node");
                writer.WriteAttributeString("Id", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                writer.WriteAttributeString("Label", sdecl.Identifier.Text);

                if ( /*TODO*/ true)
                {
                    writer.WriteAttributeString("Ignores", string.Join(", ", sdecl.IgnoredEvents.Select(s => s.Text)));
                    writer.WriteAttributeString("Defers", string.Join(", ", sdecl.DeferredEvents.Select(s => s.Text)));
                    writer.WriteAttributeString("Handles", string.Join(", ", sdecl.ActionBindings.Keys.Select(s => s.Text)));
                }

                writer.WriteEndElement();
            }
        }

        public static void WriteMachineStateLinks(MachineDeclaration mdecl, XmlTextWriter writer)
        {
            var machine = mdecl.Identifier.Text;
            foreach (var sdecl in mdecl.GetAllStateDeclarations())
            {
                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", machine);
                writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                writer.WriteAttributeString("Category", "Contains");
                writer.WriteEndElement();
            }
        }

        public static void WriteStateTransitions(MachineDeclaration mdecl, XmlTextWriter writer)
        {
            var machine = mdecl.Identifier.Text;
            foreach (var sdecl in mdecl.GetAllStateDeclarations())
            {
                foreach (var kvp in sdecl.GotoStateTransitions)
                {
                    string targetState = string.Join(".", kvp.Value.Select(s => s.Text));
                    writer.WriteStartElement("Link");
                    writer.WriteAttributeString("Source", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                    writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, targetState));
                    writer.WriteAttributeString("Category", "GotoTransition");
                    writer.WriteAttributeString("Label", kvp.Key.Text);
                    writer.WriteEndElement();
                }

                foreach (var kvp in sdecl.PushStateTransitions)
                {
                    string targetState = string.Join(".", kvp.Value.Select(s => s.Text));
                    writer.WriteStartElement("Link");
                    writer.WriteAttributeString("Source", string.Format("{0}::{1}", machine, sdecl.GetFullyQualifiedName('.')));
                    writer.WriteAttributeString("Target", string.Format("{0}::{1}", machine, targetState));
                    writer.WriteAttributeString("Category", "PushTransition");
                    writer.WriteAttributeString("Label", kvp.Key.Text);
                    writer.WriteEndElement();
                }
            }
        }

        


    }
}
