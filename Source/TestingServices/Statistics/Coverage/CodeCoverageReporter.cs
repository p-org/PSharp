//-----------------------------------------------------------------------
// <copyright file="CodeCoverageReporter.cs">
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Microsoft.PSharp.TestingServices.Coverage
{
    /// <summary>
    /// The P# code coverage reporter.
    /// </summary>
    internal class CodeCoverageReporter
    {
        #region fields

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        private readonly CoverageInfo CoverageInfo;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="coverageInfo">CoverageInfo</param>
        public CodeCoverageReporter(CoverageInfo coverageInfo)
        {
            this.CoverageInfo = coverageInfo;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Emits the visualization graph.
        /// </summary>
        /// <param name="graphFile">Graph file</param>
        public void EmitVisualizationGraph(string graphFile)
        {
            using (var writer = new XmlTextWriter(graphFile, Encoding.UTF8))
            {
                this.WriteVisualizationGraph(writer);
            }
        }

        /// <summary>
        /// Emits the code coverage report.
        /// </summary>
        /// <param name="coverageFile">Code coverage file</param>
        public void EmitCoverageReport(string coverageFile)
        {
            using (var writer = new StreamWriter(coverageFile))
            {
                this.WriteCoverageFile(writer);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Writes the visualization graph.
        /// </summary>
        /// <param name="writer">XmlTextWriter</param>
        private void WriteVisualizationGraph(XmlTextWriter writer)
        {
            // Starts document.
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;

            // Starts DirectedGraph element.
            writer.WriteStartElement("DirectedGraph", @"http://schemas.microsoft.com/vs/2009/dgml");

            // Starts Nodes element.
            writer.WriteStartElement("Nodes");

            // Iterates machines.
            foreach (var machine in this.CoverageInfo.MachinesToStates.Keys)
            {
                writer.WriteStartElement("Node");
                writer.WriteAttributeString("Id", machine);
                writer.WriteAttributeString("Group", "Expanded");
                writer.WriteEndElement();
            }

            // Iterates states.
            foreach (var tup in this.CoverageInfo.MachinesToStates)
            {
                var machine = tup.Key;
                foreach (var state in tup.Value)
                {
                    writer.WriteStartElement("Node");
                    writer.WriteAttributeString("Id", this.GetStateId(machine, state));
                    writer.WriteAttributeString("Label", state);
                    writer.WriteEndElement();
                }
            }

            // Ends Nodes element.
            writer.WriteEndElement();

            // Starts Links element.
            writer.WriteStartElement("Links");

            // Iterates states.
            foreach (var tup in this.CoverageInfo.MachinesToStates)
            {
                var machine = tup.Key;
                foreach (var state in tup.Value)
                {
                    writer.WriteStartElement("Link");
                    writer.WriteAttributeString("Source", machine);
                    writer.WriteAttributeString("Target", this.GetStateId(machine, state));
                    writer.WriteAttributeString("Category", "Contains");
                    writer.WriteEndElement();
                }
            }

            var parallelEdgeCounter = new Dictionary<Tuple<string, string>, int>();
            // Iterates transitions.
            foreach (var transition in this.CoverageInfo.Transitions)
            {
                var source = this.GetStateId(transition.MachineOrigin, transition.StateOrigin);
                var target = this.GetStateId(transition.MachineTarget, transition.StateTarget);
                var counter = 0;
                if (parallelEdgeCounter.ContainsKey(Tuple.Create(source, target)))
                {
                    counter = parallelEdgeCounter[Tuple.Create(source, target)];
                    parallelEdgeCounter[Tuple.Create(source, target)] = counter + 1;
                }
                else
                {
                    parallelEdgeCounter[Tuple.Create(source, target)] = 1;
                }

                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", source);
                writer.WriteAttributeString("Target", target);
                writer.WriteAttributeString("Label", transition.EdgeLabel);
                if(counter != 0)
                {
                    writer.WriteAttributeString("Index", counter.ToString());
                }
                writer.WriteEndElement();
            }

            // Ends Links element.
            writer.WriteEndElement();

            // Ends DirectedGraph element.
            writer.WriteEndElement();

            // Ends document.
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Writes the visualization graph.
        /// </summary>
        /// <param name="writer">TextWriter</param>
        private void WriteCoverageFile(TextWriter writer)
        {
            var machines = new List<string>(this.CoverageInfo.MachinesToStates.Keys);

            #region registered event coverage

            var uncoveredEvents = new HashSet<Tuple<string, string, string>>(this.CoverageInfo.RegisteredEvents);
            foreach (var transition in this.CoverageInfo.Transitions)
            {
                if (transition.MachineOrigin == transition.MachineTarget)
                {
                    uncoveredEvents.Remove(Tuple.Create(transition.MachineOrigin, transition.StateOrigin, transition.EdgeLabel));
                }
                else
                {
                    uncoveredEvents.Remove(Tuple.Create(transition.MachineTarget, transition.StateTarget, transition.EdgeLabel));
                }
            }

            writer.WriteLine("Total event coverage: {0}%",
                this.CoverageInfo.RegisteredEvents.Count == 0 ? "100" : 
                ((this.CoverageInfo.RegisteredEvents.Count - uncoveredEvents.Count) * 100.0 /
                this.CoverageInfo.RegisteredEvents.Count).ToString("F1"));

            // Map from machines to states to registered events
            var machineToStatesToEvents = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            machines.ForEach(m => machineToStatesToEvents.Add(m, new Dictionary<string, HashSet<string>>()));
            machines.ForEach(m =>
            {
                foreach (var state in this.CoverageInfo.MachinesToStates[m])
                {
                    machineToStatesToEvents[m].Add(state, new HashSet<string>());
                }
            });

            foreach (var ev in this.CoverageInfo.RegisteredEvents)
            {
                machineToStatesToEvents[ev.Item1][ev.Item2].Add(ev.Item3);
            }

            #endregion

            // Map from machine to its outgoing transitions
            var machineToOutgoingTransitions = new Dictionary<string, List<Transition>>();
            // Map from machine to its incoming transitions
            var machineToIncomingTransitions = new Dictionary<string, List<Transition>>();
            // Map from machine to intra-machine transitions
            var machineToIntraTransitions = new Dictionary<string, List<Transition>>();

            machines.ForEach(m => machineToIncomingTransitions.Add(m, new List<Transition>()));
            machines.ForEach(m => machineToOutgoingTransitions.Add(m, new List<Transition>()));
            machines.ForEach(m => machineToIntraTransitions.Add(m, new List<Transition>()));

            foreach (var tr in this.CoverageInfo.Transitions)
            {
                if (tr.MachineOrigin == tr.MachineTarget)
                {
                    machineToIntraTransitions[tr.MachineOrigin].Add(tr);
                }
                else
                {
                    machineToIncomingTransitions[tr.MachineTarget].Add(tr);
                    machineToOutgoingTransitions[tr.MachineOrigin].Add(tr);
                }
            }

            // Per-machine data
            foreach (var machine in machines)
            {
                writer.WriteLine("Machine: {0}", machine);
                writer.WriteLine("***************");

                #region registered event coverage

                var machineUncoveredEvents = new Dictionary<string, HashSet<string>>();
                foreach (var state in this.CoverageInfo.MachinesToStates[machine])
                {
                    machineUncoveredEvents.Add(state, new HashSet<string>(machineToStatesToEvents[machine][state]));
                }

                foreach (var tr in machineToIncomingTransitions[machine])
                {
                    machineUncoveredEvents[tr.StateTarget].Remove(tr.EdgeLabel);
                }
                foreach (var tr in machineToIntraTransitions[machine])
                {
                    machineUncoveredEvents[tr.StateOrigin].Remove(tr.EdgeLabel);
                }

                var numTotalEvents = 0;
                foreach (var tup in machineToStatesToEvents[machine])
                {
                    numTotalEvents += tup.Value.Count;
                }

                var numUncoveredEvents = 0;
                foreach (var tup in machineUncoveredEvents)
                {
                    numUncoveredEvents += tup.Value.Count;
                }

                writer.WriteLine("Machine event coverage: {0}%", 
                    numTotalEvents == 0 ? "100" :
                    ((numTotalEvents - numUncoveredEvents) * 100.0 / numTotalEvents).ToString("F1"));

                #endregion

                // Find uncovered states
                var uncoveredStates = new HashSet<string>(this.CoverageInfo.MachinesToStates[machine]);
                foreach (var tr in machineToIntraTransitions[machine])
                {
                    uncoveredStates.Remove(tr.StateOrigin);
                    uncoveredStates.Remove(tr.StateTarget);
                }

                foreach (var tr in machineToIncomingTransitions[machine])
                {
                    uncoveredStates.Remove(tr.StateTarget);
                }

                foreach (var tr in machineToOutgoingTransitions[machine])
                {
                    uncoveredStates.Remove(tr.StateOrigin);
                }

                // state maps
                var stateToIncomingEvents = new Dictionary<string, HashSet<string>>();
                foreach (var tr in machineToIncomingTransitions[machine])
                {
                    if (!stateToIncomingEvents.ContainsKey(tr.StateTarget))
                    {
                        stateToIncomingEvents.Add(tr.StateTarget, new HashSet<string>());
                    }

                    stateToIncomingEvents[tr.StateTarget].Add(tr.EdgeLabel);
                }

                var stateToOutgoingEvents = new Dictionary<string, HashSet<string>>();
                foreach (var tr in machineToOutgoingTransitions[machine])
                {
                    if (!stateToOutgoingEvents.ContainsKey(tr.StateOrigin))
                    {
                        stateToOutgoingEvents.Add(tr.StateOrigin, new HashSet<string>());
                    }

                    stateToOutgoingEvents[tr.StateOrigin].Add(tr.EdgeLabel);
                }

                var stateToOutgoingStates = new Dictionary<string, HashSet<string>>();
                var stateToIncomingStates = new Dictionary<string, HashSet<string>>();
                foreach (var tr in machineToIntraTransitions[machine])
                {
                    if (!stateToOutgoingStates.ContainsKey(tr.StateOrigin))
                    {
                        stateToOutgoingStates.Add(tr.StateOrigin, new HashSet<string>());
                    }

                    stateToOutgoingStates[tr.StateOrigin].Add(tr.StateTarget);

                    if (!stateToIncomingStates.ContainsKey(tr.StateTarget))
                    {
                        stateToIncomingStates.Add(tr.StateTarget, new HashSet<string>());
                    }

                    stateToIncomingStates[tr.StateTarget].Add(tr.StateOrigin);
                }

                // Per-state data
                foreach (var state in this.CoverageInfo.MachinesToStates[machine])
                {
                    writer.WriteLine();
                    writer.WriteLine("\tState: {0}{1}", state, uncoveredStates.Contains(state) ? " is uncovered" : "");
                    if (!uncoveredStates.Contains(state))
                    {
                        writer.WriteLine("\t\tState event coverage: {0}%",
                            machineToStatesToEvents[machine][state].Count == 0 ? "100" :
                            ((machineToStatesToEvents[machine][state].Count - machineUncoveredEvents[state].Count) * 100.0 /
                              machineToStatesToEvents[machine][state].Count).ToString("F1"));
                    }

                    if (stateToIncomingEvents.ContainsKey(state) && stateToIncomingEvents[state].Count > 0)
                    {
                        writer.Write("\t\tEvents Received: ");
                        foreach (var e in stateToIncomingEvents[state])
                        {
                            writer.Write("{0} ", e);
                        }

                        writer.WriteLine();
                    }

                    if (stateToOutgoingEvents.ContainsKey(state) && stateToOutgoingEvents[state].Count > 0)
                    {
                        writer.Write("\t\tEvents Sent: ");
                        foreach (var e in stateToOutgoingEvents[state])
                        {
                            writer.Write("{0} ", e);
                        }

                        writer.WriteLine();
                    }

                    if (stateToIncomingStates.ContainsKey(state) && stateToIncomingStates[state].Count > 0)
                    {
                        writer.Write("\t\tPrevious States: ");
                        foreach (var s in stateToIncomingStates[state])
                        {
                            writer.Write("{0} ", s);
                        }

                        writer.WriteLine();
                    }

                    if (stateToOutgoingStates.ContainsKey(state) && stateToOutgoingStates[state].Count > 0)
                    {
                        writer.Write("\t\tNext States: ");
                        foreach (var s in stateToOutgoingStates[state])
                        {
                            writer.Write("{0} ", s);
                        }

                        writer.WriteLine();
                    }
                }

                writer.WriteLine();
            }
        }

        /// <summary>
        /// Gets the state id.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <param name="stateName">State name</param>
        /// <returns>State id</returns>
        private string GetStateId(string machineName, string stateName)
        {
            return string.Format("{0}::{1}", stateName, machineName);
        }

        #endregion
    }
}