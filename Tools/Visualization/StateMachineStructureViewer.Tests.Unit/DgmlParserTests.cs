using Microsoft.PSharp.PSharpStateMachineStructureViewer;
using System;
using Xunit;
using System.Xml.Linq;
namespace StateMachineStructureViewer.Tests.Unit
{
    using MV = MachineVertex;
    using SV = StateVertex;
    using GT = GotoTransition;
    using IN = ContainsLink;

    /* 
     * Set of meta tests to make sure we're not interpreting the output wrong
     */
    public class DgmlParserTests
    {
        [Fact]
        public void TestDgmlParserStateGroup()
        {
            DgmlParser dgmlParser = new DgmlParser();
            string dgml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<DirectedGraph xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">
  <Nodes>
	<Node Id=""m1"" label=""m1"" NodeType=""Machine""/>
	<Node Id=""init"" NodeType=""State"" label=""init""/>
	<Node Id=""sg1.s1"" NodeType=""State"" label=""s1""/>
	<Node Id=""sg1.s2"" NodeType=""State"" label=""s2""/>
	<Node Id=""s1"" NodeType=""State"" label=""s1""/>
	<Node Id=""s2"" NodeType=""State"" label=""s2""/>
	<Node Id=""s3"" NodeType=""State"" label=""s2""/>
  </Nodes>
  <Links>
    <Link source=""m1"" target=""init"" LinkType=""Contains""/>
	<Link source=""m1"" target=""sg1.s1"" LinkType=""Contains""/>
	<Link source=""m1"" target=""sg1.s2"" LinkType=""Contains""/>
	<Link source=""m1"" target=""s1"" LinkType=""Contains""/>
	<Link source=""m1"" target=""s2"" LinkType=""Contains""/>
	<Link source=""m1"" target=""s3"" LinkType=""Contains""/>
	
	<Link source=""init"" label=""e1""  target=""sg1.s1"" LinkType=""GotoTransition""/>
	<Link source=""sg1.s1"" label=""e2""  target=""sg1.s2"" LinkType=""GotoTransition""/>
	<Link source=""sg1.s1"" label=""e3""  target=""s3"" LinkType=""GotoTransition""/>
	<Link source=""s1"" label=""e2""  target=""s2"" LinkType=""GotoTransition""/>
  </Links>
</DirectedGraph>";

            /*using System;
            namespace DgmlWriterTests.StateGroupTest
            {
                machine m1
                {
                event e1;
                event e2;
                event e2nosg;
                    start state Init
                    {
                        on e1 goto sg1.s1;
                        entry{ raise(e1); }
                    }

                    group sg1
                    {
                        state s1{on e2 goto s2; on e3 goto s3;}
                        state s2{ }
                    }

                    state s1{ on e2 goto s2 }

                    state s2{ }
                    state s3{ }

                }
            }";*/

            StateMachineGraph G = dgmlParser.parseDgml(XDocument.Parse(dgml));
            string dumpstr1 = G.DumpString();
            Console.WriteLine(dumpstr1);


            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]
            {
             new MV("m1", new Edge[]{
                 new IN(null, "init"), new IN(null, "sg1.s1"), new IN(null, "sg1.s2") ,
                 new IN(null, "s1"), new IN(null, "s2"), new IN(null, "s3")
             }),
             new SV("init", new Edge[]{
                 new GT("e1", "sg1.s1")
             }),
             new SV("sg1.s1", new Edge[] {
                 new GT("e2", "sg1.s2"), new GT("e3", "s3")
             }),
             new SV("sg1.s2", new Edge[] { } ),
             new SV("s1", new Edge[] { new GT("e2", "s2") } ),
             new SV("s2", new Edge[] { } ),
             new SV("s3", new Edge[] { } )
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

        [Fact]
        public void TestStateMachineStructureViewerStateGroup()
        {
            Assert.True(false);
        }
    }
}
