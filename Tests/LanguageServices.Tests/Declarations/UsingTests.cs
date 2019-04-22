﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class UsingTests
    {
        [Fact(Timeout=5000)]
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

        [Fact(Timeout=5000)]
        public void TestIncorrectUsingDeclaration()
        {
            var test = "using System.Text";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact(Timeout=5000)]
        public void TestUsingDeclarationWithoutIdentifier()
        {
            var test = "using;";
            LanguageTestUtilities.AssertFailedTestLog("Expected identifier.", test);
        }
    }
}
