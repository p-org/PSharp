// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class UsingTests
    {
        #region correct tests

        [Fact]
        public void TestUsingDeclaration()
        {
            var test = @"
using System.Text;";
            var expected = @"
using Microsoft.PSharp;
using System.Text;
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestIncorrectUsingDeclaration()
        {
            var test = "using System.Text";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact]
        public void TestUsingDeclarationWithoutIdentifier()
        {
            var test = "using;";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }

        #endregion
    }
}
