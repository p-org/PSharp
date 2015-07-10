using Microsoft.PSharp;

namespace LivenessCheck
{
    #region Events

    internal class Unit : Event { }
    internal class DoProcessing : Event { }
    internal class FinishedProcessing : Event { }
    internal class NotifyWorkerIsDone : Event { }

    #endregion
}
