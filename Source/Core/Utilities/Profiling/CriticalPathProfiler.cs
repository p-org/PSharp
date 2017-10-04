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
        /// Record when we started the critical path profiler
        /// </summary>
        internal long StartTime;

        /// <summary>
        /// Record when we stopeed the critical path profiler
        /// </summary>
        internal long StopTime;

        /// <summary>
        /// Record for how long we profiled the program in milliseconds
        /// </summary>
        internal long ProfiledTime;

        private static ConcurrentQueue<CriticalPathNode> PAGNodes;

        private static ConcurrentQueue<Tuple<long, long>> PAGEdges;

        private static ConcurrentDictionary<long, Tuple<long, long>> SenderInformation;

        /// <summary>
        /// Stopwatch.GetTimestamp gets the current number of ticks in the timer mechanism.
        /// The scaling factor helps convert this to elapsed milliseconds
        /// </summary>
        private static long ScalingFactor;

        /// <summary>
        /// The program activity graph that represents the execution of the PSharp program being profiled
        /// </summary>
        private BidirectionalGraph<CriticalPathNode, CriticalPathEdge> ProgramActivityGraph;

        static CriticalPathProfiler()
        {
            ScalingFactor = Stopwatch.Frequency / 1000L;
            SenderInformation = new ConcurrentDictionary<long, Tuple<long, long>>();
            PAGNodes = new ConcurrentQueue<CriticalPathNode>();
            PAGEdges = new ConcurrentQueue<Tuple<long, long>>();
        }

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
        /// The steps a profiler needs to take on entering an action in a machine
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="actionName"></param>
        public void OnActionEnter(Machine machine, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {                
                RecordPAGNodeAndEdge(machine, "Entering:" + actionName);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="actionName"></param>
        public void OnActionExit(Machine machine, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                // machine.LocalWatch.Stop();
                // machine.LongestPathTime += (machine.LocalWatch.ElapsedMilliseconds);
                // machine.LocalWatch.Reset();
                RecordPAGNodeAndEdge(machine, "Exiting:" + actionName);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public void OnCreateMachine(Machine parent, Machine child)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                child.LocalWatch.Start();
                if (parent == null)
                {
                    child.LongestPathTime = (Stopwatch.GetTimestamp() - this.StartTime) / ScalingFactor;
                    RecordPAGNodeAndEdge(child, "Created" + child.Id);
                }
                else
                {
                    child.LongestPathTime = parent.LongestPathTime + parent.LocalWatch.ElapsedMilliseconds;
                    RecordPAGNodeAndEdge(parent, "CreateMachine" + child.Id);
                    var targetId = RecordPAGNodeAndEdge(child, "Created:" + child.Id);
                    // record the cross edge
                    PAGEdges.Enqueue(new Tuple<long, long>(parent.predecessorId, targetId));
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnDequeue(Machine machine, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.LongestPathTime += machine.LocalWatch.ElapsedMilliseconds;
                Tuple<long, long> senderInfo;
                if (!SenderInformation.TryGetValue(eventSequenceNumber, out senderInfo))
                {
                    throw new Exception("Failed to retrieve key");
                }
                var sendNodeId = senderInfo.Item1;
                var senderLongestPath = senderInfo.Item2;                
                RecordPAGNodeAndEdge(machine, "Dequeue:" + eventSequenceNumber);
                machine.LongestPathTime = Math.Max(senderLongestPath, machine.LongestPathTime);
                if (sendNodeId != -1)
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
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="eventName"></param>
        public void OnReceiveBegin(Machine machine, string eventName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.IdleTimeStart = machine.LocalWatch.ElapsedMilliseconds;
                RecordPAGNodeAndEdge(machine, "ReceiveBegin:" + eventName);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="eventNames"></param>
        /// <param name="wasBlocked"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnReceiveEnd(Machine machine, string eventNames, bool wasBlocked, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.IdleTime += (machine.LocalWatch.ElapsedMilliseconds - machine.IdleTimeStart);
                // machine.LongestPathTime += machine.LocalWatch.ElapsedMilliseconds;
                Tuple<long, long> senderInfo;
                if (!SenderInformation.TryGetValue(eventSequenceNumber, out senderInfo))
                {
                    throw new Exception("Failed to retrieve key");
                }
                var sendNodeId = senderInfo.Item1;
                var senderLongestPath = senderInfo.Item2;
                RecordPAGNodeAndEdge(machine, "Dequeue:" + eventSequenceNumber);
                machine.LongestPathTime = Math.Max(senderLongestPath, machine.LongestPathTime);
                if (sendNodeId != -1)
                {
                    PAGEdges.Enqueue(new Tuple<long, long>(sendNodeId, machine.predecessorId));
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnSend(Machine source, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                long currentTimeStamp;
                if (source != null)
                {
                    // currentTimeStamp = source.LongestPathTime + source.LocalWatch.ElapsedMilliseconds;
                    RecordPAGNodeAndEdge(source, String.Format("Send({0}, {1})", source.Id, eventSequenceNumber));
                    SenderInformation.TryAdd(eventSequenceNumber, new Tuple<long, long>(source.predecessorId, source.LongestPathTime));
                }
                else
                {
                    currentTimeStamp = (Stopwatch.GetTimestamp() - this.StartTime) / ScalingFactor;
                    SenderInformation.TryAdd(eventSequenceNumber, new Tuple<long, long>(-1, currentTimeStamp));
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void StartCriticalPathProfiling()
        {
            this.StartTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// TBD
        /// </summary>
        public void StopCriticalPathProfiling()
        {
            this.StopTime = Stopwatch.GetTimestamp();
            this.ProfiledTime = (this.StopTime - this.StartTime) / ScalingFactor;
            // construct the PAG
            var nodes = PAGNodes.ToArray();
            foreach (var node in nodes)
            {
                ProgramActivityGraph.AddNode(node);
            }

            var edges = PAGEdges.ToArray();

            foreach (var pair in edges)
            {
                var source = ProgramActivityGraph.Nodes.Where(x => x.Id == pair.Item1).First();
                var target = ProgramActivityGraph.Nodes.Where(x => x.Id == pair.Item2).First();
                var timeDiff = target.Data.LongestElapsedTime - source.Data.LongestElapsedTime;
                ProgramActivityGraph.AddEdge(new TaggedEdge<Node<PAGNodeData>, long>(source, target, timeDiff));
            }

            var terminalNodes = ProgramActivityGraph.Nodes.Where(x => ProgramActivityGraph.OutDegree(x) == 0).ToList();
            var maxTime = terminalNodes.Max(x => x.Data.LongestElapsedTime);
            var data = new PAGNodeData("Sink", 0, maxTime);
            var sinkNode = new CriticalPathNode(data);
            ProgramActivityGraph.AddNode(sinkNode);
            HashSet<Node<PAGNodeData>> interesting = new HashSet<Node<PAGNodeData>>();
            foreach (var node in terminalNodes)
            {
                ProgramActivityGraph.AddEdge(new CriticalPathEdge(node, sinkNode, maxTime - node.Data.LongestElapsedTime));
                if (node.Data.LongestElapsedTime == maxTime)
                {
                    interesting.Add(node);
                }
            }

            // computing the actual path(s)
            foreach (var node in terminalNodes)
            {
                List<TaggedEdge<Node<PAGNodeData>, long>> criticalPathEdges =
                    new List<TaggedEdge<Node<PAGNodeData>, long>>();
                ComputeCP(node, criticalPathEdges);
                LogCP(criticalPathEdges);
            }

            // serialize the graph
            ProgramActivityGraph.Serialize(Configuration.OutputFilePath + "/PAG.dgml");
        }

        private void LogCP(List<TaggedEdge<Node<PAGNodeData>, long>> criticalPathEdges)
        {
            for (int i = criticalPathEdges.Count - 1; i >= 0; i--)
            {
                var e = criticalPathEdges[i];
                var stringRep = String.Format("{0}->{1}:{2}", e.Source, e.Target, e.Tag);
                this.Logger.WriteLine(stringRep);
            }
        }

        /// <summary>
        /// Records a node representing the latest interesting profiling event from the machine passed in,
        /// and an edge connecting the machine's previous event to this one.
        /// The nodes and edges are not directly added to the ProgramActivityGraph, which is not thread safe.
        /// They are cached for later processing.
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="extra">Extra diagnostic info</param>
        /// <returns></returns>
        private static long RecordPAGNodeAndEdge(Machine machine, string extra)
        {
            string currentStateName = machine.CurrentStateName;
            var nodeName = $"{currentStateName}:{extra}";
            var curTime = machine.LocalWatch.ElapsedMilliseconds;
            machine.LongestPathTime += curTime - machine.predecessorTimestamp;
            machine.predecessorTimestamp = curTime;
            var criticalPathTime = machine.LongestPathTime;
            var data = new PAGNodeData(nodeName, 0, criticalPathTime);
            var node = new Node<PAGNodeData>(data);
            if (machine.predecessorId != -1)
            {
                PAGEdges.Enqueue(new Tuple<long, long>(machine.predecessorId, node.Id));
            }
            PAGNodes.Enqueue(node);
            machine.predecessorId = node.Id;
            return node.Id;
        }

        private void ComputeCP(Node<PAGNodeData> source, List<TaggedEdge<Node<PAGNodeData>, long>> cp)
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
                TaggedEdge<Node<PAGNodeData>, long> cpPredecessor = inEdges.First();
                var maxTime = cpPredecessor.Source.Data.LongestElapsedTime;
                foreach (var e in inEdges)
                {
                    if (e.Source.Data.LongestElapsedTime > maxTime)
                    {
                        cpPredecessor = e;
                        maxTime = e.Source.Data.LongestElapsedTime;
                    }
                }
                ComputeCP(cpPredecessor.Source, cp);
            }
        }
    }
}