// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
