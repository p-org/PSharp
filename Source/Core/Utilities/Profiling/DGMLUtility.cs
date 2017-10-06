using System.Xml;
using System.Xml.Serialization;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// A node in the DGML representation
    /// </summary>
    public struct Node
    {
        /// <summary>
        /// Unique id for the node
        /// </summary>
        [XmlAttribute]
        public string Id;

        /// <summary>
        /// Label for the node
        /// </summary>
        [XmlAttribute]
        public string Label;

        /// <summary>
        /// The background color for this node
        /// </summary>
        [XmlAttribute]
        public string Background;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="label"></param>
        /// <param name="background"></param>
        public Node(string id, string label, string background = "White")
        {
            this.Id = id;
            this.Label = label;
            this.Background = background;
        }
    }

    /// <summary>
    /// An edge in the DGML representation
    /// </summary>
    public struct Link
    {
        /// <summary>
        /// The source node.
        /// </summary>
        [XmlAttribute]
        public string Source;

        /// <summary>
        /// The target node.
        /// </summary>
        [XmlAttribute]
        public string Target;

        /// <summary>
        /// The edge label.
        /// </summary>
        [XmlAttribute]
        public string Label;

        /// <summary>
        /// The edge color
        /// </summary>
        [XmlAttribute]
        public string Stroke;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="label"></param>
        /// <param name="stroke"></param>
        public Link(string source, string target, string label, string stroke = "Black")
        {
            this.Source = source;
            this.Target = target;
            this.Label = label;
            this.Stroke = stroke;
        }
    }

    /// <summary>
    /// The Graph used internally by the XML writer
    /// </summary>
    public struct Graph
    {
        /// <summary>
        /// Nodes to be serialized
        /// </summary>
        public Node[] Nodes;

        /// <summary>
        /// Links to be serialized
        /// </summary>
        public Link[] Links;
    }
}