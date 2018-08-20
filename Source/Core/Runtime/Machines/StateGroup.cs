//-----------------------------------------------------------------------
// <copyright file="StateGroup.cs">
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

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class used for representing a group of related states.
    /// </summary>
    public abstract class StateGroup
    {
        /// <summary>
        /// Returns the qualified (<see cref="StateGroup"/>) name of a <see cref="MachineState"/>.
        /// </summary>
        /// <param name="state">The machine state.</param>
        /// <returns>Qualified state name.</returns>
        internal static string GetQualifiedStateName(Type state)
        {
            var name = state.Name;

            while (state.DeclaringType != null)
            {
                if (!state.DeclaringType.IsSubclassOf(typeof(StateGroup))) break;
                name = string.Format("{0}.{1}", state.DeclaringType.Name, name);
                state = state.DeclaringType;
            }

            return name;
        }
    }
}
