//-----------------------------------------------------------------------
// <copyright file="IPSharpProgram.cs">
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

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// Interface to a P# program.
    /// </summary>
    public interface IPSharpProgram
    {
        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        void Rewrite();

        /// <summary>
        /// Returns the project of the P# program.
        /// </summary>
        /// <returns>PSharpProject</returns>
        PSharpProject GetProject();

        /// <summary>
        /// Returns the syntax tree of the P# program.
        /// </summary>
        /// <returns>SyntaxTree</returns>
        SyntaxTree GetSyntaxTree();

        /// <summary>
        /// Updates the syntax tree of the P# program.
        /// </summary>
        /// <param name="text">Text</param>
        void UpdateSyntaxTree(string text);
    }
}
