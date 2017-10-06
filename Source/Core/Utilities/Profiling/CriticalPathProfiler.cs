using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Utilities.Profiling
{
    using CriticalPathEdge = TaggedEdge<Node<PAGNodeData>, long>;
    using CriticalPathNode = Node<PAGNodeData>;

    /// <summary>
    /// TBD
    /// </summary>
    public class CriticalPathProfiler : ICriticalPathProfiler
    {
        /// <summary>
        /// Record when we stopped the critical path profiler
        /// </summary>
        internal long StopTime;

        /// <summary>
        /// Record for how long we profiled the program in milliseconds
        /// </summary>
        internal long ProfiledTime;

        /// <summary>
        /// The configuration supplied by the PSharp runtime.
        /// Configuration.EnableCriticalPathProfiling controls
        /// whether profiling code should be triggered or not.
        /// </summary>
        public Configuration Configuration { get; set; }

        /// <summary>
        /// The logger to log the critical path(s) found to.
        /// </summary>
        private ILogger Logger;

        /// <summary>
        /// The program activity graph that represents the execution of the PSharp program being profiled
        /// </summary>
        private BidirectionalGraph<CriticalPathNode, CriticalPathEdge> ProgramActivityGraph;

        #region static fields

        /// <summary>
        /// Record when we started the critical path profiler
        /// </summary>
        internal static long StartTime;

        /// <summary>
        /// Cache containing nodes to be added to the ProgramActivityGraph
        /// </summary>
        private static ConcurrentQueue<CriticalPathNode> PAGNodes;

        /// <summary>
        /// Cache containing edges to be added to the ProgramActivityGraph
        /// </summary>
        private static ConcurrentQueue<Tuple<long, long>> PAGEdges;

        /// <summary>
        /// Updated at sends and read at dequeues to retrieve sender information
        /// Key - The sequence number of the send
        /// Value - (The id of the send node in the PAG, the longest path time at the send)
        /// </summary>
        private static ConcurrentDictionary<long, Tuple<long, long>> SenderInformation;

        /// <summary>
        /// Stopwatch.GetTimestamp gets the current number of ticks in the timer mechanism.
        /// The scaling factor helps convert this to elapsed milliseconds
        /// </summary>
        private static long ScalingFactor;

        #endregion static fields

        static CriticalPathProfiler()
        {
            ScalingFactor = Stopwatch.Frequency / 1000L;
            SenderInformation = new ConcurrentDictionary<long, Tuple<long, long>>();
            PAGNodes = new ConcurrentQueue<CriticalPathNode>();
            PAGEdges = new ConcurrentQueue<Tuple<long, long>>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">Logger</param>
        public CriticalPathProfiler(Configuration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this.Logger = logger;
            ProgramActivityGraph = new BidirectionalGraph<CriticalPathNode, CriticalPathEdge>();
        }

        /// <summary>
        /// Update profiler state on entering an action in a machine.
        /// </summary>
        /// <param name="machine">The machine running the action.</param>
        /// <param name="actionName">The name of the action being run.</param>
        public void OnActionEnter(Machine machine, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                RecordPAGNodeAndEdge(machine, actionName, PAGNodeType.ActionBegin);
            }
        }

        /// <summary>
        /// Update profiler state on leaving an action in a machine.
        /// </summary>
        /// <param name="machine">The machine running the action.</param>
        /// <param name="actionName">The name of the action being run.</param>
        public void OnActionExit(Machine machine, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                RecordPAGNodeAndEdge(machine, actionName, PAGNodeType.ActionEnd);
            }
        }

        /// <summary>
        /// Update profiler state on machine creation
        /// </summary>
        /// <param name="parent">The creator.</param>
        /// <param name="child">The machine just created.</param>
        public void OnCreateMachine(Machine parent, Machine child)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                if (parent == null) // the runtime created the machine
                {
                    RecordPAGNodeAndEdge(child, "RuntimeCreated", PAGNodeType.Child);
                }
                else
                {
                    // child.LongestPathTime = parent.LongestPathTime + parent.LocalWatch.ElapsedMilliseconds;
                    RecordPAGNodeAndEdge(parent, "", PAGNodeType.Creator);
                    var targetId = RecordPAGNodeAndEdge(child, "", PAGNodeType.Child);
                    PAGEdges.Enqueue(new Tuple<long, long>(parent.predecessorId, targetId));
                }
            }
        }

        /// <summary>
        /// Update profiler state when a machine sends a message
        /// </summary>
        /// <param name="source">The machine sending the message.</param>
        /// <param name="eventSequenceNumber">The global sequence number of this send.</param>
        public void OnSend(Machine source, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                long currentTimeStamp = (Stopwatch.GetTimestamp() - StartTime) / ScalingFactor;
                if (source != null)
                {
                    RecordPAGNodeAndEdge(source, String.Format("({0}, {1})", source.Id, eventSequenceNumber), PAGNodeType.Send);
                    SenderInformation.TryAdd(eventSequenceNumber, new Tuple<long, long>(source.predecessorId, currentTimeStamp));
                }
                else  // the runtime sent the message
                {
                    SenderInformation.TryAdd(eventSequenceNumber, new Tuple<long, long>(-1, currentTimeStamp));
                }
            }
        }

        /// <summary>
        /// Update profiler state when the machine dequeues a message
        /// </summary>
        /// <param name="machine">The machine dequeueing the message.</param>
        /// <param name="eventSequenceNumber">The global sequence number of the send corresponding to this dequeue</param>
        public void OnDequeueEnd(Machine machine, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                Tuple<long, long> senderInfo;
                if (!SenderInformation.TryGetValue(eventSequenceNumber, out senderInfo))
                {
                    throw new Exception("Failed to map dequeue to a send in the critical path profiler");
                }
                var sendNodeId = senderInfo.Item1;
                var senderLongestPath = senderInfo.Item2;
                RecordPAGNodeAndEdge(machine, eventSequenceNumber.ToString(), PAGNodeType.DequeueEnd, true);
                if (sendNodeId != -1)  // we don't represent enqueues from the runtime in the PAG
                {
                    PAGEdges.Enqueue(new Tuple<long, long>(sendNodeId, machine.predecessorId));
                }
            }
        }

        /// <summary>
        /// Update profiler state when a machine executes a receive action.
        /// </summary>
        /// <param name="machine">The machine executing the receive action.</param>
        /// <param name="eventName">The name of the event the machine is waiting for.</param>
        public void OnReceiveBegin(Machine machine, string eventName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                RecordPAGNodeAndEdge(machine, eventName, PAGNodeType.ReceiveBegin);
            }
        }

        /// <summary>
        /// Update profiler state when the machine receives the event it was waiting for.
        /// </summary>
        /// <param name="machine">The machine that was waiting for an event.</param>
        /// <param name="eventNames">The events being waited for.</param>
        /// <param name="wasBlocked">Whether the machine was blocked on the receive.</param>
        /// <param name="eventSequenceNumber">The global sequence number of the send
        /// corresponding to the dequeue that unblocked the machine.</param>
        public void OnReceiveEnd(Machine machine, string eventNames, bool wasBlocked, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                Tuple<long, long> senderInfo;
                if (!SenderInformation.TryGetValue(eventSequenceNumber, out senderInfo))
                {
                    throw new Exception("Failed to map dequeue (receive) to a send in the critical path profiler");
                }
                var sendNodeId = senderInfo.Item1;
                var senderLongestPath = senderInfo.Item2;
                var diagnostic = String.Format("[{0}]{1}:", wasBlocked, eventSequenceNumber);
                RecordPAGNodeAndEdge(machine, diagnostic, PAGNodeType.ReceiveEnd, true);
                if (sendNodeId != -1)  // we don't represent enqueues from the runtime in the PAG
                {
                    PAGEdges.Enqueue(new Tuple<long, long>(sendNodeId, machine.predecessorId));
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        public void OnHalt(Machine machine)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start the clock associated with the profiler.
        /// This lets us timestamp actions taken by the runtime
        /// such as machine creation and sends.
        /// </summary>
        public void StartCriticalPathProfiling()
        {
            StartTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Stop the profiler.
        /// Once this method is invoked, we flush the node cache PAGNodes,
        /// and the edge cache, PAGEdges, to construct the PAG.
        /// We then compute the critical path(s) and output a DGML representation
        /// of the Program Activity Graph for this execution.
        /// </summary>
        public void StopCriticalPathProfiling()
        {
            this.StopTime = Stopwatch.GetTimestamp();
            this.ProfiledTime = (this.StopTime - StartTime) / ScalingFactor;
            ConstructPAG();

            var terminalNodes = ProgramActivityGraph.Nodes.Where(x => ProgramActivityGraph.OutDegree(x) == 0).ToList();
            var maxTime = terminalNodes.Max(x => x.Data.Timestamp);
            var sinkNode = AddSinkNodeAndEdges(terminalNodes, maxTime);

            // computing the actual path(s)
            // terminalNodes.Where(x => x.Data.LongestElapsedTime == maxTime)
            var criticalEdges = ComputeCriticalPaths(new CriticalPathNode[] { sinkNode });

            // compute the top 3 activities that take the most time
            var topActivities = new HashSet<long>(GetTopKNodes(3).Select(x => x.Id));

            // serialize the graph
            var interesting = criticalEdges.Select(x => new Tuple<string, string>(x.Source.Id.ToString(), x.Target.Id.ToString()));
            var uniqueEdges = new HashSet<Tuple<string, string>>(interesting);
            var fileName = Configuration.OutputFilePath + Configuration.PAGFileName + ".dgml";
            ProgramActivityGraph.Serialize(Configuration.OutputFilePath + "/PAG.dgml", uniqueEdges, topActivities);
        }

        /// <summary>
        /// Compute and log the critical paths
        /// </summary>
        /// <param name="terminalNodes"></param>
        private HashSet<CriticalPathEdge> ComputeCriticalPaths(IEnumerable<CriticalPathNode> terminalNodes)
        {
            HashSet<CriticalPathEdge> CriticalEdges = new HashSet<CriticalPathEdge>();
            foreach (var node in terminalNodes)
            {
                List<CriticalPathEdge> criticalPathEdges = new List<CriticalPathEdge>();
                ComputeCP(node, criticalPathEdges);
                LogCP(criticalPathEdges);
                CriticalEdges.UnionWith(criticalPathEdges);
            }
            return CriticalEdges;
        }

        /// <summary>
        /// Add a sink node, and edges from the terminal node of each machine's sequence of actions
        /// in the PAG to the sink node.
        /// </summary>
        /// <param name="terminalNodes"></param>
        /// <param name="maxTime"></param>
        private CriticalPathNode AddSinkNodeAndEdges(List<CriticalPathNode> terminalNodes, long maxTime)
        {
            var data = new PAGNodeData("Sink", 0, PAGNodeType.Sink, maxTime);
            var sinkNode = new CriticalPathNode(data);
            ProgramActivityGraph.AddNode(sinkNode);
            HashSet<Node<PAGNodeData>> interesting = new HashSet<Node<PAGNodeData>>();
            foreach (var node in terminalNodes)
            {
                ProgramActivityGraph.AddEdge(new CriticalPathEdge(node, sinkNode, maxTime - node.Data.Timestamp));
                if (node.Data.Timestamp == maxTime)
                {
                    interesting.Add(node);
                }
            }
            return sinkNode;
        }

        /// <summary>
        /// Construct the PAG using the cached nodes and edges
        /// </summary>
        private void ConstructPAG()
        {
            var idToNode = new Dictionary<long, CriticalPathNode>();

            // set up nodes
            var nodes = PAGNodes.ToArray();
            foreach (var node in nodes)
            {
                ProgramActivityGraph.AddNode(node);
                idToNode[node.Id] = node;
            }

            // set up edges
            var edges = PAGEdges.ToArray();
            foreach (var pair in edges)
            {
                var source = idToNode[pair.Item1];
                var target = idToNode[pair.Item2];
                var timeDiff = target.Data.Timestamp - source.Data.Timestamp;
                ProgramActivityGraph.AddEdge(new TaggedEdge<Node<PAGNodeData>, long>(source, target, timeDiff));
            }
        }

        private void LogCP(List<CriticalPathEdge> criticalPathEdges)
        {
            for (int i = criticalPathEdges.Count - 1; i >= 0; i--)
            {
                var e = criticalPathEdges[i];
                var stringRep = String.Format("{0}->{1}:{2}", e.Source, e.Target, e.Tag);
                this.Logger.WriteLine(stringRep);
            }
        }

        /// <summary>
        /// Records a PAG node representing the latest interesting profiling event from the machine passed in,
        /// and an edge connecting the machine's previous event to this one.
        /// The nodes and edges are not directly added to the ProgramActivityGraph (which is not thread safe).
        /// They are cached for later processing.
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="extra">Extra diagnostic info</param>
        /// <param name="nodeType"></param>
        /// <param name="isDequeueEnd">is this a node representing a dequeue/receive end</param>
        /// <returns></returns>
        private static long RecordPAGNodeAndEdge(Machine machine, string extra, PAGNodeType nodeType, bool isDequeueEnd = false)
        {
            string currentStateName = machine.CurrentStateName;
            var nodeName = $"{machine.Id}:{currentStateName}:+{nodeType.ToString()}:{extra}";
            long currentTimeStamp = (Stopwatch.GetTimestamp() - StartTime) / ScalingFactor;
            long idleTime = isDequeueEnd ? Math.Max(currentTimeStamp - machine.predecessorTimestamp, 0) : 0;
            var data = new PAGNodeData(nodeName, idleTime, nodeType, currentTimeStamp);
            var node = new CriticalPathNode(data);
            if (machine.predecessorId != -1)
            {
                PAGEdges.Enqueue(new Tuple<long, long>(machine.predecessorId, node.Id));
            }
            PAGNodes.Enqueue(node);
            machine.predecessorId = node.Id;
            machine.predecessorTimestamp = currentTimeStamp;
            return node.Id;
        }

        private void ComputeCP(CriticalPathNode source, List<CriticalPathEdge> cp)
        {
            if (ProgramActivityGraph.InDegree(source) == 0)
            {
                return;
            }

            if (ProgramActivityGraph.InDegree(source) == 1)
            {
                var inEdge = ProgramActivityGraph.InEdges(source).First();
                cp.Add(inEdge);
                ComputeCP(inEdge.Source, cp);
            }
            else
            {
                var inEdges = ProgramActivityGraph.InEdges(source);
                CriticalPathEdge cpPredecessor = inEdges.First();
                var maxTime = cpPredecessor.Source.Data.Timestamp;
                foreach (var e in inEdges)
                {
                    if (e.Source.Data.Timestamp > maxTime)
                    {
                        cpPredecessor = e;
                        maxTime = e.Source.Data.Timestamp;
                    }
                }
                cp.Add(cpPredecessor);
                ComputeCP(cpPredecessor.Source, cp);
            }
        }

        private IEnumerable<CriticalPathNode> GetTopKNodes(int k)
        {
            if (k < 0)
            {
                yield break;
            }

            var actionNodes = ProgramActivityGraph.Nodes.Where(x => x.Data.NodeType == PAGNodeType.ActionBegin);

            if (actionNodes.Count() < k)
            {
                yield break;
            }

            var actionStartEnd = actionNodes.Select(x => new Tuple<CriticalPathNode, CriticalPathNode>(x, GetCorrespondingActionEnd(x)));
            var topK = actionStartEnd.OrderByDescending(x => (x.Item2.Data.Timestamp - x.Item1.Data.Timestamp)).Take(k);
            foreach (var pair in topK)
            {
                yield return pair.Item1;
                yield return pair.Item2;
            }
        }

        /// <summary>
        /// This method assumes that profiling has stopped at a consistent point,
        /// i.e every action begin has a corresponding action end.
        /// </summary>
        /// <param name="actionBeginNode"></param>
        /// <returns></returns>
        private CriticalPathNode GetCorrespondingActionEnd(CriticalPathNode actionBeginNode)
        {
            System.Diagnostics.Debug.Assert(actionBeginNode.Data.NodeType == PAGNodeType.ActionBegin, "Require action begin node");            
            CriticalPathNode successor = ProgramActivityGraph.OutEdges(actionBeginNode).First().Target;
            while (successor.Data.NodeType != PAGNodeType.ActionEnd)
            {
                if (ProgramActivityGraph.OutDegree(successor) == 1)
                {
                    successor = ProgramActivityGraph.OutEdges(successor).First().Target;
                }
                else
                {
                    successor = ProgramActivityGraph.OutEdges(successor).Where(x => !IsDequeueEndOrReceiveEnd(x.Target)).First().Target;
                }
            }
            return successor;
        }

        private bool IsDequeueEndOrReceiveEnd(CriticalPathNode node)
        {
            return node.Data.NodeType == PAGNodeType.DequeueEnd 
                || node.Data.NodeType == PAGNodeType.ReceiveEnd;

        }
    }
}