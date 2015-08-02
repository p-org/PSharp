using Microsoft.PSharp;

namespace MultiPaxos
{
    #region Events

    class prepare : Event { }
    class accept : Event { }
    class agree : Event { }
    class reject : Event { }
    class accepted : Event { }
    class local : Event { }
    class success : Event { }
    class allNodes : Event { }
    class goPropose : Event { }
    class chosen : Event { }
    class update : Event { }
    class monitor_valueChosen : Event { }
    class monitor_valueProposed : Event { }
    class monitor_client_sent : Event { }
    class monitor_proposer_sent : Event { }
    class monitor_proposer_chosen : Event { }
    class Ping : Event { }
    class newLeader : Event { }
    class timeout : Event { }
    class startTimer : Event { }
    class cancelTimer : Event { }
    class cancelTimerSuccess : Event { }
    class response : Event { }

    #endregion
}
