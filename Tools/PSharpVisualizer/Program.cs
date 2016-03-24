using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PSharpVisualizer
{
    class Driver
    {
        static void Main(string[] args)
        {
            IProgramVisualizer rg = new ProgramVisualizer();

            var t = rg.StartAsync();

            /*
            rg.AddMachine("server");
            rg.AddState("server", "Init");
            rg.AddState("server", "Playing");

            rg.AddMachine("client");
            rg.AddState("client", "Init");
            rg.AddState("client", "Playing");
            */

            rg.AddTransition("server", "Init", "goto", "server", "Playing");
            rg.AddTransition("server", "Playing", "Pong", "client", "Playing");

            rg.AddTransition("client", "Init", "goto", "client", "Playing");
            rg.AddTransition("client", "Playing", "Ping", "server", "Playing");

            rg.Refresh();

            t.Wait();
        }
    }

    interface IProgramVisualizer
    {
        // Start the visualizer
        Task StartAsync();

        // Refresh Visualizer display
        void Refresh();

        // Report a transition
        void AddTransition(string machine_from, string state_from, string edge_label, string machine_to, string state_to);
    }

    class ProgramVisualizer : IProgramVisualizer
    {
        // Form controls
        System.Windows.Forms.Form form;
        Graph graph;
        GViewer viewer;
        Control textBox;

        // statics
        readonly double NodeFontSize = 8.0;
        readonly double SubgraphFontSize = 8.0;
        readonly double EdgeFontSize = 4.0;

        // P# Program
        HashSet<string> Machines;
        Dictionary<string, HashSet<string>> States;
        Dictionary<Transition, Edge> Transitions;
        System.Collections.Concurrent.ConcurrentBag<Transition> PendingTransitions;

        Dictionary<string, Subgraph> MachineToSubgraph;
        Dictionary<Tuple<string, string>, Node> StateToNode;
        HashSet<string> CollapsedMachines;

        public ProgramVisualizer()
        {
            graph = new Graph("P# Program");
            Machines = new HashSet<string>();
            States = new Dictionary<string, HashSet<string>>();
            MachineToSubgraph = new Dictionary<string, Subgraph>();
            States = new Dictionary<string, HashSet<string>>();
            StateToNode = new Dictionary<Tuple<string, string>, Node>();
            CollapsedMachines = new HashSet<string>();
            Transitions = new Dictionary<Transition, Edge>();
            PendingTransitions = new System.Collections.Concurrent.ConcurrentBag<Transition>();
        }

        public void AddMachine(string machine)
        {
            if (Machines.Contains(machine)) return;

            lock (graph)
            {
                AddMachineInternal(machine);
            }
        }

        void AddMachineInternal(string machine)
        {
            if (Machines.Contains(machine)) return;
            Machines.Add(machine);
            States.Add(machine, new HashSet<string>());

            var subgraph = new Subgraph(machine);
            subgraph.Label.FontSize = SubgraphFontSize;

            graph.RootSubgraph.AddSubgraph(subgraph);
            MachineToSubgraph.Add(machine, subgraph);
        }

        public void AddState(string machine, string state)
        {
            AddMachine(machine);
            if (States[machine].Contains(state)) return;

            lock(graph)
            {
                AddStateInternal(machine, state);
            }
        }

        void AddStateInternal(string machine, string state)
        {
            AddMachineInternal(machine);
            if (States[machine].Contains(state)) return;

            States[machine].Add(state);

            var node = graph.AddNode(string.Format("{0}::{1}", state, machine));
            node.Label.FontSize = NodeFontSize;

            node.LabelText = state;
            MachineToSubgraph[machine].AddNode(node);

            StateToNode.Add(Tuple.Create(state, machine), node);
        }

        public void AddTransition(string machine_from, string state_from, string edgeLabel, string machine_to, string state_to)
        {
            //PendingTransitions.Add(new Transition(machine_from, state_from, edgeLabel, machine_to, state_to));
            var tr = new Transition(machine_from, state_from, edgeLabel, machine_to, state_to);
            if (Transitions.ContainsKey(tr)) return;

            lock (graph)
            {
                AddStateInternal(tr.machine_from, tr.state_from);
                AddStateInternal(tr.machine_to, tr.state_to);

                var edge = graph.AddEdge(GetNode(tr.state_from, tr.machine_from).Id, tr.edge_label, GetNode(tr.state_to, tr.machine_to).Id);
                edge.Label.FontSize = EdgeFontSize;

                Transitions.Add(tr, edge);
            }
        }

        public Task StartAsync()
        {
            //System.Diagnostics.Debugger.Break();

            //create a form
            form = new System.Windows.Forms.Form();
            form.Size = new System.Drawing.Size(500, 500);

            //create a viewer object
            viewer = new GViewer();
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            viewer.AutoScroll = false;
            //viewer.CurrentLayoutMethod = LayoutMethod.MDS;

            // control
            var b = CreateCommandButton();
            form.Controls.Add(b);
            b.BringToFront();

            viewer.ObjectUnderMouseCursorChanged += new EventHandler<Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs>(viewer_ObjectUnderMouseCursorChanged);
            viewer.MouseDown += new MouseEventHandler(viewer_MouseDown);
            viewer.SuspendLayout();

            form.SuspendLayout();
            form.Controls.Add(viewer as Control);

            viewer.ResumeLayout(false);
            form.ResumeLayout(false);

            viewer.Graph = graph;

            return Task.Run(() => form.ShowDialog());
        }

        Control CreateCommandButton()
        {
            var button = new TextBox();
            textBox = button;

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
                ProcessInput(textBox.Text);

                if (form.IsHandleCreated)
                {
                    lock(graph)
                    {
                        form.SuspendLayout();
                        form.Invoke(new RefreshDelegate(Refresh));
                        form.ResumeLayout();
                    }
                }

                textBox.Text = "";
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

        public void Refresh()
        {
            if (!form.IsHandleCreated) return;

            lock(graph)
            {
                // Save viewer state
                var z = viewer.ZoomF;
                // refresh
                viewer.Graph = graph;
                // Restore viewer state
                viewer.ZoomF = z;
            }
        }

        private delegate void RefreshDelegate();

        private void Expand(string machine)
        {
            if(!CollapsedMachines.Contains(machine))
                return;
            CollapsedMachines.Remove(machine);

            // delete all edges on the subgraph node
            var outgoing = new List<Transition>(
                Transitions.Where(tr => tr.Key.machine_from == machine && tr.Key.machine_to != machine)
                .Select(tr => tr.Key));

            var incoming = new List<Transition>(
                Transitions.Where(tr => tr.Key.machine_from != machine && tr.Key.machine_to == machine)
                .Select(tr => tr.Key));

            var self = new List<Transition>(
                Transitions.Where(tr => tr.Key.machine_from == machine && tr.Key.machine_to == machine)
                .Select(tr => tr.Key));

            foreach (var t in outgoing.Concat(incoming))
                graph.RemoveEdge(Transitions[t]);

            graph.RemoveNode(graph.FindNode(machine));

            // re-create subgraph
            MachineToSubgraph[machine] = new Subgraph(machine);
            MachineToSubgraph[machine].Label.FontSize = SubgraphFontSize;
            
            graph.RootSubgraph.AddSubgraph(MachineToSubgraph[machine]);

            // add machine states
            foreach (var state in States[machine])
            {
                var old_node = StateToNode[Tuple.Create(state, machine)];

                var new_node = graph.AddNode(old_node.Id);
                new_node.Label.FontSize = NodeFontSize;
                new_node.LabelText = state;

                MachineToSubgraph[machine].AddNode(new_node);
                StateToNode[Tuple.Create(state, machine)] = new_node;
            }

            // Add intra-machine transitions
            foreach (var t in self)
            {
                var edge = graph.AddEdge(GetNode(t.state_from, t.machine_from).Id, t.edge_label, GetNode(t.state_to, t.machine_to).Id);
                edge.Label.FontSize = EdgeFontSize;
                Transitions[t] = edge;
            }

            // Add inter-machine transitions
            foreach (var t in outgoing.Concat(incoming))
            {
                var edge = graph.AddEdge(GetNode(t.state_from, t.machine_from).Id, t.edge_label, GetNode(t.state_to, t.machine_to).Id);
                edge.Label.FontSize = EdgeFontSize;
                Transitions[t] = edge;
            }
        }

        private void Collapse(string machine)
        {
            if (CollapsedMachines.Contains(machine))
                return;

            CollapsedMachines.Add(machine);
            
            // delete transitions of the machine
            foreach (var t in Transitions.Where(tr => tr.Key.machine_from == machine || tr.Key.machine_to == machine))
            {
                graph.RemoveEdge(t.Value);
            }
            
            // remove machine states
            foreach (var state in States[machine])
            {
                MachineToSubgraph[machine].RemoveNode(StateToNode[Tuple.Create(state, machine)]);
                graph.RemoveNode(StateToNode[Tuple.Create(state, machine)]);
            }

            graph.RootSubgraph.RemoveSubgraph(MachineToSubgraph[machine]);

            // add edges to the subgraph node
            var outgoing = new List<Transition>(
                Transitions.Where(tr => tr.Key.machine_from == machine && tr.Key.machine_to != machine)
                .Select(tr => tr.Key));

            var incoming = new List<Transition>(
                Transitions.Where(tr => tr.Key.machine_from != machine && tr.Key.machine_to == machine)
                .Select(tr => tr.Key));

            outgoing.ForEach(t => Transitions.Remove(t));
            incoming.ForEach(t => Transitions.Remove(t));

            foreach (var t in outgoing.Concat(incoming))
            {
                var edge = graph.AddEdge(GetNode(t.state_from, t.machine_from).Id, t.edge_label, GetNode(t.state_to, t.machine_to).Id);
                edge.Label.FontSize = EdgeFontSize;

                Transitions.Add(t, edge);
            }
        }

        protected Node GetNode(string state, string machine)
        {
            if (!CollapsedMachines.Contains(machine))
            {
                return StateToNode[Tuple.Create(state, machine)];
            }
            return MachineToSubgraph[machine];
        }

        protected void ProcessInput(string line)
        {
            var tok = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 0) return;

            switch (tok[0])
            {
                case "collapse":
                    lock(graph)
                    {
                        Collapse(tok[1]);
                    }
                    break;
                case "expand":
                    lock (graph)
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


    struct Transition
    {
        public string machine_from;
        public string state_from;
        public string edge_label;
        public string machine_to;
        public string state_to;

        public Transition(string machine_from, string state_from, string edge_label, string machine_to, string state_to)
        {
            this.machine_from = machine_from;
            this.state_from = state_from;
            this.edge_label = edge_label;
            this.machine_to = machine_to;
            this.state_to = state_to;
        }
    }
}