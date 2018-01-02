using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public static class TypeMapping
    {
        private static SymbolDisplayFormat _symDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private static IDictionary<string, string> defaultMappings = new Dictionary<string, string>(){
            { "System.DateTime", "Date"},
            { "System.String", "string"},

            { "System.UByte", "number"},
            { "System.UInt16", "number"},
            { "System.UInt32", "number"},
            { "System.UInt64", "number"},

            { "System.Byte", "number"},
            { "System.Int16", "number"},
            { "System.Int32", "number"},
            { "System.Int64", "number"},
            { "System.Double", "number"},
            { "System.Float", "number"},
            { "System.Decimal", "number"},

            { "System.Boolean", "boolean"},
            { "System.Object", "any"},
            { "System.Nullable`1", "@@T0@@"},
            { "System.Collections.Generic.IDictionary`2", "{ [index: @@T0@@]:@@T1@@}"},
            { "System.Collections.Generic.Dictionary`2", "{ [index: @@T0@@]:@@T1@@}"},

            { "System.Collections.Generic.IEnumerable`1", "@@T0@@[]"},
            { "System.Collections.Generic.IReadOnlyList`1", "@@T0@@[]"},
            { "System.Collections.Generic.List`1", "@@T0@@[]"},
            { "System.Array`1", "@@T0@@[]"},
            { "System.Array`2", "@@T0@@[][]"},
            { "System.Array`3", "@@T0@@[][][]"}
        };

        public static IDictionary<string, string> DefaultMappings { get { return defaultMappings; } }


        public static string GetTsType(ITypeSymbol typeSymbol, IDictionary<string, string> mappings, Dictionary<string, HashSet<string>> references)
        {
            if (mappings == null)
                mappings = defaultMappings;

            if (typeSymbol is ITypeParameterSymbol)
            {
                // need to make sure this type parameter can actually be resolved within Typescript.
                // TODO: if we ever allow more than one type argument, this needs to be revisited
                if ((typeSymbol as ITypeParameterSymbol).DeclaringType.TypeArguments.Length == 1)
                    return typeSymbol.ToString();
                else
                    return "any";
            }
            var typeWithNs = typeSymbol.ContainingNamespace + "." + typeSymbol.MetadataName;
            if (typeSymbol is IArrayTypeSymbol)
            {
                var art = (IArrayTypeSymbol)typeSymbol;
                typeWithNs = "System.Array`" + art.Rank.ToString();
                if (mappings.ContainsKey(typeWithNs))
                {
                    return mappings[typeWithNs].Replace("@@T0@@", GetTsType(art.ElementType, mappings, references));
                }
                else
                {
                    return GetTsType(art.ElementType, mappings, references) + string.Concat(Enumerable.Repeat("[]", art.Rank));
                }
            }
            if (typeSymbol is INamedTypeSymbol)
            {
                var nts = (INamedTypeSymbol)typeSymbol;

                // TODO: detect if the type has declaring syntax references elsewhere in the solution. If so,
                // we need to find the type (if it exists), and if it doesn't exist, put a "pending reference binding" on it.

                if (nts.TypeArguments.Length > 0)
                {
                    string[] types = nts.TypeArguments.Select(s => GetTsType(s, mappings, references)).ToArray();
                    string toReturn = null;
                    if (mappings.ContainsKey(typeWithNs))
                    {
                        toReturn = mappings[typeWithNs];
                    }
                    else
                    {
                        // Prevent multiple generic parameters
                        // TODO: revisit when/if we want to support this
                        if(types.Length > 1)
                        {
                            types = new string[0];
                        }

                        var baseTypeName = typeSymbol.BaseType != null ? typeSymbol.BaseType.ToDisplayString(_symDisplayFormat) : null;
                        if (baseTypeName != null && baseTypeName != "System.Object" && typeSymbol.OriginalDefinition.Locations.Any(s => s.IsInSource))
                        {
                            var fqName = typeSymbol.OriginalDefinition.ToDisplayString(_symDisplayFormat);
                            var locations = typeSymbol.OriginalDefinition.Locations.Select(x => x.SourceTree.FilePath);
                            if (!references.ContainsKey(fqName))
                                references.Add(fqName, new HashSet<string>(locations));
                            else
                                references[fqName].AddRange(locations);
                        }

                        foreach (var interf in typeSymbol.AllInterfaces)
                        {
                            var interfaceName = interf.ToDisplayString(_symDisplayFormat);
                            if (interfaceName.StartsWith("System.Collections.Generic.IDictionary"))
                            {
                                // TODO: not supported yet
                                break;
                            }
                            else if (interfaceName.StartsWith("System.Collections.Generic.IList") ||
                                    interfaceName.StartsWith("System.Collections.Generic.ICollection") ||
                                    interfaceName.StartsWith("System.Collections.Generic.IEnumerable"))
                            {
                                toReturn = "Array" + "@@TypeArgs@@";
                                break;
                            }
                            else if (interfaceName.StartsWith("System.Collections.IList") ||
                                    interfaceName.StartsWith("System.Collectins.ICollection") ||
                                    interfaceName.StartsWith("System.Collectins.IEnumerable"))
                            {
                                toReturn = "Array";
                                break;
                            }
                        }

                        if (toReturn == null)
                        {
                            toReturn = typeSymbol.ContainingNamespace + "." + typeSymbol.Name;
                            if (types.Length > 0)
                                toReturn = toReturn + "@@TypeArgs@@";
                        }
                    }

                    for (int i = 0; i < types.Length; i++)
                    {
                        toReturn = toReturn.Replace("@@T" + i.ToString() + "@@", types[i]);
                    }
                    if (toReturn.Contains("@@TypeArgs@@"))
                    {
                        var typeArgs = "<";
                        for (int i = 0; i < types.Length; i++)
                        {
                            if (i != 0)
                                typeArgs += ",";
                            typeArgs += types[i];
                        }
                        typeArgs += ">";
                        toReturn = toReturn.Replace("@@TypeArgs@@", typeArgs);
                    }
                    return toReturn;
                }
            }
            if (mappings.ContainsKey(typeWithNs))
            {
                return mappings[typeWithNs];
            }
            if(typeSymbol.OriginalDefinition.Locations.Length == 0)
            {
                return "any";
            }
            else if (!typeSymbol.OriginalDefinition.Locations.Any(s => s.IsInSource))
            {
                return "any";
            }
            else
            {
                // add reference
                var fqName = typeSymbol.OriginalDefinition.ToDisplayString(_symDisplayFormat);
                var locations = typeSymbol.OriginalDefinition.Locations.Select(x => x.SourceTree.FilePath);
                if (!references.ContainsKey(fqName))
                    references.Add(fqName, new HashSet<string>(locations));
                else
                    references[fqName].AddRange(locations);
                typeWithNs = fqName;
            }

            return typeWithNs;
        }


    }
}
