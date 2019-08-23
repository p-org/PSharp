// ------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    internal class EventDeclarations : IEnumerable<EventDeclaration>
    {
        /// <summary>
        /// Must keep these in declaration order.
        /// </summary>
        private readonly List<EventDeclaration> Declarations;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDeclarations"/> class.
        /// </summary>
        internal EventDeclarations()
        {
            this.Declarations = new List<EventDeclaration>();
        }

        internal void Add(EventDeclaration eventDeclaration, bool isExtern = false)
        {
            eventDeclaration.IsExtern = isExtern;
            this.Declarations.Add(eventDeclaration);
        }

        internal bool Find(string name, out EventDeclaration eventDeclaration)
        {
            eventDeclaration = this.Declarations.Find(decl => decl.Identifier.TextUnit.Text == name);
            return eventDeclaration != null;
        }

        /// <summary>
        /// Returns all event decls from base to most fully derived (including <paramref name="node"/>).
        /// </summary>
        internal static IEnumerable<EventDeclaration> EnumerateInheritance(EventDeclaration node)
        {
            var list = new List<EventDeclaration>() { node };
            for (; node.BaseClassDecl != null; node = node.BaseClassDecl)
            {
                list.Add(node.BaseClassDecl);
            }

            list.Reverse();
            return list.ToArray();
        }

        public IEnumerator<EventDeclaration> GetEnumerator() => this.Declarations.Where(decl => !decl.IsExtern).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
