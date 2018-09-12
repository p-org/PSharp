using Microsoft.PSharp.PSharpStateMachineStructureViewer;
using System;
using System.Xml.Linq;
using Xunit;
using Microsoft.PSharp;
using System.Text;

namespace Microsoft.PSharp.StateMachineStructureViewer.Tests.Unit
{

    using MV = MachineVertex;
    using SV = StateVertex;
    using GT = GotoTransition;
    using IN = ContainsLink;

    public class BasicTests
    {

        [Fact]
        public void TestStateGroup()
        {
            string prog = @"using System;

namespace ns1{
	machine m1{
		event e1;
		event e2;
		event e3;
		start state init
		{
			on e1 goto sg1.s1;
			entry{ raise(e1); }
		}

		group sg1
		{
			state s1{on e2 goto s2; on e3 goto s3;}
			state s2{ }
		}

		state s1{ on e2 goto s2; }

		state s2{ }
		state s3{ }

	}
}";

            // Get DGML but remove first line.
            string dgml = StateDiagramViewer.GetDgmlForProgram(prog).Split(Environment.NewLine.ToCharArray(), 2)[1];
            DgmlParser dgmlParser = new DgmlParser();

            //StateMachineGraph G = dgmlParser.parseDgml(XDocument.Parse(dgml));
            StateMachineGraph G = dgmlParser.parseDgml(XDocument.Parse(dgml));
            string dumpstr1 = G.DumpString();
            Console.WriteLine(dumpstr1);


            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]
            {
            new MV("ns1.m1", new Edge[]{
                new IN(null, "ns1.m1.init"), new IN(null, "ns1.m1.sg1.s1"), new IN(null, "ns1.m1.sg1.s2") ,
                new IN(null, "ns1.m1.s1"), new IN(null, "ns1.m1.s2"), new IN(null, "ns1.m1.s3")
            }),
            new SV("ns1.m1.init", new Edge[]{
                new GT("ns1.m1.e1", "ns1.m1.sg1.s1")
            }),
            new SV("ns1.m1.sg1.s1", new Edge[] {
                new GT("ns1.m1.e2", "ns1.m1.sg1.s2"), new GT("ns1.m1.e3", "ns1.m1.s3")
            }),
            new SV("ns1.m1.sg1.s2", new Edge[] { } ),
            new SV("ns1.m1.s1", new Edge[] { new GT("ns1.m1.e2", "ns1.m1.s2") } ),
            new SV("ns1.m1.s2", new Edge[] { } ),
            new SV("ns1.m1.s3", new Edge[] { } )
            });

            string dumpstr2 = expectedGraph.DumpString();
            Console.WriteLine(dumpstr2);

            Assert.True(expectedGraph.DeepCheckEquality(G));
        }

        [Fact]
        public void TestMachineStateInheritance()
        {
            Assert.True(false);
        }
    }
}
