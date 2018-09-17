#if NET46 || NET45
// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Tester.Utilities
{
    internal static class DependencyGraph
    {
        private class DependencyNode
        {
            internal string Name { get; private set; }
            internal DependencyNode Next { get; private set; }

            internal DependencyNode(string name)
            {
                this.Name = name;
            }

            internal DependencyNode Append(string name)
            {
                return new DependencyNode(name) { Next = this };
            }

            internal void Process(Action<string> action)
            {
                action(this.Name);
                for (var node = this.Next; node != null; node = node.Next)
                {
                    node.Process(action);
                }
            }
        }

        internal static string[] GetDependenciesToPSharp(Configuration configuration)
        {
            var domain = AppDomain.CreateDomain("DependentAssemblyLoading");
            try
            {
                // We need to do this in a separate AppDomain so we can unload the assemblies to allow instrumentation.
                var loader = (DependentAssemblyLoader)domain.CreateInstanceAndUnwrap(
                        Assembly.GetExecutingAssembly().FullName, typeof(DependentAssemblyLoader).FullName);
                return loader.GetDependenciesToPSharp(configuration.AssemblyToBeAnalyzed);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        internal static string[] GetDependenciesToTarget(string source, HashSet<string> allNames, Func<string, string[]> dependenciesFunc,
                                                  Func<string, bool> isTargetFunc)
        {
            var known = new Dictionary<string, bool>();
            var queue = new Queue<DependencyNode>();

            void evaluate()
            {
                // Adding it initially handles circular dependencies.
                var node = queue.Dequeue();
                known[node.Name] = false;

                // If this has hit the target it may still have other dependencies we want.
                var dependencies = dependenciesFunc(node.Name);
                var isTarget = dependencies.Any(dep => isTargetFunc(dep));
                if (isTarget)
                {
                    node.Process(n => known[n] = true);
                }
                foreach (var name in dependencies.Where(n => allNames.Contains(n) && !known.ContainsKey(n)))
                {
                    queue.Enqueue(isTarget ? new DependencyNode(name) : node.Append(name));
                }
            }

            for (queue.Enqueue(new DependencyNode(source)); queue.Count > 0; /* queue adjusted in loop */)
            {
                evaluate();
            }
            known[source] = true;   // Always return the source
            return known.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
        }

#if false
        internal void Test()
        {
            string[] getDependencies(string key)
            {
                switch (key)
                {
                    case "A":
                        return "B C D".Split();
                    case "B":
                        return "C E f".Split();
                    case "C":
                        return "c d e".Split();
                    case "D":
                        return "F X Z".Split();
                    case "E":
                        return "Y Z".Split();
                    case "F":
                        return "Z".Split();
                }
                return new string[0];
            }

            bool isTarget(string key)
            {
                return key == "Z";
            }

            string stringify(string[] strings)
            {
                var list = strings.ToList();
                list.Sort();
                return string.Join(" ", list);
            }

            void compare(string[] expected, string[] actual)
            {
                var exp = stringify(expected);
                var act = stringify(actual);
                var cmp = exp == act ? "Pass" : "Fail";
                Console.WriteLine($"{exp} | {act} : {cmp}");
            }

            var allNames = new HashSet<string>("A B C D E F G H I X Y Z".Split());
            var deps = GetDependenciesToTarget("A", allNames, name => getDependencies(name), name => isTarget(name));
            compare("A B E D F".Split(), deps);
            deps = GetDependenciesToTarget("B", allNames, name => getDependencies(name), name => isTarget(name));
            compare("B E".Split(), deps);
            deps = GetDependenciesToTarget("C", allNames, name => getDependencies(name), name => isTarget(name));
            compare("C".Split(), deps);
            deps = GetDependenciesToTarget("D", allNames, name => getDependencies(name), name => isTarget(name));
            compare("D F".Split(), deps);
            deps = GetDependenciesToTarget("F", allNames, name => getDependencies(name), name => isTarget(name));
            compare("F".Split(), deps);

            var numbers = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();
            allNames = new HashSet<string>(numbers);
            deps = GetDependenciesToTarget("0", allNames, name => new[] { (int.Parse(name) + 1).ToString() }, name => name == (numbers.Length - 1).ToString());
            compare(Enumerable.Range(0, 9).Select(i => i.ToString()).ToArray(), deps);
            deps = GetDependenciesToTarget("0", allNames, name => new[] { (int.Parse(name) + 1).ToString() }, name => false);
            compare("0".Split(), deps);

            // Sanity check for recursion depth
            numbers = Enumerable.Range(0, 100000).Select(i => i.ToString()).ToArray();
            allNames = new HashSet<string>(numbers);
            deps = GetDependenciesToTarget("0", allNames, name => new[] { (int.Parse(name) + 1).ToString() }, name => false);
            compare("0".Split(), deps);
        }
#endif
    }

    public sealed class DependentAssemblyLoader : MarshalByRefObject
    {
        public string[] GetDependenciesToPSharp(string assemblyUnderTest)
        {
            // Get the case-normalized directory name
            var fullTestAssemblyName = Path.GetFullPath(assemblyUnderTest);
            var dir = Path.GetDirectoryName(Assembly.ReflectionOnlyLoadFrom(fullTestAssemblyName).Location);
            var allNames = new HashSet<string>(Directory.GetFiles(dir, "*.dll"));

            // Because Assembly.GetReferencedAssemblies does not yet have the path (assembly resolution is complex), we will assume that
            // any assembly that matches a name in the executing directory is the referenced assembly.
            var assemblyNameToFullPathMap = allNames.ToDictionary(name => Path.GetFileNameWithoutExtension(name), name => name);

            string getAssemblyFullPath(AssemblyName assemblyName) =>
                assemblyNameToFullPathMap.ContainsKey(assemblyName.Name) ? assemblyNameToFullPathMap[assemblyName.Name] : string.Empty;

            string[] getDependencies(string fullPath)
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(fullPath);
                return assembly.GetReferencedAssemblies().Select(getAssemblyFullPath).Where(x => x.Length > 0).ToArray();
            }

            bool isTarget(string fullPath) => Path.GetFileName(fullPath).StartsWith("Microsoft.PSharp.");

            return DependencyGraph.GetDependenciesToTarget(fullTestAssemblyName, allNames, name => getDependencies(name), name => isTarget(name));
        }
    }
}
#endif
