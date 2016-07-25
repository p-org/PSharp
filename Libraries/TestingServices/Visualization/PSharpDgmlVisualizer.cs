//-----------------------------------------------------------------------
// <copyright file="PSharpProgramVisualizer.cs">
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
using System.Threading.Tasks;
using System.Xml;

using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp.TestingServices.Visualization
{
    /// <summary>
    /// Class implementing a P# program visualizer.
    /// </summary>
    class PSharpDgmlVisualizer : IProgramVisualizer
    {
        #region fields

        /// <summary>
        /// The graph file.
        /// </summary>
        private readonly string GraphFile;

        /// <summary>
        /// The code coverage file.
        /// </summary>
        private readonly string CodeCoverageFile;

        /// <summary>
        /// The XML writer.
        /// </summary>
        private XmlTextWriter Writer;

        /// <summary>
        /// The coverage file writer.
        /// </summary>
        private TextWriter CoverageFileWriter;

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        public CoverageInfo CoverageInfo { get; }

        #endregion

        #region constructors and destructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graphFile">Graph file</param>
        /// <param name="coverageFile">Code coverage file</param>
        public PSharpDgmlVisualizer(string graphFile, string coverageFile)
        {
            this.GraphFile = graphFile;
            this.CodeCoverageFile = coverageFile;
            this.Writer = null;
            this.CoverageInfo = new CoverageInfo();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~PSharpDgmlVisualizer()
        {
            this.Refresh();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Starts the visualisation asynchronously.
        /// </summary>
        /// <returns>Task</returns>
        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a testing iteration finishes.
        /// </summary>
        public void Step()
        {
            // skip
        }

        /// <summary>
        /// Refreshes the visualization.
        /// </summary>
        public void Refresh()
        {
            this.Writer = new XmlTextWriter(this.GraphFile, Encoding.UTF8);
            this.WriteVisualizationGraph();
            this.Writer.Close();

            using (this.CoverageFileWriter = new StreamWriter(this.CodeCoverageFile))
            {
                this.WriteCoverageFile();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Writes the visualization graph.
        /// </summary>
        private void WriteVisualizationGraph()
        {
            // Starts document.
            this.Writer.WriteStartDocument(true);
            this.Writer.Formatting = Formatting.Indented;
            this.Writer.Indentation = 2;

            // Starts DirectedGraph element.
            this.Writer.WriteStartElement("DirectedGraph", @"http://schemas.microsoft.com/vs/2009/dgml");

            // Starts Nodes element.
            this.Writer.WriteStartElement("Nodes");

            // Iterates machines.
            foreach (var machine in this.CoverageInfo.MachinesToStates.Keys)
            {
                this.Writer.WriteStartElement("Node");
                this.Writer.WriteAttributeString("Id", machine);
                this.Writer.WriteAttributeString("Group", "Expanded");
                this.Writer.WriteEndElement();
            }

            // Iterates states.
            foreach (var tup in this.CoverageInfo.MachinesToStates)
            {
                var machine = tup.Key;
                foreach (var state in tup.Value)
                {
                    this.Writer.WriteStartElement("Node");
                    this.Writer.WriteAttributeString("Id", GetStateId(machine, state));
                    this.Writer.WriteAttributeString("Label", state);
                    this.Writer.WriteEndElement();
                }
            }

            // Ends Nodes element.
            this.Writer.WriteEndElement();

            // Starts Links element.
            this.Writer.WriteStartElement("Links");

            // Iterates states.
            foreach (var tup in this.CoverageInfo.MachinesToStates)
            {
                var machine = tup.Key;
                foreach (var state in tup.Value)
                {
                    this.Writer.WriteStartElement("Link");
                    this.Writer.WriteAttributeString("Source", machine);
                    this.Writer.WriteAttributeString("Target", GetStateId(machine, state));
                    this.Writer.WriteAttributeString("Category", "Contains");
                    this.Writer.WriteEndElement();
                }
            }
            
            // Iterates transitions.
            foreach (var transition in this.CoverageInfo.Transitions)
            {
                this.Writer.WriteStartElement("Link");
                this.Writer.WriteAttributeString("Source", GetStateId(transition.MachineOrigin, transition.StateOrigin));
                this.Writer.WriteAttributeString("Target", GetStateId(transition.MachineTarget, transition.StateTarget));
                this.Writer.WriteAttributeString("Label", transition.EdgeLabel);
                this.Writer.WriteEndElement();
            }

            // Ends Links element.
            this.Writer.WriteEndElement();

            // Ends DirectedGraph element.
            this.Writer.WriteEndElement();

            // Ends document.
            this.Writer.WriteEndDocument();
        }

        /// <summary>
        /// Writes the visualization graph.
        /// </summary>
        private void WriteCoverageFile()
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

            this.CoverageFileWriter.WriteLine("Total event coverage: {0}%",
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
                this.CoverageFileWriter.WriteLine("Machine: {0}", machine);
                this.CoverageFileWriter.WriteLine("***************");

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

                this.CoverageFileWriter.WriteLine("Machine event coverage: {0}%", 
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
                    this.CoverageFileWriter.WriteLine();
                    this.CoverageFileWriter.WriteLine("\tState: {0}{1}", state, uncoveredStates.Contains(state) ? " is uncovered" : "");
                    if (!uncoveredStates.Contains(state))
                    {
                        this.CoverageFileWriter.WriteLine("\t\tState event coverage: {0}%",
                            machineToStatesToEvents[machine][state].Count == 0 ? "100" :
                            ((machineToStatesToEvents[machine][state].Count - machineUncoveredEvents[state].Count) * 100.0 /
                              machineToStatesToEvents[machine][state].Count).ToString("F1"));
                    }

                    if (stateToIncomingEvents.ContainsKey(state) && stateToIncomingEvents[state].Count > 0)
                    {
                        this.CoverageFileWriter.Write("\t\tEvents Received: ");
                        foreach (var e in stateToIncomingEvents[state])
                        {
                            this.CoverageFileWriter.Write("{0} ", e);
                        }

                        this.CoverageFileWriter.WriteLine();
                    }

                    if (stateToOutgoingEvents.ContainsKey(state) && stateToOutgoingEvents[state].Count > 0)
                    {
                        this.CoverageFileWriter.Write("\t\tEvents Sent: ");
                        foreach (var e in stateToOutgoingEvents[state])
                        {
                            this.CoverageFileWriter.Write("{0} ", e);
                        }

                        this.CoverageFileWriter.WriteLine();
                    }

                    if (stateToIncomingStates.ContainsKey(state) && stateToIncomingStates[state].Count > 0)
                    {
                        this.CoverageFileWriter.Write("\t\tPrevious States: ");
                        foreach (var s in stateToIncomingStates[state])
                        {
                            this.CoverageFileWriter.Write("{0} ", s);
                        }

                        this.CoverageFileWriter.WriteLine();
                    }

                    if (stateToOutgoingStates.ContainsKey(state) && stateToOutgoingStates[state].Count > 0)
                    {
                        this.CoverageFileWriter.Write("\t\tNext States: ");
                        foreach (var s in stateToOutgoingStates[state])
                        {
                            this.CoverageFileWriter.Write("{0} ", s);
                        }

                        this.CoverageFileWriter.WriteLine();
                    }
                }

                this.CoverageFileWriter.WriteLine();
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