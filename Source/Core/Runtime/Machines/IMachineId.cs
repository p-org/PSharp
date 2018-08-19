//-----------------------------------------------------------------------
// <copyright file="IMachineId.cs">
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

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Interface of a unique machine id.
    /// </summary>
    public interface IMachineId : IEquatable<IMachineId>, IComparable<IMachineId>
    {
        /// <summary>
        /// Unique id value.
        /// </summary>
        ulong Value { get; }

        /// <summary>
        /// Type of the machine.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        string FriendlyName { get; }
    }
}
