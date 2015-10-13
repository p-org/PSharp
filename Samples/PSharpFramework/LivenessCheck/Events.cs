using System.Collections.Generic;
using Microsoft.PSharp;

namespace LivenessCheck
{
    #region Events

    internal class Config : Event
    {
        public MachineId Id;

        public Config(MachineId id)
            : base(-1, -1)
        {
            this.Id = id;
        }
    }

    internal class MConfig : Event
    {
        public List<MachineId> Ids;

        public MConfig(List<MachineId> ids)
            : base(-1, -1)
        {
            this.Ids = ids;
        }
    }

    internal class Unit : Event { }
    internal class DoProcessing : Event { }
    internal class FinishedProcessing : Event { }
    internal class NotifyWorkerIsDone : Event { }

    #endregion
}
