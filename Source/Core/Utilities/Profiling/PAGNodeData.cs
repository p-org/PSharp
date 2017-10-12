using System;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// An enum enumerating the type of nodes in the PAG (Program Activity Graph)
    /// </summary>
    public enum PAGNodeType
    {
        /// <summary> The caller of CreateMachine </summary>
        Creator,

        /// <summary> The first node of the machine created using CreateMachine </summary>
        Child,

        /// <summary> An action begin </summary>
        ActionBegin,

        /// <summary> An action end </summary>
        ActionEnd,

        /// <summary> A message send </summary>
        Send,

        /// <summary> A dequeue end </summary>
        DequeueEnd,

        /// <summary> The beginning of a receive action </summary>
        ReceiveBegin,

        /// <summary> The end of a receive action </summary>
        ReceiveEnd,

        /// <summary> A sink node </summary>
        Sink
    };

    /// <summary>
    /// Data that a node in a Program Activity Graph (PAG) holds
    /// </summary>
    public class PAGNodeData
    {
        /// <summary>
        /// The name of the node
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The time spent idling after the previous event until
        /// this node is created
        /// </summary>
        public long IdleTime { get; private set; }

        /// <summary>
        /// The time at which this node is created
        /// </summary>
        public long Timestamp { get; private set; }

        /// <summary>
        /// An enum representing the type of event the node represents
        /// </summary>
        public PAGNodeType NodeType { get; private set; }

        /// <summary>
        /// The id of the machine whose event this node represents
        /// </summary>
        public long MachineId;

        /// <summary>
        /// The node representing the next interesting event on this machine
        /// </summary>
        public Node<PAGNodeData> MachineSuccessor;

        /// <summary>
        /// The node representing the previous interesting event on this machine
        /// </summary>
        public Node<PAGNodeData> MachinePredecessor;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="idleTime"></param>
        /// <param name="nodeType"></param>
        /// <param name="timestamp"></param>
        /// <param name="machineId"></param>
        public PAGNodeData(string name, long idleTime, PAGNodeType nodeType, long timestamp, long machineId)
        {
            Name = name;
            IdleTime = idleTime;
            Timestamp = timestamp;
            NodeType = nodeType;
            this.MachineId = machineId;
        }

        /// <summary>
        /// A string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IdleTime == 0)
                return String.Format("{0}[{1}]", Name, Timestamp);
            else
                return String.Format("{0}[{1}/{2}]", Name, IdleTime, Timestamp);
        }
    }
}