//-----------------------------------------------------------------------
// <copyright file="OptimizationTarget.cs">
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

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// P# compilation optimization target.
    /// </summary>
    public enum OptimizationTarget
    {
        /// <summary>
        /// Enables debug optimization target.
        /// </summary>
        Debug = 0,
        /// <summary>
        /// Enables release optimization target.
        /// </summary>
        Release = 1
    }
}
