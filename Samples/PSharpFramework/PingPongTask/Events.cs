using Microsoft.PSharp;

namespace PingPong
{
    #region Events

    internal class Unit : Event { }
    internal class Ping : Event { }
    internal class Pong : Event { }
    internal class Bad : Event { }

    #endregion
}
