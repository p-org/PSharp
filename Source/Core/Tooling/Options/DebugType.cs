﻿//-----------------------------------------------------------------------
// <copyright file="DebugType.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PSharp.Tooling
{
    /// <summary>
    /// The debug type.
    /// </summary>
    internal enum DebugType
    {
        None = 0,
        All,
        Parsing,
        Rewriting,
        Runtime,
        Analysis,
        Testing,
        Liveness
    }
}
