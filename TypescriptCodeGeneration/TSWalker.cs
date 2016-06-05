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
                if (types.Length == 1)
                {
                    // TODO: for right now we're constraining this to one generic parameter.
                    suffix = string.Format("<{0}>", string.Join(",", types));
                }
            }
            return suffix;
        }

        private string GetExtendingType(INamedTypeSymbol symbol, Dictionary<string, HashSet<string>> references)
        {
            string extendingType = "";
            INamedTypeSymbol tempSymbol = symbol;
            string baseTypeName = null;
            // go up the inheritance tree
            while(tempSymbol.BaseType != null && (baseTypeName = tempSymbol.BaseType.ToDisplayString(_symDisplayFormat)) != "System.Object")
            {
                if (symbol.BaseType.OriginalDefinition.Locations.Any(s => s.IsInSource))
                {
                    // The base type is defined somewhere else in the solution, so we can just use this type.
                    // The type will be resolved based on the references
                    extendingType = context.GetTsType(symbol.BaseType, references);
                    break;
                }

                bool breakWhile = false;
                switch(baseTypeName)
                {
                    case "System.Collections.Generic.Dictionary":
                        // TODO: not supported yet
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
                            var prop = new CSharpProperty();

                            var commentXml = symbol.GetDocumentationCommentXml();
                            var typeName = symbol.Type.Name.ToString();
                            Dictionary<string, HashSet<string>> references = new Dictionary<string, HashSet<string>>();
                            var newTypeName = context.GetTsType(symbol.Type, references);

                            // remove the namespace if it's the same
                            if (newTypeName.StartsWith(ns))
                            {
                                newTypeName = newTypeName.Remove(0, ns.Length + 1); // +1 for the dot
                            }

                            if (references.Count > 0)
                                classObj.AddReferences(references);

                            prop.SetSummaryViaXml(commentXml);
                            var name = node.Identifier.ToString();
                            prop.DisplayText = name + ": " + newTypeName + ";";

                            classObj.Children.Add(name, prop);
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

                        var member = new CSharpEnumMember();
                        member.SetSummaryViaXml(commentXml);
                        var name = symbol.MetadataName;
                        member.DisplayText = name;

                        enumObj.Children.Add(name, member);
                    }
                }
            }
            base.VisitEnumMemberDeclaration(node);
        }
        
    }
}
