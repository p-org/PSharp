//-----------------------------------------------------------------------
// <copyright file="IMachineFactory.cs">
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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Factory for creating P# machines.
    /// </summary>
    internal interface IMachineFactory
    {
        /// <summary>
        /// Types for which this factory is responsible
        /// </summary>
        Type BaseClassType();

        /// <summary>
        /// Creates a new P# machine of the specified type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Machine</returns>
        Machine Create(Type type);

    }
}