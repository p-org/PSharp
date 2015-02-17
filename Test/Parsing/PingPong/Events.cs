using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PingPong
{
    #region Events

    internal event Ping;
    internal event Pong;
    internal event Stop;
    internal event Unit;

    #endregion
}
