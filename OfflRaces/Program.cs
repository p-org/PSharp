using ProgramTrace;
using RuntimeTrace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace OfflRaces
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

    internal class Graph
    {
        public List<Tuple<Node, List<Node>, List<Node>>> adjacencyList = new List<Tuple<Node, List<Node>, List<Node>>>();

        public void addVertex(Node n)
        {
            adjacencyList.Add(new Tuple<Node, List<Node>, List<Node>>(n, new List<Node>(), new List<Node>()));
        }

        public void addEdge(Node from, Node to)
        {
            Tuple<Node, List<Node>, List<Node>> entry = adjacencyList.Where(item => item.Item1.Equals(from)).Single();
            entry.Item2.Add(to);

            Tuple<Node, List<Node>, List<Node>> entry1 = adjacencyList.Where(item => item.Item1.Equals(to)).Single();
            entry1.Item3.Add(from);
        }

        public void Clear()
        {
            adjacencyList.Clear();
        }

        public void removeVertex(Node n)
        {
            Tuple<Node, List<Node>, List<Node>> nodeToRemove = adjacencyList.Where(item => item.Item1.Equals(n)).Single();

            foreach(Node rem in nodeToRemove.Item2)
            {
                Tuple<Node, List<Node>, List<Node>> fromRemove = adjacencyList.Where(item => item.Item1.Equals(rem)).Single();
                fromRemove.Item3.Remove(n);
            }

            adjacencyList.Remove(nodeToRemove);
        }
    }

    public class Program
    {
        //HB graph
        static List<ThreadTrace> allThreadTraces = new List<ThreadTrace>();                     //can be simplified?

        static Graph graph = new Graph();

        static int vcCount = 0;

        //static void Main(String[] args)
        public static void findRaces()
        {
            //Thread.Sleep(5000);
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

                    //chain decomposition
                    vcCount = mFileEntries.Count();
                    //TODO: check this
                    vcCount += 5;

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

                    Console.WriteLine("Graph construction time: " + swatch.Elapsed.TotalSeconds + "s");
                    swatch.Restart();

                    //cPrintGraph();
                    updateVectorsT();

                    Console.WriteLine("Topological sort time: " + swatch.Elapsed.TotalSeconds + "s");
                    swatch.Restart();

                    detectRacesFast();

                    Console.WriteLine("Race detection time: " + swatch.Elapsed.TotalSeconds + "s");
                    swatch.Restart();

                    graph.Clear();
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
                    graph.addVertex(cn);

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
                        //TODO: check this
                        continue;
                    }

                    Node cn = new cActBegin(matching.machineID, matching.actionName, matching.actionID, mt.eventName, mt.eventID);
                    if (matching.actionID == 1)
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
                    graph.addVertex(cn);

                    if (cLatestAction != null)
                    {
                        graph.addEdge(cLatestAction, cn);
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
                            graph.addVertex(cnn);

                            cn = cnn;
                            graph.addEdge(cLatest, cn);
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
                            graph.addVertex(cn1);
                            graph.addEdge(cLatest, cn1);


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
                            graph.addVertex(cn1);
                            graph.addEdge(cLatest, cn1);


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
                            graph.addVertex(cn1);
                            graph.addEdge(cLatest, cn1);


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

        static void updateGraphCrossEdges()
        {
            foreach (Tuple<Node, List<Node>, List<Node>> item in graph.adjacencyList)
            {
                Node n = item.Item1;
                if (n.GetType().ToString().Contains("SendEvent"))
                {
                    IEnumerable<Tuple<Node, List<Node>, List<Node>>> actBegins = graph.adjacencyList.Where(v => v.Item1.GetType().ToString().Contains("cActBegin"));
                    foreach (Tuple<Node, List<Node>, List<Node>> entry in actBegins)
                    {
                        Node n1 = entry.Item1;
                        SendEvent sendNode = (SendEvent)n;
                        cActBegin beginNode = (cActBegin)n1;
                        if (sendNode.toMachine == beginNode.machineID && sendNode.sendEventID == beginNode.eventID)
                        {
                            graph.addEdge(sendNode, beginNode);
                            for (int i = 0; i < vcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i], sendNode.VectorClock[i]);
                            }
                        }
                    }
                }

                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    IEnumerable<Tuple<Node, List<Node>, List<Node>>> actBegins = graph.adjacencyList.Where(v => v.Item1.GetType().ToString().Contains("cActBegin"));
                    foreach (Tuple<Node, List<Node>, List<Node>> entry in actBegins)
                    {
                        Node n1 = entry.Item1;
                        CreateMachine createNode = (CreateMachine)n;
                        cActBegin beginNode = (cActBegin)n1;
                        if (createNode.createMachineId == beginNode.machineID && beginNode.actionID == 1)
                        {
                            graph.addEdge(createNode, beginNode);
                            for (int i = 0; i < vcCount; i++)
                            {
                                beginNode.VectorClock[i] = Math.Max(beginNode.VectorClock[i], createNode.VectorClock[i]);
                            }
                        }
                    }
                }

                else if (n.GetType().ToString().Contains("CreateTask"))
                {
                    IEnumerable<Tuple<Node, List<Node>, List<Node>>> actBegins = graph.adjacencyList.Where(v => v.Item1.GetType().ToString().Contains("cActBegin"));
                    foreach (Tuple<Node, List<Node>, List<Node>> entry in actBegins)
                    {
                        Node n1 = entry.Item1;
                        CreateTask createNode = (CreateTask)n;
                        cActBegin beginNode = (cActBegin)n1;
                        if (createNode.taskId == beginNode.taskId)
                        {
                            graph.addEdge(createNode, beginNode);
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
            Graph topoGraph = new Graph();
            foreach(var entry in graph.adjacencyList)
            {
                topoGraph.adjacencyList.Add(entry);
            }

            while (topoGraph.adjacencyList.Count > 0)
            {
                Tuple<Node, List<Node>, List<Node>> entry = topoGraph.adjacencyList.Where(item => item.Item3.Count == 0).First();
                Node current = entry.Item1;
                foreach (Node succ in entry.Item2)
                {
                    Node successor = (graph.adjacencyList.Where(v => v.Item1.Equals(succ)).Single()).Item1;
                    for (int i = 0; i < vcCount; i++)
                    {
                        //succ.VectorClock[i] = Math.Max(succ.VectorClock[i], current.VectorClock[i]);
                        successor.VectorClock[i] = Math.Max(successor.VectorClock[i], current.VectorClock[i]);
                    }
                }
                topoGraph.removeVertex(current);
            }
        }
        

 /*       static void cPrintGraph()
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
                        Console.WriteLine(m.isWrite + " " + m.location);
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
*/
        static void detectRacesFast()
        {
            Console.WriteLine("\nDETECTING RACES FAST");

            List<Tuple<cActBegin, cActBegin>> checkRaces = new List<Tuple<cActBegin, cActBegin>>();
            //List<Tuple<cActBegin, cActBegin>> pathExists = new List<Tuple<cActBegin, cActBegin>>();

            foreach (Tuple<Node, List<Node>, List<Node>> e1 in graph.adjacencyList)
            {
                Node n1 = e1.Item1;
                if (n1.GetType().ToString().Contains("cActBegin"))
                {
                    cActBegin v1 = (cActBegin)n1;
                    foreach (Tuple<Node, List<Node>, List<Node>> e2 in graph.adjacencyList)
                    {
                        Node n2 = e2.Item1;
                        if (n2.GetType().ToString().Contains("cActBegin"))
                        {
                            if (n1.Equals(n2))
                                continue;

                            cActBegin v2 = (cActBegin)n2;

                            if (v1.machineID == v2.machineID)
                                continue;

                            bool found = false;
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

                            bool ordered = true;
                            for (int i = 0; i < vcCount; i++)
                            {
                                if (v1.VectorClock[i] > v2.VectorClock[i])
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
                            Console.WriteLine("RACE: " + checking.Item1.GetHashCode() + " " + checking.Item2.GetHashCode() + " " + m.srcLocation + ";" + m.isWrite + " AND " + n.srcLocation + ";" + n.isWrite);
                            reportedRaces.Add(new Tuple<string, string>(m.srcLocation + ";" + m.isWrite, n.srcLocation + ";" + n.isWrite));
                        }
                    }
                }
            }
        }
    }
}
