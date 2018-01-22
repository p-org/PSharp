using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    class EventDeclarations : IEnumerable<EventDeclaration>
    {
        // Must keep these in declaration order.
        private List<EventDeclaration> declarations = new List<EventDeclaration>();

        internal EventDeclarations()
        {
        }

        internal void Add(EventDeclaration eventDeclaration, bool isExtern = false)
        {
            eventDeclaration.IsExtern = isExtern;
            this.declarations.Add(eventDeclaration);
        }

        internal bool Find(string name, out EventDeclaration eventDeclaration)
        {
            eventDeclaration = this.declarations.Find(decl => decl.Identifier.TextUnit.Text == name);
            return eventDeclaration != null;
        }

        /// <summary>
        /// Returns all event decls from base to most fully derived (including <paramref name="node"/>).
        /// </summary>
        /// <param name="node">The EventDeclaration to evaluate</param>
        /// <returns></returns>
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

        #region IEnumerable<EventDeclaration>

        public IEnumerator<EventDeclaration> GetEnumerator() => this.declarations.Where(decl => !decl.IsExtern).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable<EventDeclaration>
    }
}
