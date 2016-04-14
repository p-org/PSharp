using ProgramTrace;
using QuickGraph;
using RuntimeTrace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OfflineRaces
{
    internal abstract class Node
    {
        public bool visited;
        public int machineID = -1;

        //chain decomposition
        public int[] VectorClock;
    }

    internal class cActBegin : Node
    {
        public String actionName;
        public int actionID = -1;
        public String eventName;
        public int eventID;

        public int taskId = -1;
        public bool isTask;

        public List<MemAccess> addresses = new List<MemAccess>();

        public bool isStart = false;

        public cActBegin(int machineID, String actionName, int actionID, String eventName, int eventID)
        {
            this.machineID = machineID;
            this.actionName = actionName;
            this.actionID = actionID;
            this.eventName = eventName;
            this.eventID = eventID;
            this.isTask = false;
        }

        public cActBegin(int machineId, int taskId)
        {
            this.machineID = machineId;
            this.taskId = taskId;
            this.isTask = true;
        }
    }

    internal class MemAccess : Node
    {
        public bool isWrite;
        public UIntPtr location;
        public UIntPtr objHandle;
        public UIntPtr offset;
        public string srcLocation;
        public int machineId;

        public MemAccess(bool isWrite, UIntPtr location, UIntPtr objHandle, UIntPtr offset, string srcLocation, int machineId)
        {
            this.isWrite = isWrite;
            this.location = location;
            this.objHandle = objHandle;
            this.offset = offset;
            this.srcLocation = srcLocation;
            this.machineId = machineId;
        }
    }

    internal class SendEvent : Node
    {
        public int sendID;
        public int toMachine;
        public String sendEventName;
        public int sendEventID;

        public SendEvent(int machineID, int sendID, int toMachine, String sendEventName, int sendEventID)
        {
            this.machineID = machineID;
            this.sendID = sendID;
            this.toMachine = toMachine;
            this.sendEventName = sendEventName;
            this.sendEventID = sendEventID;
        }
    }

    internal class CreateMachine : Node
    {
        public int createMachineId;

        public CreateMachine(int createMachineId)
        {
            this.createMachineId = createMachineId;
        }
    }

    internal class CreateTask : Node
    {
        public int taskId;

        public CreateTask(int taskId)
        {
            this.taskId = taskId;
        }
    }

    internal class Edge : QuickGraph.IEdge<Node>
    {
        public Node src;
        public Node trg;
        public Edge(Node src, Node trg)
        {
            this.src = src;
            this.trg = trg;
        }
        Node IEdge<Node>.Source
        {
            get
            {
                return src;
            }
        }

        Node IEdge<Node>.Target
        {
            get
            {
                return trg;
            }
        }
    }


    public class Program
    {
        //HB graph
        static List<ThreadTrace> allThreadTraces = new List<ThreadTrace>();                     //can be simplified?

        //static QuickGraph.AdjacencyGraph<Node, Edge> cGraph = new AdjacencyGraph<Node, Edge>();
        static QuickGraph.BidirectionalGraph<Node, Edge> cGraph = new BidirectionalGraph<Node, Edge>();

        static int vcCount = 0;

        //static void Main(String[] args)
        public static void findRaces()
        {
            string[] dirNames = Directory.GetDirectories(".\\");
            foreach (string dirName in dirNames)
            {
                if (dirName.Contains("InstrTrace"))
                {
                    Stopwatch swatch = new Stopwatch();
                    swatch.Start();

                    string[] fileEntries = Directory.GetFiles(dirName, "*thTrace*");
                    foreach (string fileName in fileEntries)
                    {
                        //Deserialize thread traces
                        //Open the file written above and read values from it.
                        Console.WriteLine(fileName);
                        Stream stream = File.Open(fileName, FileMode.Open);
                        BinaryFormatter bformatter = new BinaryFormatter();
                        List<ThreadTrace> tt = (List<ThreadTrace>)bformatter.Deserialize(stream);

                        for (int i = 0; i < tt.Count; i++)
                        {
                            allThreadTraces.Add(tt[i]);
                            //Console.WriteLine(tt[i].machineID + " " + tt[i].actionID);
                        }
                        stream.Close();
                    }

                    string[] mFileEntries = Directory.GetFiles(dirName, "*rtTrace*");

                    foreach (string fileName in mFileEntries)
                    {
                        //chain decomposition
                        int tc = Int32.Parse(fileName.Substring(22, fileName.Length - 4 - 22));
                        if (tc > vcCount)
                            vcCount = tc;
                    }
                    vcCount = vcCount + 1;

                    foreach (string fileName in mFileEntries)
                    {
                        Stream stream = File.Open(fileName, FileMode.Open);
                        BinaryFormatter bformatter = new BinaryFormatter();
                        List<MachineTrace> machineTrace = ((List<MachineTrace>)bformatter.Deserialize(stream));
                        stream.Close();

                        updateTasks(machineTrace);
                        updateGraph(machineTrace);
                    }

                    updateGraphCrossEdges();
                    Console.WriteLine("before pruning: " + cGraph.VertexCount);
                    pruneGraph();
                    Console.WriteLine("after pruning: " + cGraph.VertexCount);
                    //cPrintGraph();
                    Console.WriteLine("Graph construction time: " + swatch.Elapsed.TotalSeconds + "s");
                    swatch.Restart();

                    updateVectorsT();
                    Console.WriteLine("Topological sort time: " + swatch.Elapsed.TotalSeconds + "s");
                    swatch.Restart();
                    

                    /*Console.WriteLine("Number of nodes = " + cGraph.VertexCount);
                    int accesses = 0;
                    foreach(Node n in cGraph.Vertices)
                    {
                        if (n.GetType().ToString().Contains("cActBegin"))
                        {
                            accesses += ((cActBegin)n).addresses.Count;
                        }
                    }
                    Console.WriteLine("Number of accesses: " + accesses);
                    */

                    //detectRacesAgain();

                    detectRacesFast();

                    Console.WriteLine("Race detection time: " + swatch.Elapsed.TotalSeconds + "s");
                    swatch.Restart();

                    cGraph.Clear();
                    allThreadTraces.Clear();
                    Console.WriteLine("---------------------------------------------");
                }
            }
            /*Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            */
        }

        static void updateTasks(List<MachineTrace> machineTrace)
        {
            int currentMachineVC = 0;

            foreach (MachineTrace mt in machineTrace)
            {
                if (mt.isTaskMachine)
                {
                    //ThreadTrace matching = null;
                    var matching = allThreadTraces.Where(item => item.isTask && item.taskId == mt.taskId);

                    if (matching.Count() == 0)
                        continue;

                    Node cn = new cActBegin(mt.machineID, mt.taskId);
                    ((cActBegin)cn).isStart = true;
                    cn.VectorClock = new int[vcCount];
                    currentMachineVC++;
                    try
                    {
                        cn.VectorClock[mt.machineID] = currentMachineVC;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("failed: " + vcCount + " " + mt.machineID);
                        Console.WriteLine(ex);
                        Environment.Exit(Environment.ExitCode);
                    }
                    cGraph.AddVertex(cn);

                    foreach (var m in matching)
                    {
                        if (m.accesses.Count > 0)
                        {   
                            foreach (ActionInstr ins in m.accesses)
                            {
                                ((cActBegin)cn).addresses.Add(new MemAccess(ins.isWrite, ins.location, ins.objHandle, ins.offset, ins.srcLocation, mt.machineID));
                            }
                        }
                    }
                }
            }
        }

        static void updateGraph(List<MachineTrace> machineTrace)
        {
            int currentMachineVC = 0;

            Node cLatestAction = null;
            foreach (MachineTrace mt in machineTrace)
            {
                if (!mt.isSend && (mt.actionID != 0) && !mt.isTaskMachine)
                {
                    ThreadTrace matching = null;

                    try
                    {
                        matching = allThreadTraces.Where(item => item.machineID == mt.machineID && item.actionID == mt.actionID).Single();
                    }
                    catch (Exception)
                    {
                        //In case entry and exit functions not defined.   
                        Console.WriteLine("Skipping entry/exit actions: " + mt.machineID + " " + mt.actionID + " " + mt.actionName);          
                        continue;
                    }

                    Node cn = new cActBegin(matching.machineID, matching.actionName, matching.actionID, mt.eventName, mt.eventID);
                    if(matching.actionID == 1)
                    {
                        ((cActBegin)cn).isStart = true;
                    }
                    cn.VectorClock = new int[vcCount];
                    currentMachineVC++;
                    try
                    {
                        cn.VectorClock[mt.machineID] = currentMachineVC;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("failed: " + vcCount + " " + mt.machineID);
                        Console.WriteLine(ex);
                        Environment.Exit(Environment.ExitCode);
                    }
                    cGraph.AddVertex(cn);
                    
                    if(cLatestAction != null)
                    {
                        cGraph.AddEdge(new Edge(cLatestAction, cn));
                    }

                    Node cLatest = cn;
                    cLatestAction = cn;
                    

                    bool createNew = false;

                    foreach (ActionInstr ins in matching.accesses)
                    {
                        //user trace send event
                        Node cn1;

                        if (createNew)
                        {
                            Node cnn = new cActBegin(matching.machineID, matching.actionName, matching.actionID, mt.eventName, mt.eventID);
                            cnn.VectorClock = new int[vcCount];
                            currentMachineVC++;
                            try
                            {
                                cnn.VectorClock[mt.machineID] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("failed: " + vcCount + " " + mt.machineID);
                                Console.WriteLine(ex);
                                Environment.Exit(Environment.ExitCode);
                            }
                            cGraph.AddVertex(cnn);

                            cn = cnn;
                            cGraph.AddEdge(new Edge(cLatest, cn));
                            createNew = false;
                            cLatest = cn;
                        }

                        if (ins.isSend)
                        {
                            MachineTrace machineSend = machineTrace.Where(item => item.machineID == matching.machineID && item.sendID == ins.sendID).Single();
   
                            cn1 = new SendEvent(machineSend.machineID, machineSend.sendID, machineSend.toMachine, machineSend.sendEventName, machineSend.sendEventID);
                            cn1.VectorClock = new int[vcCount];
                            currentMachineVC++;
                            try
                            {
                                cn1.VectorClock[mt.machineID] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("failed: " + vcCount + " " + mt.machineID);
                                Console.WriteLine(ex);
                                Environment.Exit(Environment.ExitCode);
                            }
                            cGraph.AddVertex(cn1);
                            cGraph.AddEdge(new Edge(cLatest, cn1));
                            

                            createNew = true;
                        }

                        //user trace create machine 
                        else if (ins.isCreate)
                        {
                            cn1 = new CreateMachine(ins.createMachineID);
                            cn1.VectorClock = new int[vcCount];
                            currentMachineVC++;
                            try
                            {
                                cn1.VectorClock[mt.machineID] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("failed: " + vcCount + " " + mt.machineID);
                                Console.WriteLine(ex);
                                Environment.Exit(Environment.ExitCode);
                            }
                            cGraph.AddVertex(cn1);
                            cGraph.AddEdge(new Edge(cLatest, cn1));
                            

                            createNew = true;
                        }

                        //user trace task creation
                        else if (ins.isTask)
                        {
                            cn1 = new CreateTask(ins.taskId);
                            cn1.VectorClock = new int[vcCount];
                            currentMachineVC++;
                            try
                            {
                                cn1.VectorClock[mt.machineID] = currentMachineVC;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("failed: " + vcCount + " " + mt.machineID);
                                Console.WriteLine(ex);
                                Environment.Exit(Environment.ExitCode);
                            }
                            cGraph.AddVertex(cn1);
                            cGraph.AddEdge(new Edge(cLatest, cn1));
                            

                            createNew = true;
                        }

                        //user trace reads/writes
                        else
                        {
                            ((cActBegin)cn).addresses.Add(new MemAccess(ins.isWrite, ins.location, ins.objHandle, ins.offset, ins.srcLocation, mt.machineID));
                            cn1 = cn;
           
                        }

                        cLatest = cn1;
                        cLatestAction = cn1;
                        
                    }
                }
            }
        }

        static void pruneGraph()
        {
            List<Node> remove = new List<Node>();
            foreach(Node u in cGraph.Vertices)
            {
                if (u.GetType().ToString().Contains("cActBegin"))
                {
                    cActBegin m = (cActBegin)u;
                    if (m.addresses.Count == 0)
                    {
                        remove.Add(u);
                        continue;
                    }

                    bool found = false;
                    foreach (Node v in cGraph.Vertices)
                    {
                        if (v.GetType().ToString().Contains("cActBegin"))
                        {
                            cActBegin n = (cActBegin)v;

                            if (m.Equals(n) || m.machineID == n.machineID)
                                continue;

                            foreach (MemAccess ma in m.addresses)
                            {
                                var list = n.addresses.Where(item => item.objHandle == ma.objHandle && item.offset == ma.offset);
                                if (list.Count() > 0)
                                {
                                    found = true;
                                    break;
                                }
                            }                        
                        }
                        if (found == true)
                            break;
                    }

                    if (found == false)
                        remove.Add(u);
                }
            }

            foreach (Node u in remove)
            {
                if (cGraph.InDegree(u) == 0 || cGraph.OutDegree(u) == 0)
                    cGraph.RemoveVertex(u);
                else
                {
                    IEnumerable<Edge> fromEdges = cGraph.InEdges(u);
                    IEnumerable<Edge> toEdges = cGraph.OutEdges(u);
                    foreach (Edge fromEdge in fromEdges)
                    {
                        Node from = fromEdge.src;
                        foreach (Edge toEdge in toEdges)
                        {
                            Node to = toEdge.trg;
                            cGraph.AddEdge(new Edge(from, to));
                            cGraph.RemoveVertex(u);
                        }
                    }
                }
            }
        }

        static void updateGraphCrossEdges()
        {
            foreach (Node n in cGraph.Vertices)
            {
                if (n.GetType().ToString().Contains("SendEvent"))
                {
                    IEnumerable<Node> actBegins = cGraph.Vertices.Where(item => item.GetType().ToString().Contains("cActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        SendEvent sendNode = (SendEvent)n;
                        cActBegin beginNode = (cActBegin)n1;
                        if (sendNode.toMachine == beginNode.machineID && sendNode.sendEventID == beginNode.eventID)
                        {
                            cGraph.AddEdge(new Edge(sendNode, beginNode));
                            for(int i = 0; i < vcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i], sendNode.VectorClock[i]);
                            }
                        }
                    }
                }

                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    IEnumerable<Node> actBegins = cGraph.Vertices.Where(item => item.GetType().ToString().Contains("cActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        CreateMachine createNode = (CreateMachine)n;
                        cActBegin beginNode = (cActBegin)n1;
                        if (createNode.createMachineId == beginNode.machineID && beginNode.actionID == 1)
                        {
                            cGraph.AddEdge(new Edge(createNode, beginNode));
                            for (int i = 0; i < vcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i], createNode.VectorClock[i]);
                            }
                        }    
                    }
                }

                else if (n.GetType().ToString().Contains("CreateTask"))
                {
                    IEnumerable<Node> actBegins = cGraph.Vertices.Where(item => item.GetType().ToString().Contains("cActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        CreateTask createNode = (CreateTask)n;
                        cActBegin beginNode = (cActBegin)n1;
                        if (createNode.taskId == beginNode.taskId)
                        {
                            cGraph.AddEdge(new Edge(createNode, beginNode));
                            for (int i = 0; i < vcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i], createNode.VectorClock[i]);
                            }
                        }
                    }
                }
            }
        }

        static void updateVectorsT()
        {
            BidirectionalGraph<Node, Edge> topoGraph = cGraph.Clone(); 

            while (topoGraph.VertexCount > 0)
            {
                Node current = topoGraph.Vertices.Where(item => topoGraph.InDegree(item) == 0).First();
                IEnumerable<Edge> outEdges = topoGraph.Edges.Where(item => item.src.Equals(current));
                foreach (Edge outEdge in outEdges)
                {
                    Node succ = outEdge.trg;
                    Node successor = cGraph.Vertices.Where(v => v.Equals(succ)).Single();
                    for (int i = 0; i < vcCount; i++)
                    {
                        //succ.VectorClock[i] = Math.Max(succ.VectorClock[i], current.VectorClock[i]);
                        successor.VectorClock[i] = Math.Max(successor.VectorClock[i], current.VectorClock[i]);
                    }
                }
                topoGraph.RemoveVertex(current);
            }
        }

        static void cPrintGraph()
        {
            Console.WriteLine("Printing compressed graph");
            foreach (Node n in cGraph.Vertices)
            {
                if (n.GetType().ToString().Contains("cActBegin"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((cActBegin)n).machineID + " " + ((cActBegin)n).actionID + " " + ((cActBegin)n).actionName + " " + ((cActBegin)n).isTask);
                    Console.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                    foreach (MemAccess m in ((cActBegin)n).addresses)
                    {
                        Console.WriteLine(m.isWrite + " " + m.location + " " + m.objHandle + " " + m.offset);
                    }
                }
                else if (n.GetType().ToString().Contains("SendEvent"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((SendEvent)n).machineID + " " + ((SendEvent)n).toMachine);
                    Console.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                }
                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((CreateMachine)n).createMachineId);
                    Console.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                }
                else if (n.GetType().ToString().Contains("CreateTask"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((CreateTask)n).taskId);
                    Console.WriteLine("[{0}]", string.Join(", ", n.VectorClock));
                }
            }

            Console.WriteLine();

            foreach (Edge e in cGraph.Edges)
            {
                Console.WriteLine(e.src.GetHashCode() + " ---> " + e.trg.GetHashCode());
            }
            Console.WriteLine();
        }

        static void detectRacesAgain()
        {
            Console.WriteLine("\nDETECTING RACES");

            List<Tuple<cActBegin, cActBegin>> checkRaces = new List<Tuple<cActBegin, cActBegin>>();
            //List<Tuple<cActBegin, cActBegin>> pathExists = new List<Tuple<cActBegin, cActBegin>>();

            foreach (Node n1 in cGraph.Vertices)
            {
                if (n1.GetType().ToString().Contains("cActBegin"))
                {
                    cActBegin v1 = (cActBegin)n1;
                    foreach (Node n2 in cGraph.Vertices)
                    {
                        if (n2.GetType().ToString().Contains("cActBegin"))
                        {
                            if (n1.Equals(n2))
                                continue;

                            cActBegin v2 = (cActBegin)n2;

                            if (v1.machineID == v2.machineID)
                                continue;

                            bool found = false;
                            foreach(MemAccess ma in v1.addresses)
                            {
                                var list = v2.addresses.Where(item => item.objHandle == ma.objHandle && item.offset == ma.offset);
                                if(list.Count() > 0)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found == false)
                                continue;

                            if (!(cExistsPath(v1, v2) || cExistsPath(v2, v1)))
                            {
                                checkRaces.Add(new Tuple<cActBegin, cActBegin>(v1, v2));
                            }
                        }
                    }
                }
            }

            List<Tuple<string, string>> reportedRaces = new List<Tuple<string, string>>();
            foreach (Tuple<cActBegin, cActBegin> checking in checkRaces)
            {
                List<MemAccess> addressList1 = checking.Item1.addresses;
                List<MemAccess> addressList2 = checking.Item2.addresses;

                foreach (MemAccess m in addressList1)
                {
                    foreach(MemAccess n in addressList2)
                    {
                        if (!(m.isWrite || n.isWrite))
                           continue;
                        
                        if (m.objHandle == UIntPtr.Zero && m.offset == UIntPtr.Zero && n.objHandle == UIntPtr.Zero && n.offset == UIntPtr.Zero)
                            continue;

                        if (reportedRaces.Where(item => item.Item1.Equals(m.srcLocation + ";" + m.isWrite) && item.Item2.Equals(n.srcLocation + ";" + n.isWrite)).Any()
                                        || reportedRaces.Where(item => item.Item1.Equals(n.srcLocation + ";" + n.isWrite) && item.Item2.Equals(m.srcLocation + ";" + m.isWrite)).Any())
                            continue;

                        if (m.objHandle == n.objHandle && m.offset == n.offset)
                        {
                            Console.WriteLine("RACE: " + m.srcLocation + ";" + m.isWrite + " AND " + n.srcLocation + ";" + n.isWrite);
                            reportedRaces.Add(new Tuple<string, string>(m.srcLocation + ";" + m.isWrite, n.srcLocation + ";" + n.isWrite));
                        }
                    }
                }
            }
        }

        static void detectRacesFast()
        {
            Console.WriteLine("\nDETECTING RACES FAST");

            List<Tuple<cActBegin, cActBegin>> checkRaces = new List<Tuple<cActBegin, cActBegin>>();
            //List<Tuple<cActBegin, cActBegin>> pathExists = new List<Tuple<cActBegin, cActBegin>>();

            foreach (Node n1 in cGraph.Vertices)
            {
                if (n1.GetType().ToString().Contains("cActBegin"))
                {
                    cActBegin v1 = (cActBegin)n1;
                    foreach (Node n2 in cGraph.Vertices)
                    {
                        if (n2.GetType().ToString().Contains("cActBegin"))
                        {
                            if (n1.Equals(n2))
                                continue;

                            cActBegin v2 = (cActBegin)n2;

                            if (v1.machineID == v2.machineID)
                                continue;

                            /*bool found = false;
                            foreach (MemAccess ma in v1.addresses)
                            {
                                var list = v2.addresses.Where(item => item.objHandle == ma.objHandle && item.offset == ma.offset);
                                if (list.Count() > 0)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found == false)
                                continue;
                            */

                            bool ordered = true;
                            for(int i = 0; i < vcCount; i++)
                            {
                                if(v1.VectorClock[i] > v2.VectorClock[i])
                                {
                                    ordered = false;
                                    break;
                                }
                            }
                            if (ordered == true)
                                continue;

                            bool orderedR = true;
                            for (int i = 0; i < vcCount; i++)
                            {
                                if (v2.VectorClock[i] > v1.VectorClock[i])
                                {
                                    orderedR = false;
                                    break;
                                }
                            }
                            if (orderedR == true)
                                continue;

                            checkRaces.Add(new Tuple<cActBegin, cActBegin>(v1, v2));
                        }
                    }
                }
            }

            List<Tuple<string, string>> reportedRaces = new List<Tuple<string, string>>();
            foreach (Tuple<cActBegin, cActBegin> checking in checkRaces)
            {
                List<MemAccess> addressList1 = checking.Item1.addresses;
                List<MemAccess> addressList2 = checking.Item2.addresses;

                foreach (MemAccess m in addressList1)
                {
                    foreach (MemAccess n in addressList2)
                    {
                        if (!(m.isWrite || n.isWrite))
                            continue;

                        if (m.objHandle == UIntPtr.Zero && m.offset == UIntPtr.Zero && n.objHandle == UIntPtr.Zero && n.offset == UIntPtr.Zero)
                            continue;

                        if (reportedRaces.Where(item => item.Item1.Equals(m.srcLocation + ";" + m.isWrite) && item.Item2.Equals(n.srcLocation + ";" + n.isWrite)).Any()
                                        || reportedRaces.Where(item => item.Item1.Equals(n.srcLocation + ";" + n.isWrite) && item.Item2.Equals(m.srcLocation + ";" + m.isWrite)).Any())
                            continue;

                        if (m.objHandle == n.objHandle && m.offset == n.offset)
                        {
                            Console.WriteLine("RACE: " + m.srcLocation + ";" + m.isWrite + " AND " + n.srcLocation + ";" + n.isWrite);
                            reportedRaces.Add(new Tuple<string, string>(m.srcLocation + ";" + m.isWrite, n.srcLocation + ";" + n.isWrite));
                        }
                    }
                }
            }
        }

        static bool cExistsPath(Node n1, Node n2)
        {
            foreach (Node n in cGraph.Vertices)
            {
                n.visited = false;
            }

            Stack<Node> dfsS = new Stack<Node>();
            dfsS.Push(n1);

            while (dfsS.Count > 0)
            {
                Node visiting = dfsS.Pop();
                visiting.visited = true;

                IEnumerable<Edge> outEdges = cGraph.Edges.Where(item => item.src.Equals(visiting));
                foreach (Edge outEdge in outEdges)
                {
                    Node successor = outEdge.trg;
                    if (successor.Equals(n2))
                    {
                        return true;
                    }
                    if (!successor.visited)
                        dfsS.Push(successor);
                }
            }

            return false;
        }
    }
}
