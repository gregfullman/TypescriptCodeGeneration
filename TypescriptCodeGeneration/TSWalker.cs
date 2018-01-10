using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace TypescriptCodeGeneration
{
    class TSWalker : CSharpSyntaxWalker
    {
        private SemanticModel model;
        private TsContext context;
        private static SymbolDisplayFormat _symDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        private Dictionary<string, string> _subClassConversions = new Dictionary<string, string>();

        private readonly CSharpFile _csFile;
        public CSharpFile CSFile
        {
            get { return _csFile; }
        }

        private List<CSharpFileElement> _currentFileElementHierarchy = new List<CSharpFileElement>();

        public TSWalker(SyntaxTree syntaxTree, TsOutputer output, TsContext context, SemanticModel model)
        {
            _csFile = new CSharpFile(syntaxTree.FilePath);
            this.model = model;
            this.context = context;
        }


        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var symbol = model.GetDeclaredSymbol(node);
            var fqName = symbol.ToDisplayString(_symDisplayFormat);

            CSharpNamespace nsObj = null;
            if (!CSFile.Elements.TryGetValue(fqName, out nsObj))
            {
                nsObj = new CSharpNamespace();

                var commentXml = symbol.GetDocumentationCommentXml();

                nsObj.SetSummaryViaXml(commentXml);
                nsObj.DisplayText = fqName;
                CSFile.Elements.Add(fqName, nsObj);
            }

            base.VisitNamespaceDeclaration(node);
        }

        private string GetGenericTypeSuffix(INamedTypeSymbol symbol, Dictionary<string, HashSet<string>> references)
        {
            string suffix = "";
            if (symbol.IsGenericType)
            {
                string[] types = symbol.TypeArguments.Select(s => context.GetTsType(s, references)).ToArray();
                //if (types.Length == 1)
                //{
                    // TODO: for right now we're constraining this to one generic parameter.
                    suffix = string.Format("<{0}>", string.Join(",", types));
                //}
            }
            return suffix;
        }

        private CSharpProperty BuildProperty(IPropertySymbol symbol, CSharpClass referencingClass, string nameSpace = null)
        {
            var prop = new CSharpProperty();

            var commentXml = symbol.GetDocumentationCommentXml();
            var typeName = symbol.Type.Name.ToString();
            Dictionary<string, HashSet<string>> references = new Dictionary<string, HashSet<string>>();
            var newTypeName = context.GetTsType(symbol.Type, references);
            //if (newTypeName == null || string.IsNullOrEmpty(newTypeName))
            //{

            //}
            // remove the namespace if it's the same
            if (nameSpace != null && newTypeName.StartsWith(nameSpace))
            {
                newTypeName = newTypeName.Remove(0, nameSpace.Length + 1); // +1 for the dot
            }

            if (references.Count > 0)
                referencingClass.AddReferences(references);

            prop.SetSummaryViaXml(commentXml);
            var name = symbol.Name.ToString();
            prop.DisplayText = name + ": " + newTypeName + ";";

            return prop;
        }

        private IEnumerable<KeyValuePair<string, CSharpProperty>> GetBasePropertiesFromExternalAssembly(INamedTypeSymbol symbol, CSharpClass referencingClass)
        {
            if(symbol.BaseType != null && 
               symbol.BaseType.ToDisplayString(_symDisplayFormat) != "System.Object" &&
               symbol.BaseType.ToDisplayString(_symDisplayFormat) != "System.Configuration.ApplicationSettingsBase" &&
               !symbol.BaseType.OriginalDefinition.Locations.Any(s => s.IsInSource))
            {
                var properties = symbol.GetAccessibleMembersInThisAndBaseTypes(SymbolKind.Property, Accessibility.Public);
                foreach (ISymbol property in properties)
                {
                    var propSymbol = property as IPropertySymbol;
                    if (propSymbol != null &&
                        propSymbol.GetMethod != null &&
                        propSymbol.SetMethod != null)
                    {
                        yield return new KeyValuePair<string, CSharpProperty>(propSymbol.Name, BuildProperty(propSymbol, referencingClass, null));
                    }
                }
            }
        }

        private string GetExtendingType(INamedTypeSymbol symbol, Dictionary<string, HashSet<string>> references)
        {
            string extendingType = "";
            INamedTypeSymbol tempSymbol = symbol;
            string baseTypeName = null;
            // go up the inheritance tree
            while(tempSymbol.BaseType != null && (baseTypeName = tempSymbol.BaseType.ToDisplayString(_symDisplayFormat)) != "System.Object")
            {
                if (tempSymbol.BaseType.OriginalDefinition.Locations.Any(s => s.IsInSource))
                {
                    // The base type is defined somewhere else in the solution, so we can just use this type.
                    // The type will be resolved based on the references
                    extendingType = context.GetTsType(tempSymbol.BaseType, references);
                    break;
                }

                bool breakWhile = false;
                switch(baseTypeName)
                {
                    case "System.Collections.Generic.Dictionary":
                        if(symbol.TypeArguments.Length == 0 &&
                           tempSymbol.BaseType.TypeArguments.Length == 2)
                        {
                            // TODO: check that the first argument is either an integer or a string
                            // The class is defined as a non-generic class that inherits from the generic dictionary, where types are defined.
                            // In this case, we can define an indexer property for the class
                             
                        }
                        breakWhile = true;
                        break;
                    default:
                        break;
                }

                if (breakWhile)
                    break;
                tempSymbol = tempSymbol.BaseType;
            }

            if(string.IsNullOrWhiteSpace(extendingType))
            {
                foreach(var interf in tempSymbol.AllInterfaces)
                {
                    var interfaceName = interf.ToDisplayString(_symDisplayFormat);
                    if(interfaceName.StartsWith("System.Collections.Generic.IDictionary"))
                    {
                        // TODO: not supported yet
                        break;
                    }
                    else if(interfaceName.StartsWith("System.Collections.Generic.IList") ||
                            interfaceName.StartsWith("System.Collections.Generic.ICollection"))
                    {
                        extendingType = string.Format("Array{0}", GetGenericTypeSuffix(interf, references));
                        break;
                    }
                    else if(interfaceName.StartsWith("System.Collections.IList") ||
                            interfaceName.StartsWith("System.Collectins.ICollection"))
                    {
                        extendingType = "Array";
                        break;
                    }
                }
            }


            return extendingType;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var symbol = model.GetDeclaredSymbol(node);
            var fqName = symbol.ToDisplayString(_symDisplayFormat);

            string ns, name;
            Extensions.GetNamespaceAndName(fqName, out ns, out name);
            if (!string.IsNullOrWhiteSpace(ns))
            {
                CSharpNamespace nsObj;
                if (!CSFile.Elements.TryGetValue(ns, out nsObj))
                {
                    // This handles classes within classes
                    nsObj = new CSharpNamespace();
                    nsObj.DisplayText = ns;
                    CSFile.Elements.Add(ns, nsObj);
                    _subClassConversions.Add(fqName, string.Format("{0}.{1}", fqName, name));
                }
                var classObj = new CSharpClass() { IsPartial = symbol.DeclaringSyntaxReferences.Length > 1 };

                Dictionary<string, HashSet<string>> references = new Dictionary<string, HashSet<string>>();
                string typeSuffix = GetGenericTypeSuffix(symbol, references);
                classObj.ExtendedInterface = GetExtendingType(symbol, references);
                if (string.IsNullOrEmpty(classObj.ExtendedInterface))
                {
                    foreach (var baseProperty in GetBasePropertiesFromExternalAssembly(symbol, classObj))
                        if (!baseProperty.Key.StartsWith("this", StringComparison.Ordinal) &&
                           !classObj.Children.ContainsKey(baseProperty.Key))
                        {
                            classObj.Children.Add(baseProperty.Key, baseProperty.Value);
                        }
                }

                var commentXml = symbol.GetDocumentationCommentXml();

                classObj.SetSummaryViaXml(commentXml);
                classObj.DisplayText = name + typeSuffix;

                if (references.Count > 0)
                    classObj.AddReferences(references);

                nsObj.Children.Add(name, classObj);
            }
            base.VisitClassDeclaration(node);

        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            base.VisitPropertyDeclaration(node);

            var symbol = model.GetDeclaredSymbol(node);
            var containingSymbol = symbol.ContainingSymbol;
            var fqName = containingSymbol.ToDisplayString(_symDisplayFormat);

            string ns, className;
            Extensions.GetNamespaceAndName(fqName, out ns, out className);
            if (!string.IsNullOrWhiteSpace(ns))
            {
                CSharpNamespace nsObj;
                if (CSFile.Elements.TryGetValue(ns, out nsObj))
                {
                    // get the class object
                    ICSharpFileElement classEle;
                    if (nsObj.Children.TryGetValue(className, out classEle) && classEle is CSharpClass)
                    {
                        var classObj = classEle as CSharpClass;
                        // TODO: do we want to allow for getter-only properties?
                        if (symbol.GetMethod != null && symbol.SetMethod != null)
                        {
                            var prop = BuildProperty(symbol, classObj, ns);
                            if(!classObj.Children.ContainsKey(node.Identifier.ToString()))
                                classObj.Children.Add(node.Identifier.ToString(), prop);
                        }
                    }
                }
            }
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var symbol = model.GetDeclaredSymbol(node);
            var fqName = symbol.ToDisplayString(_symDisplayFormat);

            string ns, name;
            Extensions.GetNamespaceAndName(fqName, out ns, out name);
            if (!string.IsNullOrWhiteSpace(ns))
            {
                CSharpNamespace nsObj;
                if (!CSFile.Elements.TryGetValue(ns, out nsObj))
                {
                    // This handles enums within classes
                    nsObj = new CSharpNamespace();
                    nsObj.DisplayText = ns;
                    CSFile.Elements.Add(ns, nsObj);
                    _subClassConversions.Add(fqName, string.Format("{0}.{1}", fqName, name));
                }
                var enumObj = new CSharpEnum();
                enumObj.DisplayText = node.Identifier.ToString();
                var commentXml = symbol.GetDocumentationCommentXml();
                enumObj.SetSummaryViaXml(commentXml);
                nsObj.Children.Add(name, enumObj);
            }
            base.VisitEnumDeclaration(node);
        }

        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            var symbol = model.GetDeclaredSymbol(node);
            var containingSymbol = symbol.ContainingSymbol;
            var fqName = containingSymbol.ToDisplayString(_symDisplayFormat);

            string ns, className;
            Extensions.GetNamespaceAndName(fqName, out ns, out className);
            if (!string.IsNullOrWhiteSpace(ns))
            {
                CSharpNamespace nsObj;
                if (CSFile.Elements.TryGetValue(ns, out nsObj))
                {
                    // get the class object
                    ICSharpFileElement classEle;
                    if (nsObj.Children.TryGetValue(className, out classEle) && classEle is CSharpEnum)
                    {
                        var enumObj = classEle as CSharpEnum;

                        var commentXml = symbol.GetDocumentationCommentXml();

                        var member = new CSharpEnumMember { Value = symbol.ConstantValue, DisplayText = symbol.MetadataName };
                        member.SetSummaryViaXml(commentXml);
                        enumObj.Children.Add(symbol.MetadataName, member);
                    }
                }
            }
            base.VisitEnumMemberDeclaration(node);
        }
        
    }
}
