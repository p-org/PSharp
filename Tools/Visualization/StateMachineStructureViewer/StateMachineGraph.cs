using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Used by tests to verify correctness */
namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    #region Top Level Types
    abstract class Edge
    {
        string eventName;
        string targetName;
        
        public Edge(string evt, string tgt)
        {
            eventName = evt;
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
    }

    abstract class Vertex
    {
        string vertexName;
        HashSet<Edge> edges;
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
                this.Equals(otherVertex) && 
                this.edges.Count == otherVertex.edges.Count && 
                this.edges.All(e => otherVertex.edges.Contains(e))
            );
        }
    }

    public class StateMachineGraph
    {
        HashSet<Vertex> vertices;
        // Handy constructor for Easy definition
        StateMachineGraph(Vertex [] vertexArray)
        {
            if (vertices.Count != vertexArray.Length)
            {
                throw new Exception("Duplicate Vertices");
            }
        }

        bool DeepCheckEquality(StateMachineGraph G)
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
            foreach(var v in G.vertices)
            {
                if ( ! (otherVertices.ContainsKey(v) && v.DeepCheckEquality(otherVertices[v]) ) ){
                    return false;
                }
            }
            return true;
        }
    }
    #endregion
    #region Specific Types
    class M : Vertex {
        public M(string vName, Edge[] edgeArray) : 
            base(vName, edgeArray) { }
    }
    class S : Vertex {
        public S(string vName, Edge[] edgeArray) :
            base(vName, edgeArray) { }
    }


    // Link representing a Contains relation
    class IN : Edge
    {
        public IN(string evt, string tgt) :
            base(evt, tgt)
        { }
    }

    // Link representing GoTo transition
    class GT : Edge
    {
        public GT(string evt, string tgt) : 
            base ( evt, tgt ) { }
    }

    // Link representing Push transition
    class PT : Edge
    {
        public PT(string evt, string tgt) :
            base(evt, tgt)
        { }
    }
    #endregion


}
