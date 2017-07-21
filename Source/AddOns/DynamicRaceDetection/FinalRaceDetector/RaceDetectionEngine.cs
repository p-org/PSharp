using Microsoft.PSharp;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Tracing.Machines;
using Microsoft.PSharp.Utilities;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ThreadTraces;

namespace FinalRaceDetector
{
    public class RaceDetectionEngine
    {
        #region fields

        private Profiler Profiler;

        /// <summary>
        /// The P# configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The happens-before graph.
        /// </summary>
        List<ThreadTrace> AllThreadTraces;

        BidirectionalGraph<Node, Edge> CGraph;

        int VcCount = 0;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public RaceDetectionEngine(Configuration configuration)
        {
            this.Configuration = configuration;

            this.AllThreadTraces = new List<ThreadTrace>();
            this.CGraph = new BidirectionalGraph<Node, Edge>();
            Profiler = new Profiler();
        }

        /// <summary>
        /// Starts the engine.
        /// </summary>
        public bool Start()
        {
            Output.WriteLine(". Searching for data races");

            string directoryPath = Path.GetDirectoryName(this.Configuration.AssemblyToBeAnalyzed) +
                    Path.DirectorySeparatorChar + "Output";
            string runtimeTraceDirectoryPath = directoryPath + Path.DirectorySeparatorChar +
                "RuntimeTraces" + Path.DirectorySeparatorChar;
            string threadTraceDirectoryPath = directoryPath + Path.DirectorySeparatorChar +
                "ThreadTraces" + Path.DirectorySeparatorChar;

            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }

            string[] fileEntries = Directory.GetFiles(threadTraceDirectoryPath, "*");
            int NumberOfIterations = 0;
            foreach (string fileName in fileEntries)
            {
                string iterationStart = fileName.Substring(fileName.IndexOf("iteration_") + 10);
                string iterationNumber = iterationStart.Substring(0, iterationStart.IndexOf('_'));
                int currentIteration = Int32.Parse(iterationNumber);
                if (currentIteration > NumberOfIterations)
                    NumberOfIterations = currentIteration;
            }

            string[] tfileEntries = Directory.GetFiles(threadTraceDirectoryPath, ("*_iteration_" + NumberOfIterations + "*"));
            foreach (string fileName in tfileEntries)
            {
                //Deserialize thread traces
                //Open the file written above and read values from it.
                Stream stream = File.Open(fileName, FileMode.Open);
                BinaryFormatter bformatter = new BinaryFormatter();
                List<ThreadTrace> tt = (List<ThreadTrace>)bformatter.Deserialize(stream);

                for (int i = 0; i < tt.Count; i++)
                {
                    this.AllThreadTraces.Add(tt[i]);
                }
                stream.Close();
            }

            string[] mFileEntries = Directory.GetFiles(runtimeTraceDirectoryPath, ("*_iteration_" + NumberOfIterations + "*"));
            this.VcCount = mFileEntries.Count() + 5;

            //TODO: fix this            
            foreach (string fileName in mFileEntries)
            {
                Output.WriteLine(". File name: " + fileName);
                //chain decomposition
                string fileNumberStart = fileName.Substring(fileName.LastIndexOf('_') + 1);
                string fileNumber = fileNumberStart.Substring(0, fileNumberStart.IndexOf('.'));
                int tc = Int32.Parse(fileNumber);
                if (tc > this.VcCount)
                    this.VcCount = tc;
            }
            this.VcCount = this.VcCount + 10;


            foreach (string fileName in mFileEntries)
            {
                MachineActionTrace machineTrace = null;
                using (FileStream stream = File.Open(fileName, FileMode.Open))
                {
                    DataContractSerializer serializer = new DataContractSerializer(
                        typeof(MachineActionTrace));
                    machineTrace = serializer.ReadObject(stream) as MachineActionTrace;
                }

                this.UpdateTasks(machineTrace);
                this.UpdateGraph(machineTrace);
            }

