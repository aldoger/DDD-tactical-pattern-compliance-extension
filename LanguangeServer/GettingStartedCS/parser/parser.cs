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

        /// <summary>Compilation berisi syntax tree yang di-parse. Dipakai AggregateIdentifier untuk SemanticModel.</summary>
        public Compilation Compilation => compilation;

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
        /// <summary>Semua parameter sebuah method.</summary>
        public IEnumerable<ParameterSyntax> GetMethodParameters(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.ParameterList.Parameters;
        }

        /// <summary>
        /// Semua nama tipe yang disebut dalam sebuah tipe, termasuk argumen generic.
        /// Task&lt;IReadOnlyList&lt;Human?&gt;&gt; -> Task, IReadOnlyList, Human
        /// System.Guid -> Guid,  Order[] -> Order,  (Order, Money) -> Order, Money
        /// </summary>
        public IEnumerable<string> GetTypeNames(TypeSyntax? type)
        {
            switch (type)
            {
                case null:
                    yield break;
                case IdentifierNameSyntax id:
                    yield return id.Identifier.Text;
                    break;
                case GenericNameSyntax g:
                    yield return g.Identifier.Text;
                    foreach (var arg in g.TypeArgumentList.Arguments)
                        foreach (var n in GetTypeNames(arg)) yield return n;
                    break;
                case QualifiedNameSyntax q:
                    foreach (var n in GetTypeNames(q.Right)) yield return n;
                    break;
                case AliasQualifiedNameSyntax aq:
                    foreach (var n in GetTypeNames(aq.Name)) yield return n;
                    break;
                case NullableTypeSyntax nt:
                    foreach (var n in GetTypeNames(nt.ElementType)) yield return n;
                    break;
                case ArrayTypeSyntax at:
                    foreach (var n in GetTypeNames(at.ElementType)) yield return n;
                    break;
                case RefTypeSyntax rt:
                    foreach (var n in GetTypeNames(rt.Type)) yield return n;
                    break;
                case TupleTypeSyntax tt:
                    foreach (var el in tt.Elements)
                        foreach (var n in GetTypeNames(el.Type)) yield return n;
                    break;
                case PredefinedTypeSyntax pt:
                    yield return pt.Keyword.Text;
                    break;
            }
        }

        public bool IsMethodStatic(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        public bool IsMethodPrivate(MethodDeclarationSyntax methodDeclaration)
        {
            var mods = methodDeclaration.Modifiers;

            if (mods.Any(SyntaxKind.PrivateKeyword) && !mods.Any(SyntaxKind.ProtectedKeyword))
                return true;

            bool hasAccessModifier =
                mods.Any(SyntaxKind.PublicKeyword) || mods.Any(SyntaxKind.PrivateKeyword) ||
                mods.Any(SyntaxKind.ProtectedKeyword) || mods.Any(SyntaxKind.InternalKeyword);
            if (hasAccessModifier)
                return false;

            if (methodDeclaration.ExplicitInterfaceSpecifier != null)
                return false;

            return methodDeclaration.Parent is not InterfaceDeclarationSyntax;
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