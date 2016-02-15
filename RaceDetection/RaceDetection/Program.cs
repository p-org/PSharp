using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RaceDetection
{
    class Program
    {
        static void Main(string[] args)
        {
            StringCollection referencedAssemblies = new StringCollection();
            //String input = "D:\\psharp-fabric\\PSharpShoppingList\\PSharpShoppingListServicePersistent\\bin\\Debug\\PSharpShoppingList.exe";
            //String input = "C:\\Users\\t-rasmud\\Documents\\visual studio 2015\\Projects\\RacyTests\\RacyTests\\bin\\Debug\\RacyTests.exe";
            //String input = "C:\\Users\\t-rasmud\\Documents\\visual studio 2015\\Projects\\RacyTests\\RacyCreatePayload\\bin\\Debug\\RacyCreatePayload.exe";
            //String input = "D:\\MigratingTable\\Migration\\bin\\Debug\\Migration.exe";
            //String input = "C:\\Users\\t-rasmud\\Documents\\Visual Studio 2015\\Projects\\RacyTests\\RacyInts\\bin\\Debug\\RacyInts.exe";
            //String input = "C:\\Users\\t-rasmud\\Documents\\Visual Studio 2015\\Projects\\RacyTests\\StructAddressCheck\\bin\\Debug\\StructAddressCheck.exe";
            //String input = "D:\\PSharp\\Samples\\CSharp\\Raft\\bin\\Debug\\Raft.exe";
            //String input = "D:\\PSharp\\Samples\\RacyBenchmarks\\RacyBenchmarks\\bin\\Debug\\BasicPaxosRacy.exe";
            String input = "D:\\PSharp\\Samples\\RacyBenchmarks\\BoundedAsyncRacy\\bin\\Debug\\BoundedAsyncRacy.exe";
            //String input = "D:\\PSharp\\Samples\\RacyBenchmarks\\ChainReplicationRacy\\bin\\Debug\\ChainReplicationRacy.exe";

            Assembly a = Assembly.LoadFrom(input);

            referencedAssemblies.Add(a.GetName().Name);

            AssemblyName[] an = a.GetReferencedAssemblies();
            foreach (AssemblyName item in an)
            {
                if (item.Name.Contains("mscorlib") /*|| item.Name.Contains("Model.Fabric")*/ || item.Name.Contains("System") || item.Name.Contains("NLog") || item.Name.Contains("System.Core"))
                    continue;
                referencedAssemblies.Add(item.Name);
            }

            string[] includedAssemblies = new string[referencedAssemblies.Count];
            referencedAssemblies.CopyTo(includedAssemblies, 0);
            //Old ExtendedReflection
            ProcessStartInfo info = ControllerSetUp.GetMonitorableProcessStartInfo(
                "D:\\PSharp\\RaceDetection\\Base\\bin\\Debug\\Base.exe",
                new String[] { WrapString(input) },
                MonitorInstrumentationFlags.All,
                true,

                null, // we don't monitor process at startup since it loads the DLL to monitor
                null, // ibid.

                null,
                null,
                null,
                null,
                null,
                includedAssemblies,
                null,

                null,
                null, null, null,
                null, null,

                null,
                false,
                null,
                true,
                false,
                ProfilerInteraction.Fail,
                null, "", ""
                );

            //new ExtendedReflection
            /*ProcessStartInfo info = ControllerSetup.GetMonitorableProcessStartInfo(
                "C:\\Users\\t-rasmud\\Documents\\Visual Studio 2015\\Projects\\Instr\\Base\\bin\\Debug\\Base.exe",                                       //filename
                new String[] { "C:\\Users\\t-rasmud\\Documents\\Sequential.exe" },     //arguments
                MonitorInstrumentationFlags.All,                                                                                        //monitor flags
                true,                                                                                                                   //track gc accesses

                null, // we don't monitor process at startup since it loads the DLL to monitor                                          //user assembly
                null, // ibid.                                                                                                          //user type

                null,                                                                                                                   //substitution assemblies
                null,                                                                                                                   //types to monitor
                null,                                                                                                                   //types to exclude monitor
                null,                                                                                                                   //namespaces to monitor
                null,                                                                                                                   //namespaces to exclude monitor
                new String[] { "Sequential" },                                                                                          //assemblies to monitor
                null,                                                                                                                   //assembliesToExcludeMonitor to exclude monitor

                null,                                                                                                                   //types to protect
                null,                                                                                                                   //erase cctortypes
                null,                                                                                                                   //erase finalizer types

                null,                                                                                                                   //clrmonitor log file name
                false,                                                                                                                  //clrmonitor  log verbose
                false,                                                                                                                  //crash on failure
                true,                                                                                                                   //protect all cctors
                false,                                                                                                                  //disable mscrolib suppressions
                ProfilerInteraction.Fail,                                                                                               //profiler interaction
                null, 
                "", 
                ""
                );*/
            var process = new Process();
            process.StartInfo = info;
            process.Start();
            process.WaitForExit();
            Console.WriteLine("Done instrumenting");
            Console.ReadLine();
        }

        private static string WrapString(string value)
        {
            if (value == null)
                return value;
            else
                return
                    SafeString.IndexOf(value, ' ') != -1 ? "\"" + value.TrimEnd('\\') + "\"" : value;
        }
    }
}
