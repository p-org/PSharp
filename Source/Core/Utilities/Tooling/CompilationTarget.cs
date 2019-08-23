using
namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// P# compilation target.
    /// </summary>
    public enum CompilationTarget
    {
        /// <summary>
        /// Enables execution compilation target.
        /// </summary>
        Execution = 0,

        /// <summary>
        /// Enables library compilation target.
        /// </summary>
        Library = 1,

        /// <summary>
        /// Enables testing compilation target.
        /// </summary>
        Testing = 2,

        /// <summary>
        /// Enables remote compilation target.
        /// </summary>
        Remote = 3
    }
}
