using System;
using System.Text;
using Microsoft.PSharp;
using Microsoft.PSharp.Utilities;

namespace PingPong.CustomLogging
{
    /// <summary>
    /// A simple PingPong application written using the P# high-level syntax.
    /// 
    /// The P# runtime starts by creating the P# machine 'NetworkEnvironment'. The
    /// 'NetworkEnvironment' machine then creates a 'Server' and a 'Client' machine,
    /// which then communicate by sending 'Ping' and 'Pong' events to each other for
    /// a limited amount of turns.
    /// 
    /// This sample shows how to install a custom logger during testing.
    /// 
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of P#.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Custom logger.
        /// </summary>
        static MyLogger MyLogger = null;

        static void Main(string[] args)
        {
            // Installs the custom logger.
            var myLogger = new MyLogger();
            IOLogger.InstallCustomLogger(myLogger);

            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            Program.Execute(runtime);

            // Closes the logger.
            myLogger.Close();

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        /// <summary>
        /// Test initialization method (called before testing starts).
        /// </summary>
        [Microsoft.PSharp.TestInit]
        public static void Initialize()
        {
            Program.MyLogger = new MyLogger();
            IOLogger.InstallCustomLogger(Program.MyLogger);
        }

        /// <summary>
        /// Execute a test iteration (called repeatedly for
        /// each testing iteration).
        /// </summary>
        /// <param name="runtime"></param>
        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            // Assigns a user-defined name to this network environment machine.
            runtime.CreateMachine(typeof(NetworkEnvironment), "TheUltimateNetworkEnvironmentMachine");
        }

        /// <summary>
        /// Test cleanup (called when testing terminates).
        /// </summary>
        [Microsoft.PSharp.TestDispose]
        public static void Dispose()
        {
            Program.MyLogger.Close();
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

        // The following are optional overrides (for performance).

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
