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
            // Increase verbosity to see the log 
            var conf = Microsoft.PSharp.Utilities.Configuration.Create();
            conf.Verbose = 2;

            // Install custom logger
            var my_logger = new MyLogger();
            Microsoft.PSharp.Utilities.IOLogger.InstallCustomLogger(my_logger);
            
            // Run the program
            var runtime = PSharpRuntime.Create(conf);
            Test.Execute(runtime);

            // Done
            my_logger.Close();

            Console.ReadLine();
        }

        static MyLogger my_logger = null;

        // Test initialization method (called before testing starts)
        [Microsoft.PSharp.TestInit]
        public static void Init()
        {
            Console.WriteLine("Calling Init");
            my_logger = new MyLogger();
            Microsoft.PSharp.Utilities.IOLogger.InstallCustomLogger(my_logger);
        }

        // Execute a test iteration (called repeatedly for each testing iteration)
        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            Console.WriteLine("Calling Execute");
            runtime.CreateMachine(typeof(Server));
        }

        // Test cleanup (called at the end of testing)
        [Microsoft.PSharp.TestClose]
        public static void Close()
        {
            Console.WriteLine("Calling Close");
            my_logger.Close();
        }
    }

    // Custom logger. Just dumps to console
    class MyLogger : System.IO.TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.ASCII;
            }
        }

        // Minimum override necessary
        public override void Write(char value)
        {
            Console.Write(value);
        }

        // Below are optional overrides (for performance)

        public override void Write(string value)
        {
            Console.Write(value);
        }

        public override void WriteLine(string value)
        {
            Console.WriteLine("MyLogger: {0}", value);
        }
    }
}
