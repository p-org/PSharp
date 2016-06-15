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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class implementing a P# program visualizer.
    /// </summary>
    internal class PSharpProgramVisualizer : IProgramVisualizer
    {
        #region fields

        /// <summary>
        /// The window form.
        /// </summary>
        private Form Form;

        /// <summary>
        /// The MSagl graph.
        /// </summary>
        private Graph Graph;

        /// <summary>
        /// The MSagl graph viewer.
        /// </summary>
        private GViewer Viewer;

        /// <summary>
        /// The controls.
        /// </summary>
        private Control TextBox;
        
        /// <summary>
        /// Font size for nodes.
        /// </summary>
        private readonly double NodeFontSize = 8.0;

        /// <summary>
        /// Font size for subgraphs.
        /// </summary>
        private readonly double SubgraphFontSize = 8.0;

        /// <summary>
        /// Font size for edges.
        /// </summary>
        private readonly double EdgeFontSize = 4.0;

        /// <summary>
        /// Set of machines.
        /// </summary>
        private HashSet<string> Machines;

        /// <summary>
        /// Map from machines to states.
        /// </summary>
        private Dictionary<string, HashSet<string>> MachinesToStates;

        /// <summary>
        /// Map of transitions to edges.
        /// </summary>
        private Dictionary<Transition, Edge> Transitions;

        /// <summary>
        /// Collection of pending transitions.
        /// </summary>
        private ConcurrentBag<Transition> PendingTransitions;

        /// <summary>
        /// Map from machines to subgraphs.
        /// </summary>
        private Dictionary<string, Subgraph> MachineToSubgraph;

        /// <summary>
        /// Map from states to nodes.
        /// </summary>
        private Dictionary<Tuple<string, string>, Node> StateToNode;

        /// <summary>
        /// Set of collapsed machines.
        /// </summary>
        private HashSet<string> CollapsedMachines;

        /// <summary>
        /// The refresh delegate.
        /// </summary>
        private delegate void RefreshDelegate();

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public PSharpProgramVisualizer()
        {
            this.Graph = new Graph("P# Program");
            this.Machines = new HashSet<string>();
            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.MachineToSubgraph = new Dictionary<string, Subgraph>();
            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.StateToNode = new Dictionary<Tuple<string, string>, Node>();
            this.CollapsedMachines = new HashSet<string>();
            this.Transitions = new Dictionary<Transition, Edge>();
            this.PendingTransitions = new ConcurrentBag<Transition>();
        }

        #endregion

        #region methods

        /// <summary>
        /// Adds a new machine.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        public void AddMachine(string machineName)
        {
            if (!this.Machines.Contains(machineName))
            {
                lock (this.Graph)
                {
                    this.AddMachineInternal(machineName);
                }
            }
        }

        /// <summary>
        /// Adds a new state.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <param name="stateName">State name</param>
        public void AddState(string machineName, string stateName)
        {
            this.AddMachine(machineName);

            if (!this.MachinesToStates[machineName].Contains(stateName))
            {
                lock (this.Graph)
                {
                    this.AddStateInternal(machineName, stateName);
                }
            }
        }

        /// <summary>
        /// Starts the visualisation asynchronously.
        /// </summary>
        /// <returns>Task</returns>
        public Task StartAsync()
        {
            // Creates a new form.
            this.Form = new Form();
            this.Form.Size = new System.Drawing.Size(500, 500);

            // Creates a new graph viewer.
            this.Viewer = new GViewer();
            this.Viewer.Dock = DockStyle.Fill;
            this.Viewer.AutoScroll = false;

            // Creates a control.
            var button = CreateCommandButton();
            this.Form.Controls.Add(button);
            button.BringToFront();

            this.Viewer.ObjectUnderMouseCursorChanged +=
                new EventHandler<ObjectUnderMouseCursorChangedEventArgs>(
                    ViewerObjectUnderMouseCursorChanged);
            this.Viewer.MouseDown += new MouseEventHandler(ViewerMouseDown);
            this.Viewer.SuspendLayout();

            this.Form.SuspendLayout();
            this.Form.Controls.Add(this.Viewer as Control);

            this.Viewer.ResumeLayout(false);
            this.Form.ResumeLayout(false);

            this.Viewer.Graph = this.Graph;

            return Task.Run(() => this.Form.ShowDialog());
        }

        /// <summary>
        /// Refreshes the visualization.
        /// </summary>
        public void Refresh()
        {
            if (this.Form.IsHandleCreated)
            {
                lock (this.Form)
                {
                    this.Form.SuspendLayout();
                    this.Form.Invoke(new RefreshDelegate(RefreshInternal));
                    this.Form.ResumeLayout();
                }
            }
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
            var tr = new Transition(machineOrigin, stateOrigin, edgeLabel, machineTarget, stateTarget);
            if (this.Transitions.ContainsKey(tr)) return;

            lock (this.Graph)
            {
                this.AddStateInternal(tr.MachineOrigin, tr.StateOrigin);
                this.AddStateInternal(tr.MachineTarget, tr.StateTarget);

                var edge = this.Graph.AddEdge(this.GetNode(tr.StateOrigin, tr.MachineOrigin).Id,
                    tr.EdgeLabel, this.GetNode(tr.StateTarget, tr.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;

                this.Transitions.Add(tr, edge);
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Hides an event.
        /// </summary>
        /// <param name="eventName">Event name</param>
        protected void HideEvent(string eventName)
        {
            // Deletes the corresponding transitions of the machine.
            foreach (var t in this.Transitions.Where(tr => tr.Key.EdgeLabel == eventName))
            {
                this.Graph.RemoveEdge(t.Value);
            }
        }

        /// <summary>
        /// Unhides an event.
        /// </summary>
        /// <param name="eventName">Event name</param>
        protected void UnHideEvent(string eventName)
        {
            var trlist = new List<Transition>(this.Transitions.Where(
                t => t.Key.EdgeLabel == eventName).Select(t => t.Key));

            // Add the corresponding transitions back.
            foreach (var t in trlist)
            {
                var edge = this.Graph.AddEdge(GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;

                this.Transitions[t] = edge;
            }
        }

        /// <summary>
        /// Gets a node.
        /// </summary>
        /// <param name="stateName">State name</param>
        /// <param name="machineName">Machine name</param>
        /// <returns></returns>
        protected Node GetNode(string stateName, string machineName)
        {
            if (!this.CollapsedMachines.Contains(machineName))
            {
                return this.StateToNode[Tuple.Create(stateName, machineName)];
            }

            return this.MachineToSubgraph[machineName];
        }

        /// <summary>
        /// Processes the specified input.
        /// </summary>
        /// <param name="line">Line</param>
        protected void ProcessInput(string line)
        {
            var tok = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 0) return;

            switch (tok[0])
            {
                case "collapse":
                    lock (this.Graph)
                    {
                        if (tok.Length == 2)
                        {
                            Collapse(tok[1]);
                        }
                    }
                    break;

                case "expand":
                    lock (this.Graph)
                    {
                        if (tok.Length == 2)
                        {
                            Expand(tok[1]);
                        }
                    }
                    break;

                case "hide":
                    lock (this.Graph)
                    {
                        if (tok.Length == 2)
                        {
                            HideEvent(tok[1]);
                        }
                    }
                    break;

                case "unhide":
                    lock (this.Graph)
                    {
                        if (tok.Length == 2)
                        {
                            UnHideEvent(tok[1]);
                        }
                    }
                    break;

                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Adds a new machine.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        private void AddMachineInternal(string machineName)
        {
            if (!this.Machines.Contains(machineName))
            {
                this.Machines.Add(machineName);
                this.MachinesToStates.Add(machineName, new HashSet<string>());

                var subgraph = new Subgraph(machineName);
                subgraph.Label.FontSize = this.SubgraphFontSize;

                this.Graph.RootSubgraph.AddSubgraph(subgraph);
                this.MachineToSubgraph.Add(machineName, subgraph);
            }
        }

        /// <summary>
        /// Adds a new state.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <param name="stateName">State name</param>
        private void AddStateInternal(string machineName, string stateName)
        {
            this.AddMachineInternal(machineName);
            if (!this.MachinesToStates[machineName].Contains(stateName))
            {
                this.MachinesToStates[machineName].Add(stateName);

                var node = this.Graph.AddNode(string.Format("{0}::{1}", stateName, machineName));
                node.Label.FontSize = this.NodeFontSize;

                node.LabelText = stateName;
                this.MachineToSubgraph[machineName].AddNode(node);

                this.StateToNode.Add(Tuple.Create(stateName, machineName), node);
            }
        }

        /// <summary>
        /// Creates a command button.
        /// </summary>
        /// <returns>Control</returns>
        private Control CreateCommandButton()
        {
            var button = new TextBox();
            this.TextBox = button;

            button.Location = new System.Drawing.Point(0, 31);
            button.Name = "Command";
            button.Size = new System.Drawing.Size(120, 23);

            button.Multiline = false;
            button.AcceptsReturn = false;
            button.KeyUp += Button_KeyUp;
            
            return button;
        }

        /// <summary>
        /// Processes a pressed key up button.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">KeyEventArgs</param>
        private void Button_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.ProcessInput(this.TextBox.Text);

                if (this.Form.IsHandleCreated)
                {
                    lock(this.Form)
                    {
                        this.Form.SuspendLayout();
                        this.Form.Invoke(new RefreshDelegate(RefreshInternal));
                        this.Form.ResumeLayout();
                    }
                }

                this.TextBox.Text = "";
            }
        }

        /// <summary>
        /// Event handler for when the mouse cursor changes.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ObjectUnderMouseCursorChangedEventArgs</param>
        void ViewerObjectUnderMouseCursorChanged(object sender,
            ObjectUnderMouseCursorChangedEventArgs e)
        {

        }

        /// <summary>
        /// Event handler for when the user presses the mouse down.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">MouseEventArgs</param>
        void ViewerMouseDown(object sender, MouseEventArgs e)
        {

        }

        /// <summary>
        /// Event handler for when the user scrolls.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ScrollEventArgs</param>
        void ViewerScroll(object sender, ScrollEventArgs e)
        {
            Console.WriteLine(e.NewValue);
        }

        /// <summary>
        /// Refreshes the visualization.
        /// </summary>
        private void RefreshInternal()
        {
            if (this.Form.IsHandleCreated)
            {
                lock (this.Graph)
                {
                    // Saves the viewer state.
                    var z = this.Viewer.ZoomF;
                    // Refreshes.
                    this.Viewer.Graph = this.Graph;
                    // Restores the viewer state.
                    this.Viewer.ZoomF = z;
                }
            }
        }

        /// <summary>
        /// Expands the machine in the graph.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        private void Expand(string machineName)
        {
            if (!Machines.Contains(machineName) ||
                !this.CollapsedMachines.Contains(machineName))
            {
                return;
            }

            this.CollapsedMachines.Remove(machineName);

            // Deletes all edges on the subgraph node.
            var outgoing = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin == machineName &&
                tr.Key.MachineTarget != machineName)
                .Select(tr => tr.Key));

            var incoming = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin != machineName &&
                tr.Key.MachineTarget == machineName)
                .Select(tr => tr.Key));

            var self = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin == machineName &&
                tr.Key.MachineTarget == machineName)
                .Select(tr => tr.Key));

            foreach (var t in outgoing.Concat(incoming))
            {
                this.Graph.RemoveEdge(this.Transitions[t]);
            }

            this.Graph.RemoveNode(this.Graph.FindNode(machineName));

            // Re-creates the subgraph.
            this.MachineToSubgraph[machineName] = new Subgraph(machineName);
            this.MachineToSubgraph[machineName].Label.FontSize = this.SubgraphFontSize;

            this.Graph.RootSubgraph.AddSubgraph(this.MachineToSubgraph[machineName]);

            // Adds the machine states.
            foreach (var state in this.MachinesToStates[machineName])
            {
                var old_node = this.StateToNode[Tuple.Create(state, machineName)];

                var new_node = this.Graph.AddNode(old_node.Id);
                new_node.Label.FontSize = this.NodeFontSize;
                new_node.LabelText = state;

                this.MachineToSubgraph[machineName].AddNode(new_node);
                this.StateToNode[Tuple.Create(state, machineName)] = new_node;
            }

            // Adds the intra-machine transitions.
            foreach (var t in self)
            {
                var edge = this.Graph.AddEdge(this.GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, this.GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;
                this.Transitions[t] = edge;
            }

            // Adds the inter-machine transitions.
            foreach (var t in outgoing.Concat(incoming))
            {
                var edge = this.Graph.AddEdge(this.GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, this.GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;
                this.Transitions[t] = edge;
            }
        }

        /// <summary>
        /// Collapses the machine in the graph.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        private void Collapse(string machineName)
        {
            if (!Machines.Contains(machineName) ||
                this.CollapsedMachines.Contains(machineName))
            {
                return;
            }

            this.CollapsedMachines.Add(machineName);
            
            // Deletes the transitions of the machine.
            foreach (var t in this.Transitions.Where(
                tr => tr.Key.MachineOrigin == machineName ||
                tr.Key.MachineTarget == machineName))
            {
                this.Graph.RemoveEdge(t.Value);
            }
            
            // Removes the machine states.
            foreach (var state in this.MachinesToStates[machineName])
            {
                this.MachineToSubgraph[machineName].RemoveNode(this.StateToNode[Tuple.Create(state, machineName)]);
                this.Graph.RemoveNode(this.StateToNode[Tuple.Create(state, machineName)]);
            }

            this.Graph.RootSubgraph.RemoveSubgraph(this.MachineToSubgraph[machineName]);

            // Adds the edges to the subgraph node.
            var outgoing = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin == machineName &&
                tr.Key.MachineTarget != machineName)
                .Select(tr => tr.Key));

            var incoming = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin != machineName &&
                tr.Key.MachineTarget == machineName)
                .Select(tr => tr.Key));

            outgoing.ForEach(t => this.Transitions.Remove(t));
            incoming.ForEach(t => this.Transitions.Remove(t));

            foreach (var t in outgoing.Concat(incoming))
            {
                var edge = this.Graph.AddEdge(this.GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, this.GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;

                this.Transitions.Add(t, edge);
            }
        }

        #endregion
    }
}