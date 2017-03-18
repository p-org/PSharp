//-----------------------------------------------------------------------
// <copyright file="Program.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// The P# remote manager.
    /// </summary>
    public class Program
    {
        #region main

        static void Main(string[] args)
        {
            // Parses the command line options to get the configuration.
            var configuration = new RuntimeContainerCommandLineOptions(args).Parse();

            Container.Configure(configuration);
            Container.Run();

            Output.WriteLine(". Done");
        }

        #endregion
    }
}
