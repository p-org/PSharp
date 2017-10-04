using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// TBD
    /// </summary>
    public class CriticalPathProfiler : ICriticalPathProfiler
    {
        /// <summary>
        /// Record when we started the critical path profiler
        /// </summary>
        internal long StartTime;

        private static ConcurrentQueue<Node<PAGNodeData>> PAGNodes;

        private static ConcurrentQueue<Tuple<long, long>> PAGEdges;

        private static ConcurrentDictionary<long, Tuple<long, long>> SenderInformation;

        /// <summary>
        /// Stopwatch.GetTimestamp gets the current number of ticks in the timer mechanism.
        /// The scaling factor helps convert this to elapsed milliseconds
        /// </summary>
        private static long ScalingFactor;

        private BidirectionalGraph<Node<PAGNodeData>, TaggedEdge<Node<PAGNodeData>, long>> ProgramActivityGraph;

        static CriticalPathProfiler()
        {
            ScalingFactor = Stopwatch.Frequency / 1000L;
            SenderInformation = new ConcurrentDictionary<long, Tuple<long, long>>();
            PAGNodes = new ConcurrentQueue<Node<PAGNodeData>>();
            PAGEdges = new ConcurrentQueue<Tuple<long, long>>();
        }

        /// <summary>
        /// TBD
        /// </summary>
        public Configuration Configuration { get; set; }

        private ILogger Logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public CriticalPathProfiler(Configuration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this.Logger = logger;
            ProgramActivityGraph = new BidirectionalGraph<Node<PAGNodeData>, TaggedEdge<Node<PAGNodeData>, long>>();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="actionName"></param>
        public void OnActionEnter(Machine machine, string currentStateName, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.LocalWatch.Start();
                TrackPAGActions(machine, currentStateName, actionName);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="actionName"></param>
        public void OnActionExit(Machine machine, string currentStateName, string actionName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.LocalWatch.Stop();
                machine.LongestPathTime += (machine.LocalWatch.ElapsedMilliseconds);
                machine.LocalWatch.Reset();
                TrackPAGActions(machine, currentStateName, actionName);
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
                if (parent == null)
                {
                    child.LongestPathTime = (this.StartTime - Stopwatch.GetTimestamp()) / ScalingFactor;
                    TrackPAGActions(child, child.CurrentStateName, "Created" + child.Id);
                }
                else
                {
                    child.LongestPathTime = parent.LongestPathTime;
                    TrackPAGActions(parent, parent.CurrentStateName, "CreateMachine" + child.Id);
                    var targetId = TrackPAGActions(child, child.CurrentStateName, "Created:" + child.Id);
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
                machine.LongestPathTime = Math.Max(senderLongestPath, machine.LongestPathTime);
                TrackPAGActions(machine, machine.CurrentStateName, "Dequeue:" + eventSequenceNumber);
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
        /// <param name="currentStateName"></param>
        /// <param name="eventName"></param>
        public void OnReceiveBegin(Machine machine, string currentStateName, string eventName)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.IdleTimeStart = machine.LocalWatch.ElapsedMilliseconds;
                TrackPAGActions(machine, currentStateName, "ReceiveBegin:" + eventName);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="currentStateName"></param>
        /// <param name="eventNames"></param>
        /// <param name="wasBlocked"></param>
        /// <param name="eventSequenceNumber"></param>
        public void OnReceiveEnd(Machine machine, string currentStateName, string eventNames, bool wasBlocked, long eventSequenceNumber)
        {
            if (Configuration.EnableCriticalPathProfiling)
            {
                machine.IdleTime += (machine.LocalWatch.ElapsedMilliseconds - machine.IdleTimeStart);
                machine.LongestPathTime += machine.LocalWatch.ElapsedMilliseconds;
                Tuple<long, long> senderInfo;
                if (!SenderInformation.TryGetValue(eventSequenceNumber, out senderInfo))
                {
                    throw new Exception("Failed to retrieve key");
                }
                var sendNodeId = senderInfo.Item1;
                var senderLongestPath = senderInfo.Item2;
                machine.LongestPathTime = Math.Max(senderLongestPath, machine.LongestPathTime);
                TrackPAGActions(machine, machine.CurrentStateName, "Dequeue:" + eventSequenceNumber);
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
                    currentTimeStamp = source.LongestPathTime + source.LocalWatch.ElapsedMilliseconds;
                    TrackPAGActions(source, source.CurrentStateName, String.Format("Send({0}, {1})", source.Id, eventSequenceNumber));
                    SenderInformation.TryAdd(eventSequenceNumber, new Tuple<long, long>(source.predecessorId, currentTimeStamp));
                }
                else
                {
                    currentTimeStamp = (this.StartTime - Stopwatch.GetTimestamp()) / ScalingFactor;
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

            var terminalNodes = ProgramActivityGraph.Nodes.Where(x => ProgramActivityGraph.OutDegree(x) == 0);
            var maxTime = terminalNodes.Max(x => x.Data.LongestElapsedTime);
            var data = new PAGNodeData("Sink", 0, maxTime);
            var sinkNode = new Node<PAGNodeData>(data);

            HashSet<Node<PAGNodeData>> interesting = new HashSet<Node<PAGNodeData>>();
            foreach (var node in terminalNodes)
            {
                ProgramActivityGraph.AddEdge(new TaggedEdge<Node<PAGNodeData>, long>(node, sinkNode, 0));
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

        private static long TrackPAGActions(Machine machine, string currentStateName, string actionName)
        {
            var nodeName = $"{currentStateName}:{actionName}";
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