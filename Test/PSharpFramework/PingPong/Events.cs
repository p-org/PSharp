using Microsoft.PSharp;

namespace PingPong
{
    #region Events

    internal class Unit : Event
    {
        internal Unit()
            : base(-1, -1)
        { }
    }

    internal class Ping : Event
    {
        internal Ping()
            : base(-1, -1)
        { }
    }

    internal class Pong : Event
    {
        internal Pong()
            : base(-1, -1)
        { }
    }

    #endregion
}
