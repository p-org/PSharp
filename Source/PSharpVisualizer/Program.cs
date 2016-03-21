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

namespace CoverageGraph
{
    class Driver
    {
        static void Main(string[] args)
        {
            var rg = new ProgramVisualizer();

            rg.AddMachine("server");
            rg.AddState("server", "Init");
            rg.AddState("server", "Playing");

            rg.AddMachine("client");
            rg.AddState("client", "Init");
            rg.AddState("client", "Playing");

            rg.AddTransition("server", "Init", "goto", "server", "Playing");
            rg.AddTransition("server", "Playing", "Pong", "client", "Playing");

            rg.AddTransition("client", "Init", "goto", "client", "Playing");
            rg.AddTransition("client", "Playing", "Ping", "server", "Playing");

            rg.run();
        }
    }

    class ProgramVisualizer
    {
        System.Windows.Forms.Form form;
        Graph graph;
        GViewer viewer;

        // statics
        double NodeFontSize = 8.0;
        double SubgraphFontSize = 8.0;
        double EdgeFontSize = 4.0;

        // flags
        bool stopped = false;

        // P# Program
        HashSet<string> Machines;
        Dictionary<string, HashSet<string>> States;
        Dictionary<Transition, Edge> Transitions;

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
        }

        public void AddMachine(string machine)
        {
            Debug.Assert(!Machines.Contains(machine));
            Machines.Add(machine);
            States.Add(machine, new HashSet<string>());

            var subgraph = new Subgraph(machine);
            subgraph.Label.FontSize = SubgraphFontSize;

            graph.RootSubgraph.AddSubgraph(subgraph);
            MachineToSubgraph.Add(machine, subgraph);
        }

        public void AddState(string machine, string state)
        {
            Debug.Assert(Machines.Contains(machine));
            Debug.Assert(!States[machine].Contains(state));

            States[machine].Add(state);

            var node = graph.AddNode(string.Format("{0}::{1}", state, machine));
            node.Label.FontSize = NodeFontSize;

            node.LabelText = state;
            MachineToSubgraph[machine].AddNode(node);

            StateToNode.Add(Tuple.Create(state, machine), node);
        }

        public void AddTransition(string machine_from, string state_from, string edgeLabel, string machine_to, string state_to)
        {
            Debug.Assert(Machines.Contains(machine_from) && Machines.Contains(machine_to));
            Debug.Assert(States[machine_from].Contains(state_from) && States[machine_to].Contains(state_to));

            var edge = graph.AddEdge(StateToNode[Tuple.Create(state_from, machine_from)].Id, edgeLabel, StateToNode[Tuple.Create(state_to, machine_to)].Id);
            edge.Label.FontSize = EdgeFontSize;

            Transitions.Add(new Transition(machine_from, state_from, edgeLabel, machine_to, state_to), edge);
        }

        public void run()
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

            viewer.ObjectUnderMouseCursorChanged += new EventHandler<Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs>(viewer_ObjectUnderMouseCursorChanged);
            viewer.MouseDown += new MouseEventHandler(viewer_MouseDown);
            viewer.SuspendLayout();

            form.SuspendLayout();
            form.Controls.Add(viewer as Control);

            viewer.ResumeLayout(false);
            form.ResumeLayout(false);

            viewer.Graph = graph;

            var thread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadRunner));
            thread.Start();
            form.ShowDialog();
            thread.Abort();
            //thread.Join();

            Console.WriteLine("exit");
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

        void refreshGraph()
        {
            if (!form.IsHandleCreated) return;

            // Save viewer state
            var z = viewer.ZoomF;
            // refresh
            viewer.Graph = graph;
            // Restore viewer state
            viewer.ZoomF = z;
        }

        private delegate void RefreshDelegate();

        protected void ThreadRunner()
        {
            while (true)
            {
                if (stopped) break;
                var line = Console.ReadLine();
                var quit = ProcessInput(line);

                if (form.IsHandleCreated)
                {
                    form.SuspendLayout();
                    form.Invoke(new RefreshDelegate(refreshGraph));
                    form.ResumeLayout();
                }
                if(quit) stopped = true;
            }
            form.Close();
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

        protected bool ProcessInput(string line)
        {
            var tok = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 0) return false;

            switch (tok[0])
            {
                case "exit":
                    return true;
                case "collapse":
                    Collapse(tok[1]);
                    break;
                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
            return false;
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