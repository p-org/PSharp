//-----------------------------------------------------------------------
// <copyright file="EventTests.cs">
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

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class EventTests
    {
        [TestMethod, Timeout(10000)]
        public void TestEventDeclaration()
        {
            var test = @"
namespace Foo {
event e1;
internal event e2;
public event e3;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1 : Event
    {
        public e1()
            : base()
        {
        }
    }

    internal class e2 : Event
    {
        public e2()
            : base()
        {
        }
    }

    public class e3 : Event
    {
        public e3()
            : base()
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestSimpleGenericEventDeclaration()
        {
            var test = @"
namespace Foo {
event e1<T>;
internal event e2;
public event e3;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    public class e1<T> : Event
    {
        public e1()
            : base()
        {
        }
    }

    internal class e2 : Event
    {
        public e2()
            : base()
        {
        }
    }

    public class e3 : Event
    {
        public e3()
            : base()
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestComplexGenericEventDeclaration()
        {
            var test = @"
namespace Foo {
internal event e1<T, K>;
internal event e2;
public event e3;
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class e1<T,K> : Event
    {
        public e1()
            : base()
        {
        }
    }

    internal class e2 : Event
    {
        public e2()
            : base()
        {
        }
    }

    public class e3 : Event
    {
        public e3()
            : base()
        {
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithPayload()
        {
            var test = @"namespace Foo {
internal event e1 (m:string, n:int);
internal event e2 (m:string);
public event e3 (n:int);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class e1 : Event
    {
        public string m;
        public int n;

        public e1(string m, int n)
            : base()
        {
            this.m = m;
            this.n = n;
        }
    }

    internal class e2 : Event
    {
        public string m;

        public e2(string m)
            : base()
        {
            this.m = m;
        }
    }

    public class e3 : Event
    {
        public int n;

        public e3(int n)
            : base()
        {
            this.n = n;
        }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestGenericEventDeclarationWithPayload()
        {
            var test = @"
namespace Foo {
internal event e1<T, K> (m:K, n:T);
internal event e2 (m:string);
public event e3 (n:int);
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    internal class e1<T,K> : Event
    {
        public K m;
        public T n;

        public e1(K m, T n)
            : base()
        {
            this.m = m;
            this.n = n;
        }
    }

    internal class e2 : Event
    {
        public string m;

        public e2(string m)
            : base()
        {
            this.m = m;
        }
    }

    public class e3 : Event
    {
        public int n;

        public e3(int n)
            : base()
        {
            this.n = n;
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestEventInMachineDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
public event e1<T>;
internal event e2;
public event e3<T, K> (m:K, n:T);
internal event e4 (m:string);
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        public class e1<T> : Event
        {
            public e1()
                : base()
            {
            }
        }

        internal class e2 : Event
        {
            public e2()
                : base()
            {
            }
        }

        public class e3<T,K> : Event
        {
            public K m;
            public T n;

            public e3(K m, T n)
                : base()
            {
                this.m = m;
                this.n = n;
            }
        }

        internal class e4 : Event
        {
            public string m;

            public e4(string m)
                : base()
            {
                this.m = m;
            }
        }

        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }
    }
}
