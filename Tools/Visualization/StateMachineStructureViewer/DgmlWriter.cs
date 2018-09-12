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
        private readonly static char[] unfriendlyNameSeparators = { '.' };
        private static string FriendlyName(string uniqueName)
        {
            return uniqueName.Split(unfriendlyNameSeparators).Last();
        }

        public static void WriteAll(IEnumerable<MachineInfo> machines, XmlTextWriter writer)
        {

            writer.WriteStartElement("DirectedGraph", @"http://schemas.microsoft.com/vs/2009/dgml");

            writer.WriteStartElement("Nodes");
            writer.WriteComment(" Start Machines ");
            foreach (MachineInfo mInfo in machines)
            {
                WriteMachine(mInfo, writer);
            }
            writer.WriteComment(" End Machines ");
            // Move on to the states within the machines
            writer.WriteComment(" Start States");
            foreach (MachineInfo mInfo in machines)
            {
                WriteMachineStates(mInfo, writer);
            }
            writer.WriteComment(" End States ");
            writer.WriteEndElement(/*"Nodes"*/);

            // On to the edges

            writer.WriteStartElement("Links");
            foreach (MachineInfo mInfo in machines)
            {
                WriteMachineStateLinks(mInfo, writer);
            }

            foreach (MachineInfo mInfo in machines)
            {
                foreach(string stateName in mInfo.GetAllStates())
                {
                    WriteStateTransitions(ResolutionHelper.Instance().GetState(stateName), writer);
                }
            }
            writer.WriteEndElement(/*"Links"*/);

            WriteAppendix(writer);

            writer.WriteEndElement(/*DirectedGraph*/);
        }

        private static void WriteAppendix(XmlTextWriter writer)
        {
            // Properties : Define custom properties to show Ignored, Deferred and Handled events
            writer.WriteStartElement("Properties");
            
            string[] customProperties = {
                "Ignores", "Defers", "Handles",
            };
            foreach (string propertyName in customProperties)
            {
                writer.WriteStartElement("Property");
                writer.WriteAttributeString("Id", propertyName);
                writer.WriteAttributeString("DataType", "System.String");
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement(/*"Properties"*/);
            

            // Categories
            writer.WriteStartElement("Categories");

            writer.WriteStartElement("Category");
            writer.WriteAttributeString("Id", "GotoTransition");
            writer.WriteEndElement();
            writer.WriteStartElement("Category");
            writer.WriteAttributeString("Id", "PushTransition");
            writer.WriteAttributeString("StrokeDashArray", "2");
            writer.WriteEndElement(/*"Category"*/);

            writer.WriteEndElement(/*"Categories"*/);
        }

        public static void WriteMachine(MachineInfo machineInfo, XmlTextWriter writer)
        {
            writer.WriteStartElement("Node");
            writer.WriteAttributeString("Id", machineInfo.uniqueName);
            writer.WriteAttributeString("Group", "Expanded");
            writer.WriteEndElement(/*"Node"*/);
        }
        public static void WriteMachineStateLinks(MachineInfo mInfo, XmlTextWriter writer)
        {
            var machine = mInfo.uniqueName;
            foreach (string stateName in mInfo.GetAllStates() )
            {
                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", machine);
                writer.WriteAttributeString("Target", stateName);
                writer.WriteAttributeString("Category", "Contains");
                writer.WriteEndElement(/*"Link"*/);
            }
        }


        public static void WriteMachineStates(MachineInfo machineInfo, XmlTextWriter writer)
        {
            var machine = machineInfo.uniqueName;
            writer.WriteComment(String.Format("Start states for Machine '{0}'", machineInfo.uniqueName));
            foreach (string stateName in machineInfo.GetAllStates())
            {
                StateInfo stateInfo = ResolutionHelper.Instance().GetState( stateName );
                StateDeclaration sdecl = stateInfo.stateDeclaration;
                writer.WriteStartElement("Node");
                writer.WriteAttributeString("Id", stateName);
                writer.WriteAttributeString("Label", FriendlyName(stateName) );

                if ( /*TODO*/ true)
                {
                    writer.WriteAttributeString("Ignores", string.Join(", ", sdecl.IgnoredEvents.Select(s => s.Text)));
                    writer.WriteAttributeString("Defers", string.Join(", ", sdecl.DeferredEvents.Select(s => s.Text)));
                    writer.WriteAttributeString("Handles", string.Join(", ", sdecl.ActionBindings.Keys.Select(s => s.Text)));
                }

                writer.WriteEndElement();
            }
            writer.WriteComment(String.Format("End states for Machine '{0}'", machineInfo.uniqueName));
        }

        public static void WriteStateTransitions(StateInfo sInfo, XmlTextWriter writer) {

            string sourceStateName = sInfo.uniqueName;
            writer.WriteComment(String.Format("Start outgoing transitions from {0}", sourceStateName));
            foreach (var kvp in sInfo.GetGotoTransitions())
            {
                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", sourceStateName);
                writer.WriteAttributeString("Target", kvp.Value);
                writer.WriteAttributeString("Category", "GotoTransition");
                writer.WriteAttributeString("Label", FriendlyName(kvp.Key) );
                writer.WriteEndElement();
            }

            foreach (var kvp in sInfo.GetPushTransitions())
            {
                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", sourceStateName);
                writer.WriteAttributeString("Target", kvp.Value);
                writer.WriteAttributeString("Category", "PushTransition");
                writer.WriteAttributeString("Label", FriendlyName(kvp.Key));
                writer.WriteEndElement();
            }
            writer.WriteComment(String.Format("End outgoing transitions from {0}", sourceStateName));
        }
        

        


    }
}
