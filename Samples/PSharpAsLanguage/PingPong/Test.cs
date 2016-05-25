using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;

namespace PingPong
{
    public class Test
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            Microsoft.PSharp.Utilities.IOLogger.InstallCustomLogger(new MyLogger());
            runtime.CreateMachine(typeof(Server));
        }
    }

    class MyLogger : System.IO.TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.ASCII;
            }
        }

        public override void Write(char value)
        {
            Console.Write(value);
        }
    }
}
