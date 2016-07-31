//-----------------------------------------------------------------------
// <copyright file="KnownTypesProvider.cs">
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
using System.Reflection;

using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Provider for known types. Used for serialization.
    /// </summary>
    internal static class KnownTypesProvider
    {
        /// <summary>
        /// Known types used for serialization.
        /// </summary>
        public static List<Type> KnownTypes = new List<Type> {
            typeof(TestReport),
            typeof(CoverageInfo),
            typeof(Transition)
        };

        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            return KnownTypes;
        }
    }
}
