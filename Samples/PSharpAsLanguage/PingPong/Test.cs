using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;

namespace PingPong
{
    public class Test
    {
        /// <summary>
        /// Custom logger.
        /// </summary>
        static MyLogger MyLogger = null;

        static void Main(string[] args)
        {
            // Increases verbosity to see the log.
            var configuration = Microsoft.PSharp.Utilities.Configuration.Create();
            configuration.Verbose = 2;

            // Installs the custom logger.
            var myLogger = new MyLogger();
            Microsoft.PSharp.Utilities.IOLogger.InstallCustomLogger(myLogger);
            
            // Runs the program.
            var runtime = PSharpRuntime.Create(configuration);
            Test.Execute(runtime);

            // Closes the logger.
            myLogger.Close();

            Console.ReadLine();
        }

        /// <summary>
        /// Test initialization method (called before testing starts).
        /// </summary>
        [Microsoft.PSharp.TestInit]
        public static void Initialize()
        {
            Test.MyLogger = new MyLogger();
            Microsoft.PSharp.Utilities.IOLogger.InstallCustomLogger(Test.MyLogger);
        }

        /// <summary>
        /// Execute a test iteration (called repeatedly for
        /// each testing iteration).
        /// </summary>
        /// <param name="runtime"></param>
        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.CreateMachine(typeof(Server), "TheUltimateServerMachine");
        }

        /// <summary>
        /// Test cleanup (called when testing terminates).
        /// </summary>
        [Microsoft.PSharp.TestDispose]
        public static void Dispose()
        {
            Test.MyLogger.Close();
        }
    }

    /// <summary>
    /// Custom logger that just dumps to console.
    /// </summary>
    class MyLogger : System.IO.TextWriter
    {
        /// <summary>
        /// The encoding.
        /// </summary>
        public override Encoding Encoding
        {
            get
            {
                return Encoding.ASCII;
            }
        }

        /// <summary>
        /// Minimum override necessary.
        /// </summary>
        /// <param name="value">Character</param>
        public override void Write(char value)
        {
            Console.Write(value);
        }

        // Below are optional overrides (for performance).

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
