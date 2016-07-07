//-----------------------------------------------------------------------
// <copyright file="BugTraceObject.cs">
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

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class implementing a bug trace object.
    /// </summary>
    internal class BugTraceObject
    {
        internal string Type { get; set; }
        internal string Machine { get; set; }
        internal int MachineId { get; set; }
        internal string TargetMachine { get; set; }
        internal int TargetMachineId { get; set; }
        internal string Event { get; set; }
    }
}
