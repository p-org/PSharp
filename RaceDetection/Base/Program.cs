using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ExtendedReflection.Utilities;
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Monitoring;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.ExtendedReflection.Metadata;
using EREngine;

namespace Base
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var executable = args[0];
            Console.WriteLine("reached " + executable);

            var path = Assembly.GetAssembly(typeof(Base.Program)).Location;

            /*try
            {
                Assembly.LoadFrom(executable);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Couldn't load assembly 2 " + ex);
                return;
            }*/


            var assembly = Assembly.LoadFrom(executable);

            string[] searchDirectories = new[]{
                Path.GetDirectoryName(path),
                Path.GetDirectoryName(assembly.Location),
            };

            var resolver = new AssemblyResolver();
            Array.ForEach(searchDirectories, d => resolver.AddSearchDirectory(d));
            resolver.Attach();
            var engine = new RemoteEngine();
            engine.Execute(executable, args);
        }
    }

    [Serializable]
    internal class RemoteEngine : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Execute(string executable, string[] args)
        {
            try
            {
                var assembly = Assembly.LoadFrom(executable);
                var engine = new MyEngine();
                var main = new MyMain(assembly);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }
    }
}
