using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;


namespace Core.Utilities.Profiling
{
    /// <summary>
    /// A class representing a bidirectional directed graph.
    /// Inspired by the QuickGraph library (http://quickgraph.codeplex.com)
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    internal class BidirectionalGraph<TNode, TEdge> where TNode : INode
                                                    where TEdge : IEdge<TNode>
    {
        private readonly Dictionary<TNode, HashSet<TEdge>> InEdgesMap;
        private readonly Dictionary<TNode, HashSet<TEdge>> OutEdgesMap;

        private int edgeCount = 0;

        public BidirectionalGraph()
        {
            this.InEdgesMap = new Dictionary<TNode, HashSet<TEdge>>();
            this.OutEdgesMap = new Dictionary<TNode, HashSet<TEdge>>();
        }

        public bool IsNodesEmpty
        {
            get { return this.OutEdgesMap.Count == 0; }
        }

        public int NodeCount
        {
            get { return this.OutEdgesMap.Count; }
        }

        public virtual IEnumerable<TNode> Nodes
        {
            get { return this.OutEdgesMap.Keys; }
        }

        public bool ContainsNode(TNode v)
        {
            return this.OutEdgesMap.ContainsKey(v);
        }

        public bool IsOutEdgesEmpty(TNode v)
        {
            return this.OutEdgesMap[v].Count == 0;
        }

        public int OutDegree(TNode v)
        {
            return this.OutEdgesMap[v].Count;
        }

        public IEnumerable<TEdge> OutEdges(TNode v)
        {
            return this.OutEdgesMap[v];
        }

        public int InDegree(TNode v)
        {
            return this.InEdgesMap[v].Count;
        }

        public IEnumerable<TEdge> InEdges(TNode v)
        {
            return this.InEdgesMap[v];
        }

        public bool IsEdgesEmpty
        {
            get { return this.edgeCount == 0; }
        }

        public int EdgeCount
        {
            get
            {
                return this.edgeCount;
            }
        }

        public bool ContainsEdge(TEdge edge)
        {
            HashSet<TEdge> outEdges;
            return this.OutEdgesMap.TryGetValue(edge.Source, out outEdges) && outEdges.Contains(edge);
        }

        public bool TryGetInEdges(TNode v, out IEnumerable<TEdge> edges)
        {
            HashSet<TEdge> edgeSet;
            if (this.InEdgesMap.TryGetValue(v, out edgeSet))
            {
                edges = edgeSet.AsEnumerable();
                return true;
            }

            edges = null;
            return false;
        }

        public bool TryGetOutEdges(TNode v, out IEnumerable<TEdge> edges)
        {
            HashSet<TEdge> edgeSet;
            if (this.OutEdgesMap.TryGetValue(v, out edgeSet))
            {
                edges = edgeSet.AsEnumerable();
                return true;
            }

            edges = null;
            return false;
        }

        public virtual IEnumerable<TEdge> Edges
        {
            get
            {
                foreach (var edges in this.OutEdgesMap.Values)
                {
                    foreach (var edge in edges)
                    {
                        yield return edge;
                    }
                }
            }
        }

        public bool ContainsEdge(TNode source, TNode target)
        {
            IEnumerable<TEdge> outEdges;
            if (!this.TryGetOutEdges(source, out outEdges))
            {
                return false;
            }
            foreach (var outEdge in outEdges)
            {
                if (outEdge.Target.Equals(target))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetEdge(TNode source, TNode target, out TEdge edge)
        {
            HashSet<TEdge> edgeList;
            if (this.OutEdgesMap.TryGetValue(source, out edgeList) &&
                edgeList.Count > 0)
            {
                foreach (var e in edgeList)
                {
                    if (e.Target.Equals(target))
                    {
                        edge = e;
                        return true;
                    }
                }
            }
            edge = default(TEdge);
            return false;
        }

        public bool TryGetEdges(TNode source, TNode target, out IEnumerable<TEdge> edges)
        {
            HashSet<TEdge> edgeList;
            if (this.OutEdgesMap.TryGetValue(source, out edgeList))
            {
                HashSet<TEdge> list = new HashSet<TEdge>();
                foreach (var edge in edgeList)
                {
                    if (edge.Target.Equals(target))
                    {
                        list.Add(edge);
                    }
                }
                edges = list;
                return true;
            }
            else
            {
                edges = null;
                return false;
            }
        }

        public bool AddNode(TNode v)
        {
            if (this.ContainsNode(v))
            {
                return false;
            }
            this.InEdgesMap.Add(v, new HashSet<TEdge>());
            this.OutEdgesMap.Add(v, new HashSet<TEdge>());         
            return true;
        }

        public virtual int AddNodeRange(IEnumerable<TNode> vertices)
        {
            int count = 0;
            foreach (var v in vertices)
            {
                if (this.AddNode(v))
                {
                    count++;
                }
            }
            return count;
        }

        public virtual bool RemoveNode(TNode v)
        {
            if (!this.ContainsNode(v))
                return false;

            var edgesToRemove = new HashSet<TEdge>();
            foreach (var outEdge in this.OutEdges(v))
            {
                this.InEdgesMap[outEdge.Target].Remove(outEdge);
                edgesToRemove.Add(outEdge);
            }
            foreach (var inEdge in this.InEdges(v))
            {
                if (this.OutEdgesMap[inEdge.Source].Remove(inEdge))
                {
                    edgesToRemove.Add(inEdge);
                }
            }

            this.OutEdgesMap.Remove(v);
            this.InEdgesMap.Remove(v);
            this.edgeCount -= edgesToRemove.Count;

            return true;
        }

        public virtual bool AddEdge(TEdge e)
        {
            this.OutEdgesMap[e.Source].Add(e);
            this.InEdgesMap[e.Target].Add(e);
            this.edgeCount++;
            return true;
        }

        public int AddEdgeRange(IEnumerable<TEdge> edges)
        {
            int count = 0;
            foreach (var edge in edges)
            {
                if (this.AddEdge(edge))
                {
                    count++;
                }
            }
            return count;
        }

        public virtual bool AddNodesAndEdge(TEdge e)
        {
            this.AddNode(e.Source);
            this.AddNode(e.Target);
            return this.AddEdge(e);
        }

        public int AddNodesAndEdgeRange(IEnumerable<TEdge> edges)
        {
            int count = 0;
            foreach (var edge in edges)
            {
                if (this.AddNodesAndEdge(edge))
                {
                    count++;
                }
            }
            return count;
        }

        public virtual bool RemoveEdge(TEdge e)
        {
            if (this.OutEdgesMap[e.Source].Remove(e))
            {
                this.InEdgesMap[e.Target].Remove(e);
                this.edgeCount--;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ClearOutEdges(TNode v)
        {
            var outEdges = this.OutEdgesMap[v];
            foreach (var edge in outEdges)
            {
                this.InEdgesMap[edge.Target].Remove(edge);
            }

            this.edgeCount -= outEdges.Count;
            outEdges.Clear();
        }

        public void ClearInEdges(TNode v)
        {
            var inEdges = this.InEdgesMap[v];
            foreach (var edge in inEdges)
            {
                this.OutEdgesMap[edge.Source].Remove(edge);
            }

            this.edgeCount -= inEdges.Count;
            inEdges.Clear();
        }

        public void ClearEdges(TNode v)
        {
            ClearOutEdges(v);
            ClearInEdges(v);
        }

        public void Serialize(string xmlpath)
        {
            Graph g = new Graph();
            List<Node> nodes = new List<Node>();
            List<Link> edges = new List<Link>();

            foreach (var v in this.Nodes)
            {
                var node = new Node(v.Id.ToString(), v.ToString());
                nodes.Add(node);
            }
           
            foreach (var e in this.Edges)
            {
                var edge = new Link(e.Source.Id.ToString(), e.Target.Id.ToString(), e.ToString());
                edges.Add(edge);
            }

            g.Nodes = nodes.ToArray();
            g.Links = edges.ToArray();

            XmlRootAttribute root = new XmlRootAttribute("DirectedGraph");
            root.Namespace = "http://schemas.microsoft.com/vs/2009/dgml";
            XmlSerializer serializer = new XmlSerializer(typeof(Graph), root);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter xmlWriter = XmlWriter.Create(xmlpath, settings);
            serializer.Serialize(xmlWriter, g);
        }
    }
}