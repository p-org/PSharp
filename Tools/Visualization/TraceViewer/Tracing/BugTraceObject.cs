namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class implementing a bug trace object.
    /// </summary>
    public class BugTraceObject
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Machine { get; set; }
        public string MachineState { get; set; }
        public string Action { get; set; }
        public string TargetMachine { get; set; }
    }
}
