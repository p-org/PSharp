// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// Attribute for custom C# rewriting pass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomCSharpRewritingPass : Attribute { }
}
