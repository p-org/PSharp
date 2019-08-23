namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Handles the <see cref="IMachineRuntime.OnEventDropped"/> event.
    /// </summary>
    public delegate void OnEventDroppedHandler(Event e, MachineId target);
}
