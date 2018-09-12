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
            StateDiagramViewer.ResetResolutionHelper();
            string prog = @"
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

            StateMachineGraph G = dgmlParser.parseDgml(XDocument.Parse(dgml));
            
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

            Assert.True(expectedGraph.DeepCheckEquality(G));
        }

        [Fact]
        public void TestMachineStateInheritance()
        {
            StateDiagramViewer.ResetResolutionHelper();
            string prog = @"
namespace ns1
{
    machine bm1
    {
        event e1;
        event e2;
        start state binit
        {
            on e1 goto s1;
            entry{ raise(e1); }
        }
        state s1
        {
            on e2 goto s2;
            entry { raise(s2); }
        }
        state s2 { }
    }
    machine dm1 : bm1
    {
        event e1;
        start state dinit {
            on e1 goto s1;
        }
        state s2{ }
    }
}
";
            string dgml = StateDiagramViewer.GetDgmlForProgram(prog).Split(Environment.NewLine.ToCharArray(), 2)[1];
            DgmlParser dgmlParser = new DgmlParser();

            StateMachineGraph G = dgmlParser.parseDgml(XDocument.Parse(dgml));
            
            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]{
                new MV("ns1.bm1", new Edge[]{
                    new IN(null, "ns1.bm1.binit"), new IN(null, "ns1.bm1.s1"), new IN(null, "ns1.bm1.s2")
                }),
                new MV("ns1.dm1", new Edge[]{
                    new IN(null, "ns1.dm1.dinit"), new IN(null, "ns1.dm1.s2"),
                    new IN(null, "ns1.dm1>ns1.bm1.binit"), new IN(null, "ns1.dm1>ns1.bm1.s1"), new IN(null, "ns1.dm1>ns1.bm1.s2")
                }),
                // bm1 states
                new SV("ns1.bm1.binit", new Edge[]{
                    new GT("ns1.bm1.e1", "ns1.bm1.s1")
                }),
                new SV("ns1.bm1.s1", new Edge[]{
                    new GT("ns1.bm1.e2", "ns1.bm1.s2")
                }),
                new SV("ns1.bm1.s2", new Edge[]{}),
                // dm1 states
                new SV("ns1.dm1.dinit", new Edge[]{
                    new GT("ns1.dm1.e1", "ns1.dm1>ns1.bm1.s1")
                }),
                new SV("ns1.dm1.s2", new Edge[]{}),
                // States inherited by dm1
                new SV("ns1.dm1>ns1.bm1.binit", new Edge[]{
                    new GT("ns1.bm1.e1", "ns1.dm1>ns1.bm1.s1")
                }),
                new SV("ns1.dm1>ns1.bm1.s1", new Edge[]{
                    new GT("ns1.bm1.e2", "ns1.dm1>ns1.bm1.s2")
                }),
                new SV("ns1.dm1>ns1.bm1.s2", new Edge[]{}),
            });
            
            Assert.True(expectedGraph.DeepCheckEquality(G));
        }
    }
}
