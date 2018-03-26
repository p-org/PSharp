using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    internal class StackDelta
    {
        /// <summary>
        /// Depth to which the stack was popped
        /// </summary>
        public int PopDepth { get; private set; }

        /// <summary>
        /// Pushed suffix
        /// </summary>
        public List<string> PushedSuffix { get; private set; }

        public StackDelta()
        {
            PopDepth = 0;
            PushedSuffix = new List<string>();
        }

        public void Pop()
        {
            if (PushedSuffix.Count > 0)
            {
                PushedSuffix.RemoveAt(PushedSuffix.Count - 1);
            }
            else
            {
                PopDepth++;
            }
        }

        public void Push(string elem)
        {
            PushedSuffix.Add(elem);
        }

    }
}
