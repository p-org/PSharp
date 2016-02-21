//-----------------------------------------------------------------------
// <copyright file="Offline.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using QuickGraph;

using Microsoft.PSharp.Instrumentation;

namespace Microsoft.PSharp.DynamicRaceDetection.Offline
{
    internal abstract class Node
    {
        public bool visited;
    }

    internal class ActBegin : Node
    {
        public int machineID;
        public String actionName;
        public int actionID;
        public String eventName;
        public int eventID;

        public ActBegin(int machineID, String actionName, int actionID, String eventName, int eventID)
        {
            this.machineID = machineID;
            this.actionName = actionName;
            this.actionID = actionID;
            this.eventName = eventName;
            this.eventID = eventID;
        }
    }

    //May not be required!!!!!!!
    internal class ActEnd : Node
    {
        public int machineID;
        public int actionID;

        public ActEnd(int machineID, int actionID)
        {
            this.machineID = machineID;
            this.actionID = actionID;
        }
    }

    internal class MemAccess : Node
    {
        public bool isWrite;
        public UIntPtr location;
        public UIntPtr objHandle;
        public UIntPtr offset;
        public string srcLocation;

        public MemAccess(bool isWrite, UIntPtr location, UIntPtr objHandle, UIntPtr offset, string srcLocation)
        {
            this.isWrite = isWrite;
            this.location = location;
            this.objHandle = objHandle;
            this.offset = offset;
            this.srcLocation = srcLocation;
        }
    }

    internal class SendEvent : Node
    {
        public int machineID;
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


