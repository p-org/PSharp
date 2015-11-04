using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class Log
    {
        public int Term;
        public int Command;
    }
}
