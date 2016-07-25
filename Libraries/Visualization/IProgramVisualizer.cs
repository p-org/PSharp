//-----------------------------------------------------------------------
// <copyright file="IProgramVisualizer.cs">
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

using System.Threading.Tasks;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Interface of a program visualizer.
    /// </summary>
    public interface IProgramVisualizer
    {
        /// <summary>
        /// Starts the visualisation asynchronously.
        /// </summary>
        /// <returns>Task</returns>
        Task StartAsync();

        /// <summary>
        /// Called when a testing iteration finishes
        /// </summary>
        void Step();

        /// <summary>
        /// Refreshes the visualization.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Adds a new transition.
        /// </summary>
        /// <param name="machineOrigin">Origin machine</param>
        /// <param name="stateOrigin">Origin state</param>
        /// <param name="edgeLabel">Edge label</param>
        /// <param name="machineTarget">Target machine</param>
        /// <param name="stateTarget">Target state</param>
        void AddTransition(string machineOrigin, string stateOrigin, string edgeLabel,
            string machineTarget, string stateTarget);

        /// <summary>
        /// Declares a state
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <param name="state">state name</param>
        void DeclareMachineState(string machine, string state);

        /// <summary>
        /// Declares a registered state, event pair
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <param name="state">state name</param>
        /// <param name="eventName">Event name that the state is prepared to handle</param>
        void DeclareStateEvent(string machine, string state, string eventName);
    }
}