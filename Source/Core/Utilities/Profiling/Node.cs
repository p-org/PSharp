using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// A node in the Program Activity Graph (PAG)
    /// constructed by the critical path profiler
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Node<T> : INode
    {
        static long counter = 0;
        /// <summary>
        /// The unique id of this node
        /// </summary>
        public long Id { get; private set; }
        /// <summary>
        /// The data contained in this node
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public Node(T data)
        {
            this.Id = Interlocked.Increment(ref counter);
            this.Data = data;
        }

        /// <summary>
        /// A string representation of this node
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("[{0}]{1}", this.Id, this.Data);
        }
    }
}