            this.UpdateGraphCrossEdges();
            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StopMeasuringExecutionTime();
                Output.WriteLine("... Graph construction runtime: '" +
                    this.Profiler.Results() + "' seconds.");
            }

            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }
            //Console.WriteLine("before pruning: nodes = " + CGraph.VertexCount + "; edges = " + CGraph.EdgeCount);
            this.PruneGraph();
            //Console.WriteLine("after pruning: nodes = " + CGraph.VertexCount + "; edges = " + CGraph.EdgeCount);
            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StopMeasuringExecutionTime();
                Output.WriteLine("... Graph prune runtime: '" +
                    this.Profiler.Results() + "' seconds.");
            }

            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }
            this.UpdateVectorsT();
            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StopMeasuringExecutionTime();
                Output.WriteLine("... Topological sort runtime: '" +
                    this.Profiler.Results() + "' seconds.");
            }

            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }
            bool foundBug = this.DetectRacesFast();
            if (this.Configuration.EnableProfiling)
            {
                this.Profiler.StopMeasuringExecutionTime();
                Output.WriteLine("... Race detection runtime: '" +
                    this.Profiler.Results() + "' seconds.");
            }

            this.CGraph.Clear();
            this.AllThreadTraces.Clear();
            return foundBug;
        }

        /// <summary>
        /// Updates the tasks.
        /// </summary>
        /// <param name="machineTrace">MachineActionTrace</param>
        void UpdateTasks(MachineActionTrace machineTrace)
        {
            int currentMachineVC = 0;

            foreach (MachineActionInfo info in machineTrace)
            {
                if (info.Type == MachineActionType.TaskMachineCreation)
                {
                    //ThreadTrace matching = null;
                    var matching = this.AllThreadTraces.Where(item => item.IsTask && item.TaskId == info.TaskId);
                    if (matching.Count() == 0)
                        continue;

                    Node cn = new CActBegin(info.TaskMachineId.GetHashCode(), info.TaskId);
                    ((CActBegin)cn).IsStart = true;
                    cn.VectorClock = new int[this.VcCount];
                    currentMachineVC++;
                    try
                    {
                        cn.VectorClock[info.TaskMachineId.GetHashCode()] = currentMachineVC;
                    }
                    catch (Exception ex)
                    {
                        Output.WriteLine("failed: " + this.VcCount + " " + info.TaskMachineId);
                        Output.WriteLine(ex.ToString());
                        Environment.Exit(Environment.ExitCode);
                    }
                    this.CGraph.AddVertex(cn);

                    foreach (var m in matching)
                    {
                        if (m.Accesses.Count > 0)
                        {
                            foreach (ActionInstr ins in m.Accesses)
                            {
                                ((CActBegin)cn).Addresses.Add(new MemAccess(ins.IsWrite, ins.Location, ins.ObjHandle, ins.Offset,
                                    ins.SrcLocation, info.TaskMachineId.GetHashCode()));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the graph.
        /// </summary>
        /// <param name="machineTrace">MachineActionTrace</param>
        void UpdateGraph(MachineActionTrace machineTrace)
        {
            int currentMachineVC = 0;

            Node cLatestAction = null;
            foreach (MachineActionInfo info in machineTrace)
            {
                if (info.Type != MachineActionType.SendAction && (info.ActionId != 0) && !(info.Type == MachineActionType.TaskMachineCreation))
                {
                    ThreadTrace matching = null;

                    try
                    {
                        matching = this.AllThreadTraces.Where(item => item.MachineId == (int)info.MachineId &&
                            item.ActionId == info.ActionId).Single();
                    }
                    catch (Exception)
                    {
                        //TODO: check correctness
                        //In case entry and exit functions not defined.   
                        //Output.WriteLine("Skipping entry/exit actions: " + mt.MachineId + " " + mt.ActionId + " " + mt.ActionName);          
                        continue;
                    }

                    Node cn = new CActBegin(matching.MachineId, matching.ActionName,
                        matching.ActionId, info.EventName, info.EventId);
                    if (matching.ActionId == 1)
                    {
                        ((CActBegin)cn).IsStart = true;
                    }

                    cn.VectorClock = new int[this.VcCount];
                    currentMachineVC++;

                    try
                    {
                        cn.VectorClock[info.MachineId] = currentMachineVC;
                    }
                    catch (Exception ex)
                    {
                        Output.WriteLine("failed: " + this.VcCount + " " + info.MachineId);
                        Output.WriteLine(ex.ToString());
                        Environment.Exit(Environment.ExitCode);
                    }

                    this.CGraph.AddVertex(cn);

                    if (cLatestAction != null)
                    {
                        this.CGraph.AddEdge(new Edge(cLatestAction, cn));
                    }

                    Node cLatest = cn;
                    cLatestAction = cn;


                    bool createNew = false;

                    foreach (ActionInstr ins in matching.Accesses)
                    {
                        // User trace send event.
                        Node cn1;

                        if (createNew)
                        {
                            Node cnn = new CActBegin(matching.MachineId, matching.ActionName,
                                matching.ActionId, info.EventName, info.EventId);
                            cnn.VectorClock = new int[this.VcCount];
                            currentMachineVC++;

                            try
                            {
                                cnn.VectorClock[info.MachineId] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Output.WriteLine("failed: " + this.VcCount + " " + info.MachineId);
                                Output.WriteLine(ex.ToString());
                                Environment.Exit(Environment.ExitCode);
                            }

                            this.CGraph.AddVertex(cnn);

                            cn = cnn;
                            this.CGraph.AddEdge(new Edge(cLatest, cn));
                            createNew = false;
                            cLatest = cn;
                        }

                        if (ins.IsSend)
                        {
                            Output.WriteLine("Searching: " + ins.SendId);
                            foreach(var m in machineTrace)
                            {
                                if(!string.IsNullOrEmpty(m.SendEventName) && (int)m.MachineId == matching.MachineId)
                                {
                                    Output.WriteLine("Available: " + m.SendId);
                                }
                            }
                            MachineActionInfo machineSend = machineTrace.Where(
                                item => (int)item.MachineId == matching.MachineId &&
                                item.SendId == ins.SendId).Single();

                            cn1 = new SendEvent((int)machineSend.MachineId, machineSend.SendId,
                                (int)machineSend.TargetMachineId, machineSend.SendEventName, machineSend.EventId);
                            cn1.VectorClock = new int[this.VcCount];
                            currentMachineVC++;

                            try
                            {
                                cn1.VectorClock[info.MachineId] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Output.WriteLine("failed: " + this.VcCount + " " + info.MachineId);
                                Output.WriteLine(ex.ToString());
                                Environment.Exit(Environment.ExitCode);
                            }

                            this.CGraph.AddVertex(cn1);
                            this.CGraph.AddEdge(new Edge(cLatest, cn1));

                            createNew = true;
                        }
                        // User trace create machine.
                        else if (ins.IsCreate)
                        {
                            cn1 = new CreateMachine(ins.CreateMachineId);
                            cn1.VectorClock = new int[this.VcCount];
                            currentMachineVC++;

                            try
                            {
                                cn1.VectorClock[info.MachineId] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Output.WriteLine("failed: " + this.VcCount + " " + info.MachineId);
                                Output.WriteLine(ex.ToString());
                                Environment.Exit(Environment.ExitCode);
                            }

                            this.CGraph.AddVertex(cn1);
                            this.CGraph.AddEdge(new Edge(cLatest, cn1));

                            createNew = true;
                        }
                        // User trace task creation.
                        else if (ins.IsTask)
                        {
                            cn1 = new CreateTask(ins.TaskId);
                            cn1.VectorClock = new int[this.VcCount];
                            currentMachineVC++;

                            try
                            {
                                cn1.VectorClock[info.MachineId] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Output.WriteLine("failed: " + this.VcCount + " " + info.MachineId);
                                Output.WriteLine(ex.ToString());
                                Environment.Exit(Environment.ExitCode);
                            }

                            this.CGraph.AddVertex(cn1);
                            this.CGraph.AddEdge(new Edge(cLatest, cn1));


                            createNew = true;
                        }
                        // User trace reads/writes.
                        else
                        {
                            ((CActBegin)cn).Addresses.Add(new MemAccess(ins.IsWrite, ins.Location,
                                ins.ObjHandle, ins.Offset, ins.SrcLocation, (int)info.MachineId));
                            cn1 = cn;

                        }

                        cLatest = cn1;
                        cLatestAction = cn1;
                    }
                }
            }
        }

        /// <summary>
        /// Prunes the graph.
        /// </summary>
        void PruneGraph()
        {
            List<Node> remove = new List<Node>();
            foreach (Node u in this.CGraph.Vertices)
            {
                if (u.GetType().ToString().Contains("CActBegin"))
                {
                    CActBegin m = (CActBegin)u;
                    if (m.Addresses.Count == 0)
                    {
                        remove.Add(u);
                        continue;
                    }

                    bool found = false;
                    foreach (Node v in this.CGraph.Vertices)
                    {
                        if (v.GetType().ToString().Contains("CActBegin"))
                        {
                            CActBegin n = (CActBegin)v;

                            if (m.Equals(n) || m.MachineId == n.MachineId)
                            {
                                continue;
                            }

                            foreach (MemAccess ma in m.Addresses)
                            {
                                var list = n.Addresses.Where(item
                                    => item.ObjHandle == ma.ObjHandle &&
                                    item.Offset == ma.Offset);
                                if (list.Count() > 0)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (found == true)
                        {
                            break;
                        }
                    }

                    if (found == false)
                    {
                        remove.Add(u);
                    }
                }
            }

            foreach (Node u in remove)
            {
                if (this.CGraph.InDegree(u) == 0 || this.CGraph.OutDegree(u) == 0)
                {
                    this.CGraph.RemoveVertex(u);
                }
                else
                {
                    IEnumerable<Edge> fromEdges = this.CGraph.InEdges(u);
                    IEnumerable<Edge> toEdges = this.CGraph.OutEdges(u);
                    foreach (Edge fromEdge in fromEdges)
                    {
                        Node from = fromEdge.Source;
                        foreach (Edge toEdge in toEdges)
                        {
                            Node to = toEdge.Target;
                            this.CGraph.AddEdge(new Edge(from, to));
                        }

                        this.CGraph.RemoveVertex(u);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the graph cross edges.
        /// </summary>
        void UpdateGraphCrossEdges()
        {
            foreach (Node n in this.CGraph.Vertices)
            {
                if (n.GetType().ToString().Contains("SendEvent"))
                {
                    IEnumerable<Node> actBegins = this.CGraph.Vertices.Where(item
                        => item.GetType().ToString().Contains("CActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        SendEvent sendNode = (SendEvent)n;
                        CActBegin beginNode = (CActBegin)n1;
                        if (sendNode.ToMachine == beginNode.MachineId &&
                            sendNode.SendEventId == beginNode.EventId)
                        {
                            this.CGraph.AddEdge(new Edge(sendNode, beginNode));
                            for (int i = 0; i < this.VcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i],
                                    sendNode.VectorClock[i]);
                            }
                        }
                    }
                }
                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    IEnumerable<Node> actBegins = this.CGraph.Vertices.Where(item
                        => item.GetType().ToString().Contains("CActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        CreateMachine createNode = (CreateMachine)n;
                        CActBegin beginNode = (CActBegin)n1;
                        if (createNode.CreateMachineId == beginNode.MachineId && beginNode.ActionId == 1)
                        {
                            this.CGraph.AddEdge(new Edge(createNode, beginNode));
                            for (int i = 0; i < this.VcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i],
                                    createNode.VectorClock[i]);
                            }
                        }
                    }
                }
                else if (n.GetType().ToString().Contains("CreateTask"))
                {
                    IEnumerable<Node> actBegins = this.CGraph.Vertices.Where(item
                        => item.GetType().ToString().Contains("CActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        CreateTask createNode = (CreateTask)n;
                        CActBegin beginNode = (CActBegin)n1;
                        if (createNode.TaskId == beginNode.TaskId)
                        {
                            this.CGraph.AddEdge(new Edge(createNode, beginNode));
                            for (int i = 0; i < this.VcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i],
                                    createNode.VectorClock[i]);
                            }
                        }
                    }
                }
            }
        }

        void UpdateVectorsT()
        {
            if (this.CGraph.VertexCount == 0)
            {
                return;
            }

            BidirectionalGraph<Node, Edge> topoGraph = this.CGraph.Clone();

            while (topoGraph.VertexCount > 0)
            {
                Node current = topoGraph.Vertices.Where(item => topoGraph.InDegree(item) == 0).First();

                IEnumerable<Edge> outEdges = topoGraph.Edges.Where(item => item.Source.Equals(current));
                foreach (Edge outEdge in outEdges)
                {
                    Node succ = outEdge.Target;
                    Node successor = this.CGraph.Vertices.Where(v => v.Equals(succ)).Single();
                    for (int i = 0; i < this.VcCount; i++)
                    {
                        //succ.VectorClock[i] = Math.Max(succ.VectorClock[i], current.VectorClock[i]);
                        successor.VectorClock[i] = Math.Max(successor.VectorClock[i], current.VectorClock[i]);
                    }
                }
                topoGraph.RemoveVertex(current);
            }
        }

        /// <summary>
        /// Prints the graph.
        /// </summary>
        void PrintGraph(BidirectionalGraph<Node, Edge> graph)
        {
            Output.WriteLine("Printing compressed graph");
            foreach (Node n in graph.Vertices)
            {
                if (n.GetType().ToString().Contains("CActBegin"))
                {
                    Output.WriteLine(n.GetHashCode() + " " + n.ToString() + " " +
                        ((CActBegin)n).MachineId + " " + ((CActBegin)n).ActionId +
                        " " + ((CActBegin)n).ActionName + " " + ((CActBegin)n).IsTask + " " + ((CActBegin)n).TaskId + " " + ((CActBegin)n).EventId
                        + " " + ((CActBegin)n).EventName);
                    Output.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                    foreach (MemAccess m in ((CActBegin)n).Addresses)
                    {
                        Output.WriteLine(m.IsWrite + " " + m.Location + " " + m.ObjHandle + " " + m.Offset + " " + m.SrcLocation);
                    }
                }
                else if (n.GetType().ToString().Contains("SendEvent"))
                {
                    Output.WriteLine(n.GetHashCode() + " " + n.ToString() + " " +
                        ((SendEvent)n).MachineId + " " + ((SendEvent)n).ToMachine + " " + ((SendEvent)n).SendEventId + " " + ((SendEvent)n).SendEventName);
                    Output.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                }
                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    Output.WriteLine(n.GetHashCode() + " " + n.ToString() + " " +
                        ((CreateMachine)n).CreateMachineId);
                    Output.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                }
                else if (n.GetType().ToString().Contains("CreateTask"))
                {
                    Output.WriteLine(n.GetHashCode() + " " + n.ToString() + " " +
                        ((CreateTask)n).TaskId);
                    Output.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                }
            }

            Output.WriteLine("");

            foreach (Edge e in graph.Edges)
            {
                Output.WriteLine(e.Source.GetHashCode() + " ---> " + e.Target.GetHashCode());
            }

            Output.WriteLine("");
        }

        void DetectRacesAgain()
        {
            Output.WriteLine("\nDETECTING RACES");

            List<Tuple<CActBegin, CActBegin>> checkRaces = new List<Tuple<CActBegin, CActBegin>>();

            foreach (Node n1 in this.CGraph.Vertices)
            {
                if (n1.GetType().ToString().Contains("CActBegin"))
                {
                    CActBegin v1 = (CActBegin)n1;
                    foreach (Node n2 in this.CGraph.Vertices)
                    {
                        if (n2.GetType().ToString().Contains("CActBegin"))
                        {
                            if (n1.Equals(n2))
                            {
                                continue;
                            }

                            CActBegin v2 = (CActBegin)n2;
                            if (v1.MachineId == v2.MachineId)
                            {
                                continue;
                            }

                            bool found = false;
                            foreach (MemAccess ma in v1.Addresses)
                            {
                                var list = v2.Addresses.Where(item
                                    => item.ObjHandle == ma.ObjHandle &&
                                    item.Offset == ma.Offset);
                                if (list.Count() > 0)

                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found == false)
                            {
                                continue;
                            }

                            if (!(cExistsPath(v1, v2) || cExistsPath(v2, v1)))
                            {
                                checkRaces.Add(new Tuple<CActBegin, CActBegin>(v1, v2));
                            }
                        }
                    }
                }
            }

            List<Tuple<string, string>> reportedRaces = new List<Tuple<string, string>>();
            foreach (Tuple<CActBegin, CActBegin> checking in checkRaces)
            {
                List<MemAccess> addressList1 = checking.Item1.Addresses;
                List<MemAccess> addressList2 = checking.Item2.Addresses;

                foreach (MemAccess m in addressList1)
                {
                    foreach (MemAccess n in addressList2)
                    {
                        if (!(m.IsWrite || n.IsWrite))
                        {
                            continue;
                        }

                        if (m.ObjHandle == UIntPtr.Zero && m.Offset == UIntPtr.Zero &&
                            n.ObjHandle == UIntPtr.Zero && n.Offset == UIntPtr.Zero)
                        {
                            continue;
                        }

                        if (reportedRaces.Where(item
                            => item.Item1.Equals(m.SrcLocation + ";" + m.IsWrite) &&
                            item.Item2.Equals(n.SrcLocation + ";" + n.IsWrite)).Any()
                            || reportedRaces.Where(item => item.Item1.Equals(n.SrcLocation + ";" + n.IsWrite) &&
                            item.Item2.Equals(m.SrcLocation + ";" + m.IsWrite)).Any())
                        {
                            continue;
                        }

                        if (m.ObjHandle == n.ObjHandle && m.Offset == n.Offset)
                        {
                            Output.WriteLine("RACE: " + m.SrcLocation + ";" + m.IsWrite + " AND " +
                                n.SrcLocation + ";" + n.IsWrite);
                            reportedRaces.Add(new Tuple<string, string>(m.SrcLocation + ";" +
                                m.IsWrite, n.SrcLocation + ";" + n.IsWrite));
                        }
                    }
                }
            }
        }

        bool DetectRacesFast()
        {
            Output.WriteLine("\nDETECTING RACES");

            if (this.CGraph.VertexCount == 0)
            {
                return false;
            }

            List<Tuple<CActBegin, CActBegin>> checkRaces = new List<Tuple<CActBegin, CActBegin>>();

            foreach (Node n1 in this.CGraph.Vertices)
            {
                if (n1.GetType().ToString().Contains("CActBegin"))
                {
                    CActBegin v1 = (CActBegin)n1;
                    foreach (Node n2 in this.CGraph.Vertices)
                    {
                        if (n2.GetType().ToString().Contains("CActBegin"))
                        {
                            if (n1.Equals(n2))
                            {
                                continue;
                            }

                            CActBegin v2 = (CActBegin)n2;

                            if (v1.MachineId == v2.MachineId)
                            {
                                continue;
                            }

                            bool ordered = true;
                            for (int i = 0; i < this.VcCount; i++)
                            {
                                if (v1.VectorClock[i] > v2.VectorClock[i])
                                {
                                    ordered = false;
                                    break;
                                }
                            }

                            if (ordered == true)
                            {
                                continue;
                            }

                            bool orderedR = true;
                            for (int i = 0; i < this.VcCount; i++)
                            {
                                if (v2.VectorClock[i] > v1.VectorClock[i])
                                {
                                    orderedR = false;
                                    break;
                                }
                            }

                            if (orderedR == true)
                            {
                                continue;
                            }

                            checkRaces.Add(new Tuple<CActBegin, CActBegin>(v1, v2));
                        }
                    }
                }
            }

            List<Tuple<string, string>> reportedRaces = new List<Tuple<string, string>>();
            foreach (Tuple<CActBegin, CActBegin> checking in checkRaces)
            {
                List<MemAccess> addressList1 = checking.Item1.Addresses;
                List<MemAccess> addressList2 = checking.Item2.Addresses;

                foreach (MemAccess m in addressList1)
                {
                    foreach (MemAccess n in addressList2)
                    {
                        if (!(m.IsWrite || n.IsWrite))
                        {
                            continue;
                        }

                        if (m.ObjHandle == UIntPtr.Zero && m.Offset == UIntPtr.Zero &&
                            n.ObjHandle == UIntPtr.Zero && n.Offset == UIntPtr.Zero)
                        {
                            continue;
                        }

                        if (reportedRaces.Where(item
                            => item.Item1.Equals(m.SrcLocation + ";" + m.IsWrite) &&
                            item.Item2.Equals(n.SrcLocation + ";" + n.IsWrite)).Any()
                            || reportedRaces.Where(item => item.Item1.Equals(n.SrcLocation + ";" + n.IsWrite) &&
                            item.Item2.Equals(m.SrcLocation + ";" + m.IsWrite)).Any())
                        {
                            continue;
                        }

                        if (m.ObjHandle == n.ObjHandle && m.Offset == n.Offset)
                        {
                            Output.WriteLine("RACE: " + m.SrcLocation + ";" + (m.IsWrite ? "write" : "read") + " AND " +
                                n.SrcLocation + ";" + (n.IsWrite ? "write" : "read"));
                            reportedRaces.Add(new Tuple<string, string>(m.SrcLocation + ";" + m.IsWrite,
                                n.SrcLocation + ";" + n.IsWrite));
                            Output.WriteLine("");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool cExistsPath(Node n1, Node n2)
        {
            foreach (Node n in this.CGraph.Vertices)
            {
                n.IsVisited = false;
            }

            Stack<Node> dfsS = new Stack<Node>();
            dfsS.Push(n1);

            while (dfsS.Count > 0)
            {
                Node visiting = dfsS.Pop();
                visiting.IsVisited = true;

                IEnumerable<Edge> outEdges = this.CGraph.Edges.Where(item => item.Source.Equals(visiting));
                foreach (Edge outEdge in outEdges)
                {
                    Node successor = outEdge.Target;
                    if (successor.Equals(n2))
                    {
                        return true;
                    }

                    if (!successor.IsVisited)
                    {
                        dfsS.Push(successor);
                    }
                }
            }

            return false;
        }
    }
}
