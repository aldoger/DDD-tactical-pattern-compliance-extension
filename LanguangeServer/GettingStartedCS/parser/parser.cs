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

        public IEnumerable<ClassDeclarationSyntax> GetClasses()
        {
            return root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        }

        public IEnumerable<MethodDeclarationSyntax> GetMethods(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
        }

        public IEnumerable<PropertyDeclarationSyntax> GetProperties(ClassDeclarationSyntax classDeclare)
        {
            return classDeclare.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        }

        public string? GetBaseClassNameType(ClassDeclarationSyntax classDeclaration)
        {
            var baseType = classDeclaration.BaseList?.Types.FirstOrDefault();
            return baseType?.Type.ToString();
        }
        
        public bool HasConstructor(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Any();
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

        public bool IsPropertyImmutable(PropertyDeclarationSyntax property)
        {
            // Expression-bodied properties cannot be assigned through a setter.
            if (property.ExpressionBody != null)
                return true;

            var accessors = property.AccessorList?.Accessors;

            if (accessors is null)
                return false;

            bool hasGetter = accessors.Value.Any(a =>
                a.IsKind(SyntaxKind.GetAccessorDeclaration));

            bool hasSetter = accessors.Value.Any(a =>
                a.IsKind(SyntaxKind.SetAccessorDeclaration));

            bool hasInit = accessors.Value.Any(a =>
                a.IsKind(SyntaxKind.InitAccessorDeclaration));

            // A getter is required, and neither setter nor init accessor is allowed
            // under a strict getter-only policy.
            return hasGetter && !hasSetter && !hasInit;
        }

        static readonly HashSet<string> TemporalNames = new()
        { 
            "DateTime", "DateTimeOffset", "TimeSpan", "DateOnly", "TimeOnly"
        };

        public bool IsTemporalSyntax(PropertyDeclarationSyntax prop)
        {
            var type = prop.Type;

            // unwrap DateTime?
            if (type is NullableTypeSyntax nullable)
                type = nullable.ElementType;

            var name = type switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,                 // DateTime
                QualifiedNameSyntax q => q.Right.Identifier.Text,            // System.DateTime
                AliasQualifiedNameSyntax a => a.Name.Identifier.Text,          // global::System.DateTime
                _ => null
            };

            return name is not null && TemporalNames.Contains(name);
        }

    }
}