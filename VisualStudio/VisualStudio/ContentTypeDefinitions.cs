//-----------------------------------------------------------------------
// <copyright file="ContentTypeDefinitions.cs">
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// Exports content type definitions.
    /// </summary>
    internal static class ContentTypeDefinitions
    {
        [Export]
        [Name("psharp")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition PSharpContentType = null;

        [Export]
        [FileExtension(".psharp")]
        [ContentType("psharp")]
        internal static FileExtensionToContentTypeDefinition PSharpFileType = null;
    }
}
