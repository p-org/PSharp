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
    public sealed class RewritingPassDependency : Attribute
    {
        /// <summary>
        /// Pass dependencies.
        /// </summary>
        internal Type[] Dependencies;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dependencies">Dependencies</param>
        public RewritingPassDependency(params Type[] dependencies)
        {
            this.Dependencies = dependencies;
        }
    }
}
