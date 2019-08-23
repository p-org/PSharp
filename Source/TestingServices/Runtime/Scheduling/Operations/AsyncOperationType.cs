// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// The type of an operation performed by an asynchronous task.
    /// </summary>
    public enum AsyncOperationType
    {
        /// <summary>
        /// An asynchronous task starts executing.
        /// </summary>
        Start = 0,

        /// <summary>
        /// An asynchronous task creates another asynchronous task.
        /// </summary>
        Create,

        /// <summary>
        /// An asynchronous task sends an event.
        /// </summary>
        Send,

        /// <summary>
        /// An asynchronous task receives an event.
        /// </summary>
        Receive,

        /// <summary>
        /// An asynchronous task stops executing.
        /// </summary>
        Stop,

        /// <summary>
        /// An asynchronous task yields.
        /// </summary>
        Yield,

        /// <summary>
        /// An asynchronous task waits to reach quiescence.
        /// </summary>
        WaitForQuiescence,

        /// <summary>
        /// An asynchronous task waits for another asynchronous task to stop.
        /// </summary>
        Join
    }
}
