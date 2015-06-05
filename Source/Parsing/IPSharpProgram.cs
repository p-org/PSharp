//-----------------------------------------------------------------------
// <copyright file="IPSharpProgram.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Interface to a P# program.
    /// </summary>
    public interface IPSharpProgram
    {
        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        /// <returns>Rewritten text</returns>
        string Rewrite();

        /// <summary>
        /// Returns the rewritten to C#-IR text of this P# program.
        /// </summary>
        /// <returns>Rewritten text</returns>
        string GetRewrittenText();
    }
}
