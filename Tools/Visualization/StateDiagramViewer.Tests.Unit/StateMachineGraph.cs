using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

/* Used by tests to verify correctness */
namespace Microsoft.PSharp.StateMachineStructureViewer.Tests.Unit
{
    #region Top Level Types
    public abstract class Edge
    {
        string eventName;
        string targetName;
        
        public Edge(string evt, string tgt)
        {
            eventName = (evt!=null)? evt : "(NULL)";
            targetName = tgt;
        }

        public override int GetHashCode() 
        {
            return (int)(((long)eventName.GetHashCode() + targetName.GetHashCode() + GetType().GetHashCode() )/3);
        }

        
        public override bool Equals(Object obj)
        {   Edge e = obj as Edge;
            return (e.GetType() == this.GetType() && e.eventName == this.eventName && e.targetName == this.targetName);
        }
        

        internal string DumpString()
        {
            return String.Format("({0},{1})", eventName, targetName);
        }
    }

    public abstract class Vertex
    {
        string vertexName;
        HashSet<Edge> edges;

        internal Vertex(string vName)
        {
            vertexName = vName;
            edges = new HashSet<Edge>();
        }
        internal void AddEdge(Edge edge)
        {
            edges.Add(edge);
        }

        public Vertex(string vName, Edge[] edgeArray)
        {
            vertexName = vName;
            edges = new HashSet<Edge>(edgeArray);
            if( edges.Count != edgeArray.Length)
            {
                throw new Exception("Duplicate edges going out of Vertex " + vertexName);
            }
        }

        
        public override int GetHashCode()
        {
            return (int)(((long)vertexName.GetHashCode() + GetType().GetHashCode()) / 2);
        }
        
        public override bool Equals(Object obj)
        {
            Vertex v = obj as Vertex;
            return (v.GetType() == this.GetType() && this.vertexName == v.vertexName);
        }

        internal bool DeepCheckEquality(Vertex otherVertex)
        {
            return (
                this.GetType() == otherVertex.GetType() &&
                this.vertexName == otherVertex.vertexName && 
                this.edges.Count == otherVertex.edges.Count && 
                this.edges.All(e => otherVertex.edges.Contains(e))
            );
        }

        public string DumpString()
        {
            string edgeStr = string.Join(", ", edges.Select(x=>x.DumpString()));
            return String.Format("{0}({1}){{ {2} }}", this.GetType().Name, this.vertexName, edgeStr);
        }
    }

    public class StateMachineGraph
    {
        HashSet<Vertex> vertices;
        public StateMachineGraph() {
            vertices = new HashSet<Vertex>();
        }
        // Handy constructor for Easy definition
        public StateMachineGraph(Vertex[] vertexArray)
        {
            vertices = new HashSet<Vertex>(vertexArray);
            if (vertices.Count != vertexArray.Length)
            {
                throw new Exception("Duplicate Vertices");
            }
        }

        public bool DeepCheckEquality(StateMachineGraph G)
        {
            if (G.vertices.Count != this.vertices.Count)
            {
                return false;
            }

            Dictionary<Vertex, Vertex> otherVertices = new Dictionary<Vertex, Vertex>();
            foreach (var v in this.vertices)
            {
                // TryGetValue is a funny thing
                otherVertices.Add(v, v);
            }
            foreach (var v in G.vertices)
            {
                if (!(otherVertices.ContainsKey(v) && v.DeepCheckEquality(otherVertices[v]))) {
                    return false;
                }
            }
            return true;
        }

        public string DumpString(){
            return String.Join( ", ", vertices.Select(v => v.DumpString()) );
        }

        internal void AddVertex(Vertex vertex)
        {
            vertices.Add(vertex);
        }
    }
    #endregion
    #region Specific Types
    public class MachineVertex : Vertex {
        internal MachineVertex(string vName) :
            base(vName)
        { }
        public MachineVertex(string vName, Edge[] edgeArray) : 
            base(vName, edgeArray) { }
    }
    public class StateVertex : Vertex {
        internal StateVertex(string vName) : 
            base(vName) { }

        public StateVertex(string vName, Edge[] edgeArray) :
            base(vName, edgeArray) { }
        
    }


    // Link representing a Contains relation
    public class ContainsLink : Edge
    {
        public ContainsLink(string evt, string tgt) :
            base(evt, tgt)
        { }
    }

    // Link representing GoTo transition
    public class GotoTransition : Edge
    {
        public GotoTransition(string evt, string tgt) : 
            base ( evt, tgt ) { }
    }

    // Link representing Push transition
    public class PushTransition : Edge
    {
        public PushTransition(string evt, string tgt) :
            base(evt, tgt)
        { }
    }
    #endregion

    #region dgml parser
    public class DgmlParser {

        private readonly static XNamespace dgmlNamespace  = "http://schemas.microsoft.com/vs/2009/dgml";

        public DgmlParser()
        {

        }
        public StateMachineGraph ParseDgml(XDocument xdoc)
        {
            
            StateMachineGraph G = new StateMachineGraph();
            Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>();
            HashSet<Edge> edges = new HashSet<Edge>();

            var vertexNodes = xdoc.Descendants(dgmlNamespace + "Node");
            var linkNodes = xdoc.Descendants(dgmlNamespace  + "Link");
            foreach (XElement node in vertexNodes)
            {
                Vertex v = ProcessNode(node, G);
                if (v != null) {
                    vertices.Add(node.Attribute("Id").Value, v);
                }
            }

            foreach (XElement node in linkNodes)
            {

                Edge e = ProcessLink(node);
                if (e != null)
                {
                    string sourceVertexName = node.Attribute("Source").Value;
                    //string targetVertexName = node.Attribute("Target").Value;
                    if (vertices.ContainsKey(sourceVertexName))
                    {
                        vertices[sourceVertexName].AddEdge(e);
                    }
                    else
                    {
                        throw new Exception("Unkown vertex with name: " + sourceVertexName);
                    }
                }
            }
            foreach ( var vertex in vertices.Values)
            {
                G.AddVertex(vertex);
            }
            return G;
        }

        private string FriendlyName(string localName)
        {
            return localName.Split( new char[]{'.'} ).Last();
        }

        private Vertex ProcessNode(XElement node, StateMachineGraph G)
        {
            Console.WriteLine(node.Attribute("Category").Value);
            switch (node.Attribute("Category").Value)
            {
                case "Machine":
                    return new MachineVertex(node.Attribute("Id").Value);

                case "State":
                    return new StateVertex(node.Attribute("Id").Value);

                default:
                    return null;
            }
        }

        private Edge ProcessLink(XElement node)
        {
            if (node.Attribute("Category") != null)
            {
                string evt = (node.Attribute("Event")!=null)? node.Attribute("Event").Value : null;
                string tgt = node.Attribute("Target").Value;
                switch (node.Attribute("Category").Value)
                {
                    case "Contains":
                        return new ContainsLink(evt, tgt);

                    case "GotoTransition":
                        return new GotoTransition(evt, tgt);

                    case "PushTransition":
                        return new PushTransition(evt, tgt);

                    default:
                        return null;
                }
            }
            else
            {
                return null;
            }

        }
    }
    #endregion


}
