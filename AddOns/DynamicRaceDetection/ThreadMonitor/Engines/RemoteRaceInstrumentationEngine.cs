//-----------------------------------------------------------------------
// <copyright file="RemoteRaceInstrumentationEngine.cs">
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

namespace Microsoft.PSharp.Monitoring
{
    [Serializable]
    internal class RemoteRaceInstrumentationEngine : MarshalByRefObject
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
                new RaceInstrumentationEngine();

                new EntryPoint(assembly);
                //Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //Environment.Exit(-1);
            }
        }
    }
}
