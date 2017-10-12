using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// A tagged edge.
    /// </summary>
    /// <typeparam name="TNode">Type of the vertices.</typeparam>
    /// <typeparam name="TTag">Type of the tag.</typeparam>
    [DebuggerDisplay("{Source}->{Target}:{Tag}")]
    public class TaggedEdge<TNode, TTag>
       : IEdge<TNode>
    {
        readonly TNode _Source;

        readonly TNode _Target;

        /// <summary>
        /// The tag associated with this edge
        /// </summary>
        public TTag Tag { get; set; }

        /// <summary>
        /// A tagged edge edge connecting source to target.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="tag">The tag.</param>
        public TaggedEdge(TNode source, TNode target, TTag tag)
        {            
            this._Source = source;
            this._Target = target;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets the source vertex
        /// </summary>
        /// <value></value>
        public TNode Source
        {
            get { return this._Source; }
        }

        /// <summary>
        /// Gets the target vertex
        /// </summary>
        /// <value></value>
        public TNode Target
        {
            get { return this._Target; }
        }
        
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return String.Format("{0}", this.Tag);
        }
    }

}
