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
    class PSharpProgramVisualizer : IProgramVisualizer
    {
        // Form controls
        private Form Form;
        private Graph Graph;
        private GViewer Viewer;
        private Control TextBox;

        // statics
        private readonly double NodeFontSize = 8.0;
        private readonly double SubgraphFontSize = 8.0;
        private readonly double EdgeFontSize = 4.0;

        // P# Program
        private HashSet<string> Machines;
        private Dictionary<string, HashSet<string>> States;
        private Dictionary<Transition, Edge> Transitions;
        private ConcurrentBag<Transition> PendingTransitions;

        private Dictionary<string, Subgraph> MachineToSubgraph;
        private Dictionary<Tuple<string, string>, Node> StateToNode;
        private HashSet<string> CollapsedMachines;

        public PSharpProgramVisualizer()
        {
            this.Graph = new Graph("P# Program");
            this.Machines = new HashSet<string>();
            this.States = new Dictionary<string, HashSet<string>>();
            this.MachineToSubgraph = new Dictionary<string, Subgraph>();
            this.States = new Dictionary<string, HashSet<string>>();
            this.StateToNode = new Dictionary<Tuple<string, string>, Node>();
            this.CollapsedMachines = new HashSet<string>();
            this.Transitions = new Dictionary<Transition, Edge>();
            this.PendingTransitions = new System.Collections.Concurrent.ConcurrentBag<Transition>();
        }

        public void AddMachine(string machine)
        {
            if (this.Machines.Contains(machine)) return;

            lock (this.Graph)
            {
                AddMachineInternal(machine);
            }
        }

        void AddMachineInternal(string machine)
        {
            if (this.Machines.Contains(machine)) return;
            this.Machines.Add(machine);
            this.States.Add(machine, new HashSet<string>());

            var subgraph = new Subgraph(machine);
            subgraph.Label.FontSize = this.SubgraphFontSize;

            this.Graph.RootSubgraph.AddSubgraph(subgraph);
            this.MachineToSubgraph.Add(machine, subgraph);
        }

        public void AddState(string machine, string state)
        {
            AddMachine(machine);
            if (this.States[machine].Contains(state)) return;

            lock(this.Graph)
            {
                AddStateInternal(machine, state);
            }
        }

        void AddStateInternal(string machine, string state)
        {
            AddMachineInternal(machine);
            if (this.States[machine].Contains(state)) return;

            this.States[machine].Add(state);

            var node = this.Graph.AddNode(string.Format("{0}::{1}", state, machine));
            node.Label.FontSize = this.NodeFontSize;

            node.LabelText = state;
            this.MachineToSubgraph[machine].AddNode(node);

            this.StateToNode.Add(Tuple.Create(state, machine), node);
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
            //this.PendingTransitions.Add(new Transition(machineOrigin, stateOrigin, edgeLabel, machineTarget, stateTarget));
            var tr = new Transition(machineOrigin, stateOrigin, edgeLabel, machineTarget, stateTarget);
            if (this.Transitions.ContainsKey(tr)) return;

            lock (this.Graph)
            {
                AddStateInternal(tr.MachineOrigin, tr.StateOrigin);
                AddStateInternal(tr.MachineTarget, tr.StateTarget);

                var edge = this.Graph.AddEdge(GetNode(tr.StateOrigin, tr.MachineOrigin).Id,
                    tr.EdgeLabel, GetNode(tr.StateTarget, tr.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;

                this.Transitions.Add(tr, edge);
            }
        }

        /// <summary>
        /// Starts the visualisation asynchronously.
        /// </summary>
        /// <returns>Task</returns>
        public Task StartAsync()
        {
            //System.Diagnostics.Debugger.Break();

            //create a form
            this.Form = new Form();
            this.Form.Size = new System.Drawing.Size(500, 500);

            //create a viewer object
            this.Viewer = new GViewer();
            this.Viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Viewer.AutoScroll = false;
            //this.Viewer.CurrentLayoutMethod = LayoutMethod.MDS;

            // control
            var b = CreateCommandButton();
            this.Form.Controls.Add(b);
            b.BringToFront();

            this.Viewer.ObjectUnderMouseCursorChanged += new EventHandler<Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs>(viewer_ObjectUnderMouseCursorChanged);
            this.Viewer.MouseDown += new MouseEventHandler(viewer_MouseDown);
            this.Viewer.SuspendLayout();

            this.Form.SuspendLayout();
            this.Form.Controls.Add(this.Viewer as Control);

            this.Viewer.ResumeLayout(false);
            this.Form.ResumeLayout(false);

            this.Viewer.Graph = this.Graph;

            return Task.Run(() => this.Form.ShowDialog());
        }

        Control CreateCommandButton()
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

        private void Button_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ProcessInput(this.TextBox.Text);

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

        void viewer_ObjectUnderMouseCursorChanged(object sender, Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs e)
        {
        }

        void viewer_MouseDown(object sender, MouseEventArgs e)
        {
        }

        void viewer_Scroll(object sender, ScrollEventArgs e)
        {
            Console.WriteLine(e.NewValue);
        }

        /// <summary>
        /// Refreshes the visualization.
        /// </summary>
        public void Refresh()
        {
            if (!this.Form.IsHandleCreated) return;
            lock(this.Form)
            {
                this.Form.SuspendLayout();
                this.Form.Invoke(new RefreshDelegate(RefreshInternal));
                this.Form.ResumeLayout();
            }
        }

        void RefreshInternal()
        {
            if (!this.Form.IsHandleCreated) return;

            lock(this.Graph)
            {
                // Save viewer state
                var z = this.Viewer.ZoomF;
                // refresh
                this.Viewer.Graph = this.Graph;
                // Restore viewer state
                this.Viewer.ZoomF = z;
            }
        }

        private delegate void RefreshDelegate();

        private void Expand(string machine)
        {
            if(!this.CollapsedMachines.Contains(machine))
                return;
            this.CollapsedMachines.Remove(machine);

            // delete all edges on the subgraph node
            var outgoing = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin == machine && tr.Key.MachineTarget != machine)
                .Select(tr => tr.Key));

            var incoming = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin != machine && tr.Key.MachineTarget == machine)
                .Select(tr => tr.Key));

            var self = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin == machine && tr.Key.MachineTarget == machine)
                .Select(tr => tr.Key));

            foreach (var t in outgoing.Concat(incoming))
                this.Graph.RemoveEdge(this.Transitions[t]);

            this.Graph.RemoveNode(this.Graph.FindNode(machine));

            // re-create subgraph
            this.MachineToSubgraph[machine] = new Subgraph(machine);
            this.MachineToSubgraph[machine].Label.FontSize = this.SubgraphFontSize;

            this.Graph.RootSubgraph.AddSubgraph(this.MachineToSubgraph[machine]);

            // add machine states
            foreach (var state in this.States[machine])
            {
                var old_node = this.StateToNode[Tuple.Create(state, machine)];

                var new_node = this.Graph.AddNode(old_node.Id);
                new_node.Label.FontSize = this.NodeFontSize;
                new_node.LabelText = state;

                this.MachineToSubgraph[machine].AddNode(new_node);
                this.StateToNode[Tuple.Create(state, machine)] = new_node;
            }

            // Add intra-machine transitions
            foreach (var t in self)
            {
                var edge = this.Graph.AddEdge(GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;
                this.Transitions[t] = edge;
            }

            // Add inter-machine transitions
            foreach (var t in outgoing.Concat(incoming))
            {
                var edge = this.Graph.AddEdge(GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;
                this.Transitions[t] = edge;
            }
        }

        private void Collapse(string machine)
        {
            if (this.CollapsedMachines.Contains(machine))
                return;

            this.CollapsedMachines.Add(machine);
            
            // delete transitions of the machine
            foreach (var t in this.Transitions.Where(tr => tr.Key.MachineOrigin == machine || tr.Key.MachineTarget == machine))
            {
                this.Graph.RemoveEdge(t.Value);
            }
            
            // remove machine states
            foreach (var state in this.States[machine])
            {
                this.MachineToSubgraph[machine].RemoveNode(this.StateToNode[Tuple.Create(state, machine)]);
                this.Graph.RemoveNode(this.StateToNode[Tuple.Create(state, machine)]);
            }

            this.Graph.RootSubgraph.RemoveSubgraph(this.MachineToSubgraph[machine]);

            // add edges to the subgraph node
            var outgoing = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin == machine && tr.Key.MachineTarget != machine)
                .Select(tr => tr.Key));

            var incoming = new List<Transition>(
                this.Transitions.Where(tr => tr.Key.MachineOrigin != machine && tr.Key.MachineTarget == machine)
                .Select(tr => tr.Key));

            outgoing.ForEach(t => this.Transitions.Remove(t));
            incoming.ForEach(t => this.Transitions.Remove(t));

            foreach (var t in outgoing.Concat(incoming))
            {
                var edge = this.Graph.AddEdge(GetNode(t.StateOrigin, t.MachineOrigin).Id,
                    t.EdgeLabel, GetNode(t.StateTarget, t.MachineTarget).Id);
                edge.Label.FontSize = this.EdgeFontSize;

                this.Transitions.Add(t, edge);
            }
        }

        protected Node GetNode(string state, string machine)
        {
            if (!this.CollapsedMachines.Contains(machine))
            {
                return this.StateToNode[Tuple.Create(state, machine)];
            }
            return this.MachineToSubgraph[machine];
        }

        protected void ProcessInput(string line)
        {
            var tok = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 0) return;

            switch (tok[0])
            {
                case "collapse":
                    lock(this.Graph)
                    {
                        Collapse(tok[1]);
                    }
                    break;
                case "expand":
                    lock (this.Graph)
                    {
                        Expand(tok[1]);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
        }
    }
}