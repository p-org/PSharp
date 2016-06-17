//-----------------------------------------------------------------------
// <copyright file="EntryPoint.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;

using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Monitoring;

namespace Microsoft.PSharp.Monitoring
{
    class EntryPoint
    {
        public EntryPoint(Assembly assembly)
        {
            TryLoadReferencedAssemblies(new[] { assembly });

            //Type t = assembly.GetType(mainClass, true);

            try
            {
                MethodInfo main_method = assembly.EntryPoint;
                object result = null;
                ParameterInfo[] parameters = main_method.GetParameters();
                //object classInstance = Activator.CreateInstance(t, null);
                if (parameters.Length == 0)
                {
                    result = main_method.Invoke(main_method, null);
                }
                else
                {
                    //The invoke does NOT work it throws "Object does not match target type"             
                    result = main_method.Invoke(main_method, new Object[] { new string[] { } });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public Exception ObjectAccessThreadMonitor_ReadAccess(UIntPtr address, uint size, bool @volatile)
        {
            return raw_access(address, size, @volatile, false);
        }

        private Exception raw_access(UIntPtr address, uint size, bool @volatile, bool write)
        {
            //Required?
            using (_ProtectingThreadContext.Acquire())
            {
                try
                {
                    //Console.WriteLine("Variable access hook");
                }
                catch (Exception e)
                {
                    return e;
                }
            }

            return null;
        }

        static private SafeDictionary<string, Assembly> assemblies = new SafeDictionary<string, Assembly>();

        private void TryLoadReferencedAssemblies(Assembly[] inputAssemblies)
        {
            var ws = new SafeDictionary<string, Assembly>();
            
            //foreach (Assembly b in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    ws.Add(b.GetName().FullName, b);
            //}

            foreach (Assembly a in inputAssemblies)
            {
                if (a == null)
                {
                    continue;
                }
                
                // recursively load all the assemblies reachables from the root!
                if (!assemblies.ContainsKey(a.GetName().FullName) && !ws.ContainsKey(a.GetName().FullName))
                {
                    ws.Add(a.GetName().FullName, a);
                }

                while (ws.Count > 0)
                {
                    var en = ws.Keys.GetEnumerator();
                    en.MoveNext();
                    var a_name = en.Current;
                    var a_assembly = ws[a_name];
                    assemblies.Add(a_name, a_assembly);
                    ws.Remove(a_name);

                    foreach (AssemblyName name in a_assembly.GetReferencedAssemblies())
                    {
                        Assembly b;
                        ExtendedReflection.Utilities.ReflectionHelper.TryLoadAssembly(name.FullName, out b);

                        if (b != null)
                        {
                            //Console.WriteLine("Loaded {0}", name.FullName);
                            if (!assemblies.ContainsKey(b.GetName().FullName) && !ws.ContainsKey(b.GetName().FullName))
                            {
                                ws.Add(b.GetName().FullName, b);
                            }
                        }
                    }
                }
            }
        }
    }
}
