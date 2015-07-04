//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// The P# remote manager.
    /// </summary>
    public class Program
    {
        #region main

        static void Main(string[] args)
        {
            // Parses the command line options.
            new RemoteManagerCommandLineOptions(args).Parse();
            
            Manager.Run();

            Output.WriteLine(". Done");
        }

        #endregion
    }
}
