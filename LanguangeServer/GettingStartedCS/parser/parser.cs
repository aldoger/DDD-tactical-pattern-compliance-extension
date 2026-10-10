using GettingStartedCS.main.StructureInfo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace GettingStartedCS.parser
{
    using GettingStartedCS.main.StructureInfo;
    public class Parser
    {
        private SyntaxTree tree;
        private CompilationUnitSyntax root;
        private Compilation compilation;
        private SemanticModel semanticModel;

        public Parser()
        {
            semanticModel = null;
        }

        public void Parse(string input)
        {
            this.tree = CSharpSyntaxTree.ParseText(input);
            this.root = (CompilationUnitSyntax)tree.GetRoot();

            // Build a Compilation from this tree so the SemanticModel has
            // something to bind symbols against.
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location) // System.Private.CoreLib
            };

            this.compilation = CSharpCompilation.Create("ParserAssembly")
                .AddReferences(references)
                .AddSyntaxTrees(tree);

            // The SemanticModel is scoped to ONE tree, obtained from the Compilation
            this.semanticModel = compilation.GetSemanticModel(tree);
        }

        public IEnumerable<ClassDeclarationSyntax> GetClasses()
        {
            return root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        }

        public IEnumerable<MethodDeclarationSyntax> GetMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
        }

        public IEnumerable<PropertyDeclarationSyntax> GetProperties(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        }

        public string GetPropertyType(PropertyDeclarationSyntax classProperty)
        {
            return classProperty.Type.ToString();
        }
        public string? GetBaseClassName(ClassDeclarationSyntax classDeclaration)
        {
            var baseType = classDeclaration.BaseList?.Types.FirstOrDefault();
            return baseType?.Type.ToString();
        }
        public List<string> GetClassInterfaceName(ClassDeclarationSyntax classDeclaration)
        {
            var interfaceTypes = classDeclaration.BaseList?.Types.Where(t => t.Type is IdentifierNameSyntax id && id.Identifier.Text.StartsWith("I"));
            return interfaceTypes?.Select(t => t.Type.ToString()).ToList() ?? new List<string>();
        }
        public bool IsPropertyImmutable(PropertyDeclarationSyntax classProperty)
        {
            // Case 1: Expression-bodied property (e.g. `public string Name => _name;`)
            // No accessor list at all — this is inherently get-only, so immutable.
            if (classProperty.ExpressionBody != null && classProperty.AccessorList == null)
            {
                return true;
            }

            // Case 2: Has explicit "readonly" modifier on the property itself
            // (valid in readonly struct members, or explicit readonly properties in C# 8+)
            bool hasReadonlyModifier = classProperty.Modifiers
                .Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

            if (hasReadonlyModifier)
            {
                return true;
            }

            // Case 3: No accessor list and no expression body — malformed/incomplete syntax
            if (classProperty.AccessorList == null)
            {
                return false;
            }

            var accessors = classProperty.AccessorList.Accessors;

            bool hasGet = accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            bool hasSet = accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
            bool hasInit = accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration));

            // Case 4: Has "set" — mutable, not immutable, regardless of get/init presence
            if (hasSet)
            {
                return false;
            }

            // Case 5: Has "init" (with or without get) — immutable after construction
            if (hasInit)
            {
                return true;
            }

            // Case 6: Only "get", no set, no init — e.g. `public string Name { get; }`
            if (hasGet)
            {
                return true;
            }

            return false;
        }
        public string GetClassBaseType(ClassDeclarationSyntax classDeclaration)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol == null)
                return null;

            // If the base is just System.Object, treat it as "no explicit base class"
            if (classSymbol.BaseType?.SpecialType == SpecialType.System_Object)
                return null;

            return classSymbol.BaseType.Name;
        }
        
        public bool HasConstructor(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Any();
        }
        public List<ParameterStructureInfo> GetMethodsParameter(MethodDeclarationSyntax methodDeclaration)
        {
            var parameters = new List<ParameterStructureInfo>();
            foreach (var parameter in methodDeclaration.ParameterList.Parameters)
            {
                if(parameter.Type == null)
                {
                    parameters.Add(new ParameterStructureInfo(parameter.Identifier.Text, "unknown"));
                    continue;
                }
                parameters.Add(new ParameterStructureInfo(parameter.Identifier.Text, parameter.Type.ToString()));
            }
            return parameters;
        }
        public string GetMethodReturnType(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.ReturnType.ToString();
        }
        public bool IsMethodStatic(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }
        public bool IsClassStatic(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }
        public bool IsClassImmutable(ClassDeclarationSyntax classDeclaration)
        {
            var propertiesOk = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
                .All(IsPropertyImmutable);

            var fieldsOk = classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .Where(f => !f.Modifiers.Any(SyntaxKind.StaticKeyword)
                         && !f.Modifiers.Any(SyntaxKind.ConstKeyword))
                .All(f => f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword));

            return propertiesOk && fieldsOk;
        }
        public bool HasValueEquality(ClassDeclarationSyntax classDeclaration)
        {
            var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (symbol == null)
                return false;

            if (symbol.IsRecord)
                return true;

            if (symbol.AllInterfaces.Any(i => i.OriginalDefinition.ToDisplayString() == "System.IEquatable<T>"))
                return true;

            bool overridesEquals = false, overridesHash = false;
            for (var t = symbol; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType)
            {
                overridesEquals |= t.GetMembers("Equals").OfType<IMethodSymbol>()
                    .Any(m => m.IsOverride && m.Parameters.Length == 1
                           && m.Parameters[0].Type.SpecialType == SpecialType.System_Object);

                overridesHash |= t.GetMembers("GetHashCode").OfType<IMethodSymbol>()
                    .Any(m => m.IsOverride && m.Parameters.Length == 0);
            }

            return overridesEquals && overridesHash;
        }
    }
}