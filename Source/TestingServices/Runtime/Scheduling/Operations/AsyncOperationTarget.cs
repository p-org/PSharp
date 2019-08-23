namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// The target of an operation performed by an asynchronous task.
    /// </summary>
    public enum AsyncOperationTarget
    {
        /// <summary>
        /// The target of the operation is a task. For example, 'Create', 'Start'
        /// and 'Stop' are operations that act upon a task.
        /// </summary>
        Task = 0,

        /// <summary>
        /// The target of the operation is an inbox. For example, 'Send'
        /// and 'Receive' are operations that act upon an inbox.
        /// </summary>
        Inbox
    }
}