    class Program
    {
        //HB graph
        static QuickGraph.AdjacencyGraph<Node, Edge> HbGraph = new AdjacencyGraph<Node, Edge>();
        static List<ThreadTrace> allThreadTraces = new List<ThreadTrace>();                     //can be simplified?
        static void Main(String[] args)
        {
            string[] fileEntries = Directory.GetFiles("C:\\Users\\Pantazis\\workspace\\PSharp\\RaceDetection\\RaceDetection\\bin\\Debug");
            foreach (string fileName in fileEntries)
            {
                //Deserialize thread traces
                if (fileName.Contains("thTrace"))
                {
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
            }
            foreach (string fileName in fileEntries)
            {
                if (fileName.Contains("rtTrace"))
                {
                    Stream stream = File.Open(fileName, FileMode.Open);
                    BinaryFormatter bformatter = new BinaryFormatter();
                    List<MachineTrace> machineTrace = ((List<MachineTrace>)bformatter.Deserialize(stream));
                    stream.Close();

                    updateGraph(machineTrace);
                    updateGraphCrossEdges();
                }
            }

            printGraph();
            Console.WriteLine("Detecting races");
            detectRaces();

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        static void updateGraph(List<MachineTrace> machineTrace)
        {
            Node latestAction = null;
            foreach (MachineTrace mt in machineTrace)
            {
                if (!mt.isSend && (mt.actionID != 0))
                {
                    ThreadTrace matching = null;

                    matching = allThreadTraces.Where(item => item.machineID == mt.machineID && item.actionID == mt.actionID).Single();

                    Node nd = new ActBegin(matching.machineID, matching.actionName, matching.actionID, mt.eventName, mt.eventID);
                    HbGraph.AddVertex(nd);

                    if (latestAction != null)
                    {
                        HbGraph.AddEdge(new Edge(latestAction, nd));
                    }

                    Node latest = nd;
                    latestAction = nd;
                    foreach (ActionInstr ins in matching.accesses)
                    {
                        //user trace send event
                        Node nd1;
                        if (ins.isSend)
                        {
                            MachineTrace machineSend = machineTrace.Where(item => item.machineID == matching.machineID && item.sendID == ins.sendID).Single();
                            nd1 = new SendEvent(machineSend.machineID, machineSend.sendID, machineSend.toMachine, machineSend.sendEventName, machineSend.sendEventID);
                        }

                        //user trace create machine
                        else if (ins.isCreate)
                        {
                            nd1 = new CreateMachine(ins.createMachineID);
                        }

                        //user trace reads/writes
                        else
                        {
                            nd1 = new MemAccess(ins.isWrite, ins.location, ins.objHandle, ins.offset, ins.srcLocation);
                        }
                        HbGraph.AddVertex(nd1);
                        HbGraph.AddEdge(new Edge(latest, nd1));
                        latest = nd1;
                        latestAction = nd1;
                    }
                }
            }
        }

        static void updateGraphCrossEdges()
        {
            foreach (Node n in HbGraph.Vertices)
            {
                if (n.GetType().ToString().Contains("SendEvent"))
                {
                    IEnumerable<Node> actBegins = HbGraph.Vertices.Where(item => item.GetType().ToString().Contains("ActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        SendEvent sendNode = (SendEvent)n;
                        ActBegin beginNode = (ActBegin)n1;
                        if (sendNode.toMachine == beginNode.machineID && sendNode.sendEventID == beginNode.eventID)
                            HbGraph.AddEdge(new Edge(sendNode, beginNode));
                    }
                }

                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    IEnumerable<Node> actBegins = HbGraph.Vertices.Where(item => item.GetType().ToString().Contains("ActBegin"));
                    foreach (Node n1 in actBegins)
                    {
                        CreateMachine createNode = (CreateMachine)n;
                        ActBegin beginNode = (ActBegin)n1;
                        if (createNode.createMachineId == beginNode.machineID && beginNode.actionID == 1)
                            HbGraph.AddEdge(new Edge(createNode, beginNode));
                    }
                }
            }
        }

        static void printGraph()
        {
            Console.WriteLine("Printing graph");
            foreach (Node n in HbGraph.Vertices)
            {
                if (n.GetType().ToString().Contains("ActBegin"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((ActBegin)n).machineID + " " + ((ActBegin)n).actionID + " " + ((ActBegin)n).actionName);
                }
                else if (n.GetType().ToString().Contains("MemAccess"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((MemAccess)n).isWrite + " " + ((MemAccess)n).location);
                }
                else if (n.GetType().ToString().Contains("SendEvent"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((SendEvent)n).machineID + " " + ((SendEvent)n).toMachine);
                }
                else if (n.GetType().ToString().Contains("CreateMachine"))
                {
                    Console.WriteLine(n.GetHashCode() + " " + n.ToString() + " " + ((CreateMachine)n).createMachineId);
                }
            }

            Console.WriteLine();

            foreach (Edge e in HbGraph.Edges)
            {
                Console.WriteLine(e.src.GetHashCode() + " ---> " + e.trg.GetHashCode());
            }
            Console.WriteLine();
        }

        static void detectRaces()
        {
            List<Tuple<Node, Node>> racyPairs = new List<Tuple<Node, Node>>();
            List<Tuple<string, string>> reportedRaces = new List<Tuple<string, string>>();

            foreach (Node n in HbGraph.Vertices)
            {
                if (n.GetType().ToString().Contains("MemAccess"))
                {
                    MemAccess loc1 = (MemAccess)n;
                    foreach (Node n1 in HbGraph.Vertices)
                    {
                        if (n.Equals(n1))
                            continue;
                        if (racyPairs.Contains(new Tuple<Node, Node>(n, n1)) || racyPairs.Contains(new Tuple<Node, Node>(n1, n)))
                            continue;
                        if (n1.GetType().ToString().Contains("MemAccess"))
                        {
                            MemAccess loc2 = (MemAccess)n1;

                            if (loc1.isWrite || loc2.isWrite)
                            {
                                if (loc1.objHandle == UIntPtr.Zero && loc1.offset == UIntPtr.Zero && loc2.objHandle == UIntPtr.Zero && loc2.offset == UIntPtr.Zero)
                                    continue;
                                if ((loc1.location == loc2.location) && (loc1.offset == loc2.offset) && !existsPath(n, n1) && !existsPath(n1, n))
                                {
                                    Console.WriteLine("RACE DETECTED: " + loc1.location + " and " + loc2.location + " i.e " + loc1.srcLocation + ";" + loc1.isWrite + " and " + loc2.srcLocation + ";" + loc2.isWrite);
                                    racyPairs.Add(new Tuple<Node, Node>(n, n1));
                                    reportedRaces.Add(new Tuple<string, string>(loc1.location + ";" + loc1.isWrite, loc2.location + ";" + loc2.isWrite));
                                }
                            }
                        }
                    }
                }
            }
        }

        /*static bool existsPath(Node n1, Node n2)
        {
            foreach(Node n in HbGraph.Vertices)
            {
                n.visited = false;
            }

            Queue<Node> dfsQ = new Queue<Node>();
            dfsQ.Enqueue(n1);

            while(dfsQ.Count > 0)
            {
                Node visiting = dfsQ.Dequeue();
                visiting.visited = true;

                IEnumerable<Edge> outEdges = HbGraph.Edges.Where(item => item.src.Equals(visiting));
                foreach(Edge outEdge in outEdges)
                {
                    Node successor = outEdge.trg;
                    if (successor.Equals(n2))
                    {
                        return true;
                    }
                    if(!successor.visited)
                        dfsQ.Enqueue(successor);
                }
            }

            return false;
        }*/

        static bool existsPath(Node n1, Node n2)
        {
            foreach (Node n in HbGraph.Vertices)
            {
                n.visited = false;
            }

            Stack<Node> dfsS = new Stack<Node>();
            dfsS.Push(n1);

            while (dfsS.Count > 0)
            {
                Node visiting = dfsS.Pop();
                visiting.visited = true;

                IEnumerable<Edge> outEdges = HbGraph.Edges.Where(item => item.src.Equals(visiting));
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
