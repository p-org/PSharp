using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Handles the <see cref="IMachineRuntime.OnFailure"/> event.
    /// </summary>
    public delegate void OnFailureHandler(Exception ex);
}
