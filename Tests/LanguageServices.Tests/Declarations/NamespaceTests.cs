// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class NamespaceTests
    {
        [Fact(Timeout=5000)]
        public void TestNamespaceDeclaration()
        {
            var test = @"
namespace Foo { }";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestNamespaceDeclaration2()
        {
            var test = @"
namespace Foo { }
namespace Bar { }";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
}

namespace Bar
{
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestNamespaceDeclarationCompact()
        {
            var test = @"
namespace Foo{}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact(Timeout=5000)]
        public void TestUnexpectedTokenWithoutNamespace()
        {
            var test = "private";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [Fact(Timeout=5000)]
        public void TestNamespaceDeclarationWithoutIdentifier()
        {
            var test = "namespace { }";
            LanguageTestUtilities.AssertFailedTestLog("Expected namespace identifier.", test);
        }
    }
}
