using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace GettingStartedCS.parser
{
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

        // Get all class declarations, methods in class, and properties in the syntax tree
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

        // Get Detail Information about a class, including its name, base type, methods, and properties
        public string GetPropertyType(PropertyDeclarationSyntax classProperty)
        {
            return classProperty.Type.ToString();
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
    }
}