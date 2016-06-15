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

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class implementing a P# program visualizer.
    /// </summary>
    class PSharpDgmlVisualizer : IProgramVisualizer
    {
        #region fields

        /// <summary>
        /// The log file.
        /// </summary>
        private readonly string Logfile;

        /// <summary>
        /// The XML writer.
        /// </summary>
        private XmlTextWriter Writer;

        /// <summary>
        /// Map from machines to states.
        /// </summary>
        private Dictionary<string, HashSet<string>> MachinesToStates;

        /// <summary>
        /// Set of machine transitions.
        /// </summary>
        private HashSet<Transition> Transitions;

        #endregion

        #region constructors and destructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logfile">Log file</param>
        public PSharpDgmlVisualizer(string logfile)
        {
            this.Logfile = logfile;
            this.Writer = null;

            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.Transitions = new HashSet<Transition>();
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
        /// Refreshes the visualization.
        /// </summary>
        public void Refresh()
        {
            this.Writer = new XmlTextWriter(this.Logfile, Encoding.UTF8);
            this.Dump();
            this.Writer.Close();
        }

        /// <summary>
        /// Adds a new transition.
        /// </summary>
        /// <param name="machineOrigin">Origin machine</param>
        /// <param name="stateOrigin">Origin state</param>
        /// <param name="edgeLabel">Edge label</param>
        /// <param name="machineTarget">Target machine</param>
        /// <param name="stateTarget">Target state</param>
        public void AddTransition(string machineOrigin, string stateOrigin, string edgeLabel,
            string machineTarget, string stateTarget)
        {
            AddState(machineOrigin, stateOrigin);
            AddState(machineTarget, stateTarget);
            this.Transitions.Add(new Transition(machineOrigin, stateOrigin,
                edgeLabel, machineTarget, stateTarget));
        }
        
        #endregion

        #region private methods

        private void Dump()
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
            foreach (var machine in this.MachinesToStates.Keys)
            {
                this.Writer.WriteStartElement("Node");
                this.Writer.WriteAttributeString("Id", machine);
                this.Writer.WriteAttributeString("Group", "Expanded");
                this.Writer.WriteEndElement();
            }

            // Iterates states.
            foreach (var tup in this.MachinesToStates)
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
            foreach (var tup in this.MachinesToStates)
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
            foreach (var transition in this.Transitions)
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
        /// Adds a new state.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <param name="stateName">State name</param>
        private void AddState(string machineName, string stateName)
        {
            if (!this.MachinesToStates.ContainsKey(machineName))
            {
                this.MachinesToStates.Add(machineName, new HashSet<string>());
            }

            this.MachinesToStates[machineName].Add(stateName);
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