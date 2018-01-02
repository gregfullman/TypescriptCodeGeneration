using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public static class Extensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> range)
        {
            foreach (var item in range)
                list.Add(item);
        }

        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> range)
        {
            foreach (var item in range)
                set.Add(item);
        }

        public static void GetNamespaceAndName(string input, out string ns, out string name)
        {
            ns = null;
            name = null;
            string[] pieces = input.Split(new[] { '.' });
            if (pieces.Length > 0)
            {
                name = pieces[pieces.Length - 1];
                if (pieces.Length > 1)
                {
                    ns = string.Join(".", pieces.Take(pieces.Length - 1));
                }
            }
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static ImmutableArray<ISymbol> GetAccessibleMembersInThisAndBaseTypes(this ITypeSymbol containingType, SymbolKind kind, Accessibility accessibility)
        {
            return containingType.GetBaseTypesAndThis()
                                 .Where(x => !x.Equals(containingType))
                                 .SelectMany(x => x.GetMembers()
                                                   .OfType<ISymbol>()
                                                   .Where(y => y.Kind == kind && y.DeclaredAccessibility == accessibility))
                                 .ToImmutableArray();
        }
    }
}
