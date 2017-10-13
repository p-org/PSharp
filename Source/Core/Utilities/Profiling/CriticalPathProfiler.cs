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
        /// Constructor that installs this profiler on the runtime passed in,
        /// and begins profiling
        /// </summary>
        /// <param name="runtime"></param>
        public CriticalPathProfiler(PSharpRuntime runtime)
        {
            this.Configuration = runtime.Configuration;
            this.Logger = runtime.Logger;
            ProgramActivityGraph = new BidirectionalGraph<CriticalPathNode, CriticalPathEdge>();
            if (Configuration.EnableCriticalPathProfiling == false)
            {
                Configuration.EnableCriticalPathProfiling = true;
            }
            runtime.SetCriticalPathProfiler(this);
            this.StartCriticalPathProfiling();
        }

        /// <summary>
        /// Update profiler state on entering an action in a machine.
        /// </summary>
        /// <param name="machine">The machine running the action.</param>
        /// <param name="actionName">The name of the action being run.</param>
        public void OnActionEnter(Machine machine, string actionName)
        {
            RecordPAGNodeAndEdge(machine, actionName, PAGNodeType.ActionBegin);
        }

        /// <summary>
        /// Update profiler state on leaving an action in a machine.
        /// </summary>
        /// <param name="machine">The machine running the action.</param>
        /// <param name="actionName">The name of the action being run.</param>
        public void OnActionExit(Machine machine, string actionName)
        {
            RecordPAGNodeAndEdge(machine, actionName, PAGNodeType.ActionEnd);
        }

        /// <summary>
        /// Update profiler state on machine creation
        /// </summary>
        /// <param name="parent">The creator.</param>
        /// <param name="child">The machine just created.</param>
        public void OnCreateMachine(Machine parent, Machine child)
        {
            if (parent == null) // the runtime created the machine
            {
                RecordPAGNodeAndEdge(child, "", PAGNodeType.Child);
            }
            else
            {
                // child.LongestPathTime = parent.LongestPathTime + parent.LocalWatch.ElapsedMilliseconds;
                RecordPAGNodeAndEdge(parent, "", PAGNodeType.Creator);
                var targetId = RecordPAGNodeAndEdge(child, "", PAGNodeType.Child);
                PAGEdges.Enqueue(new Tuple<long, long>(parent.predecessorId, targetId));
            }
        }

        /// <summary>
        /// Update profiler state when a machine sends a message
        /// </summary>
        /// <param name="source">The machine sending the message.</param>
        /// <param name="eventSequenceNumber">The global sequence number of this send.</param>
        public void OnSend(Machine source, long eventSequenceNumber)
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

        /// <summary>
        /// Update profiler state when the machine dequeues a message
        /// </summary>
        /// <param name="machine">The machine dequeueing the message.</param>
        /// <param name="eventSequenceNumber">The global sequence number of the send corresponding to this dequeue</param>
        public void OnDequeueEnd(Machine machine, long eventSequenceNumber)
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

        /// <summary>
        /// Update profiler state when a machine executes a receive action.
        /// </summary>
        /// <param name="machine">The machine executing the receive action.</param>
        /// <param name="eventName">The name of the event the machine is waiting for.</param>
        public void OnReceiveBegin(Machine machine, string eventName)
        {
            RecordPAGNodeAndEdge(machine, eventName, PAGNodeType.ReceiveBegin);
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
            CleanUpPAG();
            var sinkNode = AddSinkNodeAndEdges();

            // computing the actual path(s)
            // terminalNodes.Where(x => x.Data.LongestElapsedTime == maxTime)
            var criticalEdges = ComputeCriticalPaths(new CriticalPathNode[] { sinkNode });

            // compute the top activities that take the most time
            HashSet<long> topActivities = GetTopActivities(10);

            // serialize the graph
            var interesting = criticalEdges.Select(x => new Tuple<string, string>(x.Source.Id.ToString(), x.Target.Id.ToString()));
            var uniqueEdges = new HashSet<Tuple<string, string>>(interesting);
            if (Configuration.PAGFileName == null || Configuration.PAGFileName.Length == 0)
            {
                Configuration.PAGFileName = "PAG";
            }
            var fileName = Configuration.OutputFilePath + "\\" + Configuration.PAGFileName + ".dgml";
            ProgramActivityGraph.Serialize(fileName, uniqueEdges, topActivities);
        }

        /// <summary>
        /// Recompute CP on optimizing action "actionName" by optfactor
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="optFactor"></param>
        public void Query(string actionName, int optFactor)
        {
            var sources = GetPAGNodesForAction(actionName);
            var compressedEdges = GetEdgesToCompress(actionName);
            Queue<CriticalPathNode> worklist = new Queue<CriticalPathNode>();
            foreach (var source in sources)
            {
                worklist.Enqueue(source);
            }

            while (worklist.Any())
            {
                var current = worklist.Dequeue();
                foreach (var edge in ProgramActivityGraph.OutEdges(current))
                {
                    var target = edge.Target;
                    if (target.Data.NodeType == PAGNodeType.Sink)
                    {
                        continue;
                    }
                    if (compressedEdges.Contains(edge))
                    {
                        edge.Tag = edge.Tag / optFactor;
                    }

                    if (target == current.Data.MachineSuccessor)
                    {
                        if (!IsDequeueEndOrReceiveEnd(target))
                        {
                            target.Data.Timestamp = (current.Data.Timestamp + edge.Tag);
                        }
                        else
                        {
                            var otherPredecessor = GetOtherMachinePredecessor(current);
                            var senderTimeStamp = otherPredecessor.Data.Timestamp;
                            var currentTimeStamp = current.Data.Timestamp;
                            UpdateTarget(target, senderTimeStamp, currentTimeStamp);
                        }
                    }
                    // This is a dequeueEnd/child node
                    else
                    {
                        if (target.Data.NodeType == PAGNodeType.Child)
                        {
                            target.Data.Timestamp = current.Data.Timestamp + edge.Tag;
                        }
                        else
                        {
                            var senderTimeStamp = current.Data.Timestamp;
                            var currentTimeStamp = target.Data.MachinePredecessor.Data.Timestamp;
                            UpdateTarget(target, senderTimeStamp, currentTimeStamp);
                        }
                    }
                    if (target.Data.NodeType != PAGNodeType.Sink)
                    {
                        worklist.Enqueue(target);
                    }
                }
            }

            // Remove the existing sink node and add a new one reflecting the new CP
            var sinkNode = ProgramActivityGraph.Nodes.Where(x => ProgramActivityGraph.OutDegree(x) == 0).First();
            ProgramActivityGraph.RemoveNode(sinkNode);

            sinkNode = AddSinkNodeAndEdges();
            var criticalEdges = ComputeCriticalPaths(new CriticalPathNode[] { sinkNode });

            // compute the top activities that take the most time
            var topActivities = GetTopActivities(10);

            // serialize the graph
            var interesting = criticalEdges.Select(x => new Tuple<string, string>(x.Source.Id.ToString(), x.Target.Id.ToString()));
            var uniqueEdges = new HashSet<Tuple<string, string>>(interesting);
            var fileName = Configuration.OutputFilePath + "\\" + Configuration.PAGFileName + ".Opt" + ".dgml";
            ProgramActivityGraph.Serialize(fileName, uniqueEdges, topActivities);
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
        private CriticalPathNode AddSinkNodeAndEdges()
        {
            var terminalNodes = ProgramActivityGraph.Nodes.Where(x => ProgramActivityGraph.OutDegree(x) == 0).ToList();
            var maxTime = terminalNodes.Max(x => x.Data.Timestamp);
            var data = new PAGNodeData(null, "Sink", 0, PAGNodeType.Sink, maxTime, -1);
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
                if (source.Data.Mid == target.Data.Mid &&
                    !(source.Data.NodeType == PAGNodeType.Send &&
                    target.Data.NodeType == PAGNodeType.DequeueEnd)) // there could be send -> deqEnd on the same machine
                {
                    source.Data.MachineSuccessor = target;
                    target.Data.MachinePredecessor = source;
                }
                var timeDiff = target.Data.Timestamp - source.Data.Timestamp;
                ProgramActivityGraph.AddEdge(new TaggedEdge<Node<PAGNodeData>, long>(source, target, timeDiff));
            }
        }

        private void LogCP(List<CriticalPathEdge> criticalPathEdges)
        {
            var original = Logger.LoggingVerbosity;
            Logger.LoggingVerbosity = 0;
            this.Logger.WriteLine("Writing a CP");
            for (int i = criticalPathEdges.Count - 1; i >= 0; i--)
            {
                var e = criticalPathEdges[i];
                var stringRep = String.Format("{0}->{1}:{2}", e.Source, e.Target, e.Tag);
                this.Logger.WriteLine(stringRep);
            }
            Logger.LoggingVerbosity = original;
        }

        /// <summary>
        /// Records a PAG node representing the latest interesting profiling event from the machine passed in,
        /// and an edge connecting the machine's previous event to this one.
        /// The nodes and edges are not directly added to the ProgramActivityGraph (which is not thread safe).
        /// They are cached for later processing.
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="actionName">Extra diagnostic info</param>
        /// <param name="nodeType"></param>
        /// <param name="isDequeueEnd">is this a node representing a dequeue/receive end</param>
        /// <returns></returns>
        private static long RecordPAGNodeAndEdge(Machine machine, string actionName, PAGNodeType nodeType, bool isDequeueEnd = false)
        {
            string currentStateName = machine.CurrentStateName;
            long currentTimeStamp = (Stopwatch.GetTimestamp() - StartTime) / ScalingFactor;
            long idleTime = isDequeueEnd ? Math.Max(currentTimeStamp - machine.predecessorTimestamp, 0) : 0;
            var data = new PAGNodeData(machine, actionName, idleTime, nodeType, currentTimeStamp, (long)machine.Id.Value);
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
            var successor = actionBeginNode.Data.MachineSuccessor;
            while (successor.Data.NodeType != PAGNodeType.ActionEnd)
            {
                successor = successor.Data.MachineSuccessor;
            }
            return successor;
        }

        private bool IsDequeueEndOrReceiveEnd(CriticalPathNode node)
        {
            return node.Data.NodeType == PAGNodeType.DequeueEnd
                || node.Data.NodeType == PAGNodeType.ReceiveEnd;
        }

        private bool IsSendOrCreate(CriticalPathNode node)
        {
            return node.Data.NodeType == PAGNodeType.Send
                || node.Data.NodeType == PAGNodeType.Creator;
        }

        private IEnumerable<CriticalPathNode> GetPAGNodesForAction(string actionName)
        {
            return ProgramActivityGraph.Nodes.Where(x => x.Data.ActionName.Equals(actionName)
                               && x.Data.NodeType == PAGNodeType.ActionBegin);
        }

        private HashSet<CriticalPathEdge> GetEdgesToCompress(string actionName)
        {
            HashSet<CriticalPathEdge> result = new HashSet<CriticalPathEdge>();
            foreach (var source in GetPAGNodesForAction(actionName))
            {
                var actionEnd = GetCorrespondingActionEnd(source);
                var node = source;
                while (node != actionEnd)
                {
                    foreach (var edge in ProgramActivityGraph.OutEdges(node))
                    {
                        if (edge.Target == node.Data.MachineSuccessor)
                        {
                            result.Add(edge);
                            node = edge.Target;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private CriticalPathNode GetOtherMachineSuccessor(CriticalPathNode node)
        {
            System.Diagnostics.Debug.Assert(IsSendOrCreate(node),
                "Querying for other machine successor for a node without any");
            return ProgramActivityGraph.OutEdges(node)
                    .Where(x => x.Target.Data.Mid.Value != node.Data.Mid.Value)
                    .Select(x => x.Target).First();
        }

        private CriticalPathNode GetOtherMachinePredecessor(CriticalPathNode node)
        {
            System.Diagnostics.Debug.Assert(IsDequeueEndOrReceiveEnd(node),
                "Querying for other machine predecessor for a node without any");
            return ProgramActivityGraph.InEdges(node)
                    .Where(x => x.Target.Data.Mid.Value != node.Data.Mid.Value)
                    .Select(x => x.Target).First();
        }

        private void UpdateTarget(CriticalPathNode target, long senderTimeStamp, long currentTimeStamp)
        {
            target.Data.Timestamp = Math.Max(senderTimeStamp, currentTimeStamp);
            target.Data.IdleTime = Math.Max(senderTimeStamp - currentTimeStamp, 0);
            foreach (var e in ProgramActivityGraph.InEdges(target))
            {
                e.Tag = e.Target.Data.Timestamp - e.Source.Data.Timestamp;
            }
        }

        /// <summary>
        /// Output the top k nodes where k = (total #action nodes/fraction)
        /// </summary>
        /// <param name="fraction"></param>
        /// <returns></returns>
        private HashSet<long> GetTopActivities(int fraction)
        {
            var numOfActionNodes = ProgramActivityGraph.Nodes.Where(x => x.Data.NodeType == PAGNodeType.ActionBegin).Count();
            var k = Math.Max(numOfActionNodes / fraction, numOfActionNodes > 0 ? 1 : 0);
            var topActivities = new HashSet<long>(GetTopKNodes(k).Select(x => x.Id));
            this.Logger.WriteLine("Logging top activities:");
            foreach (var activity in topActivities)
            {
                this.Logger.WriteLine(activity.ToString());
            }
            return topActivities;
        }

        /// <summary>
        /// When we stop critical path profiling, we add fake action end nodes, for all
        /// machines whose last node isn't an action end/create/child
        /// </summary>
        private void CleanUpPAG()
        {
            HashSet<CriticalPathNode> toAddNodes = new HashSet<CriticalPathNode>();
            HashSet<CriticalPathEdge> toAddEdges = new HashSet<CriticalPathEdge>();
            var terminalNodes = ProgramActivityGraph.Nodes.Where(x => ProgramActivityGraph.OutDegree(x) == 0).ToList();
            foreach (var node in terminalNodes)
            {
                var nodeType = node.Data.NodeType;
                if (IsSafeNodeType(nodeType))
                {
                    continue;
                }

                var pred = node;

                while (pred != null && pred.Data.NodeType != PAGNodeType.ActionBegin)
                {
                    pred = pred.Data.MachinePredecessor;
                }

                System.Diagnostics.Debug.Assert(pred != null, "Ill formed PAG");

                var actionName = pred.Data.ActionName;

                var data = new PAGNodeData(pred.Data.StateName, pred.Data.ActionName, 0,
                    PAGNodeType.ActionEnd, node.Data.Timestamp, node.Data.Mid, node.Data.parentId);
                var fakeEnd = new CriticalPathNode(data);
                node.Data.MachineSuccessor = fakeEnd;
                fakeEnd.Data.MachinePredecessor = node;
                toAddNodes.Add(fakeEnd);
                toAddEdges.Add(new CriticalPathEdge(node, fakeEnd, 0));
            }
            ProgramActivityGraph.AddNodeRange(toAddNodes);
            ProgramActivityGraph.AddEdgeRange(toAddEdges);
        }

        private bool IsSafeNodeType(PAGNodeType nodeType)
        {
            return nodeType == PAGNodeType.ActionEnd || nodeType == PAGNodeType.Creator || nodeType == PAGNodeType.Child;
        }
    }
}