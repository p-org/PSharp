using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.TraceUtils
{
    /// <summary>
    /// Utility class where each node tracs how many unique leaves are reachable from it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CountingTrie<T>
    {
        /// <summary>
        /// A node in the counting trie
        /// </summary>
        public class CountingTrieNode
        {
            /// <summary>
            /// The value represented by the node
            /// </summary>
            public T value;
            /// <summary>
            /// The number of unique leaves reachable from here
            /// </summary>
            public int UniqueCount { get; internal set; }
            internal CountingTrieNode(T val)
            {
                this.value = val;
                UniqueCount = 0;
            }

            /// <summary>
            /// More descriptive for debugger
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"CTN[{value},{UniqueCount}]";
            }
        }

        CountingTrieNode root;
        Dictionary< Tuple<CountingTrieNode,T>, CountingTrieNode > edges;

        /// <summary>
        /// Constructor
        /// </summary>
        public CountingTrie()
        {
            root = new CountingTrieNode(default(T));
            edges = new Dictionary<Tuple<CountingTrieNode, T>, CountingTrieNode>();
        }


        /// <summary>
        /// Inserts a sequence into the trie
        /// </summary>
        /// <param name="seq"></param>
        public void insert(List<T> seq)
        {
            CountingTrieNode at = root;
            List<CountingTrieNode> path = new List<CountingTrieNode>();
            bool isNewSequence = false;
            path.Add(root);
            foreach (T s in seq)
            {
                var edgeKey = new Tuple<CountingTrieNode, T>(at, s);
                if (edges.ContainsKey(edgeKey))
                {
                    at = edges[edgeKey];
                }
                else
                {
                    CountingTrieNode newNode = new CountingTrieNode(s);
                    edges.Add(edgeKey, newNode);
                    at = newNode;
                    isNewSequence = true;
                }
                path.Add(at);
            }
            if (isNewSequence)
            {
                foreach (CountingTrieNode tn in path)
                {
                    tn.UniqueCount++;
                }
            }
        }
        /// <summary>
        /// Returns the child of the node 'parent' which has value 'childValue'
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childValue"></param>
        /// <returns></returns>
        public CountingTrieNode GetChild(CountingTrieNode parent, T childValue)
        {
            if (parent == null)
            {
                return root;
            }
            else {
                Tuple<CountingTrieNode, T> querykey = new Tuple<CountingTrieNode, T>(parent, childValue);
                if (edges.ContainsKey(querykey))
                {
                    return edges[querykey];
                }
                else
                {
                    return null;
                }
            }

        }
    }
}
