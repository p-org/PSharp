using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The halt event.
    /// </summary>
    [DataContract]
    public sealed class Halt : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Halt"/> class.
        /// </summary>
        public Halt()
            : base()
        {
        }
    }
}
