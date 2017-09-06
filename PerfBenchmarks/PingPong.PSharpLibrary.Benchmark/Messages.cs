using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingPong.PSharpLibrary
{
    public class Messages
    {
        public class Run : Event { };

        public class Started : Event { };

        public class Msg : Event { };
    }
}
