using Microsoft.PSharp;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// Interface to the P# critical path profiler
    /// </summary>
    public interface ICriticalPathProfiler
    {
        /// <summary>
        /// The configuration that says whether profiling is enabled
        /// </summary>
        Configuration Configuration { get; set; }

        /// <summary>
        /// Start our profiler. To be invoked immediately after runtime creation and
        /// profiler installation on the runtime
        /// </summary>
        void StartCriticalPathProfiling();

        /// <summary>
        /// Stop the profiler
        /// </summary>
        void StopCriticalPathProfiling();

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machine">The machine that the event is being dequeued by.</param>
        /// <param name="eventSequenceNumber">the global sequence number associated with the
        /// send corresponding to this dequeue.</param>
        void OnDequeueEnd(Machine machine, long eventSequenceNumber);

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="source">A reference to the source machine.</param>
        /// <param name="eventSequenceNumber">The global sequence number of this send.</param>
        void OnSend(Machine source, long eventSequenceNumber);

        /// <summary>
        /// Called when a machine begins waiting to receive an event.
        /// </summary>
        /// <param name="machine">Id of the machine that is entering the wait state.</param>
        /// <param name="eventNames">The names of the specific events being waited for, if any.</param>
        void OnReceiveBegin(Machine machine, string eventNames);

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machine">The machine that received the event.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        /// <param name="eventSequenceNumber">the sequence number of the send corresponding to this event.</param>
        void OnReceiveEnd(Machine machine, string eventName, bool wasBlocked, long eventSequenceNumber);

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="parent">The creator machine.</param>
        /// <param name="child">The machine that has been created.</param>
        void OnCreateMachine(Machine parent, Machine child);

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="machine">The machine that has been halted.</param>
        void OnHalt(Machine machine);

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machine">The machine executing the action.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnActionEnter(Machine machine, string actionName);

        /// <summary>
        /// Called when a machine finishes an action.
        /// </summary>
        /// <param name="machine">The machine executing the action.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnActionExit(Machine machine, string actionName);

        /// <summary>
        /// Predicts what the critical path will be if action with name "actionName"
        /// that originally took x time, takes "optimiaztionFactor" time instead
        /// and serializes the new PAG
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="optimizationFactor"></param>
        void Query(string actionName, int optimizationFactor);
    }
}