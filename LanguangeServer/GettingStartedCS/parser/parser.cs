using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GettingStartedCS.parser
{
    public class Parser
    {
        /// <summary>
        /// The global usings that SDK-style projects with &lt;ImplicitUsings&gt;enable&lt;/ImplicitUsings&gt; get from
        /// obj/.../GlobalUsings.g.cs. The extension skips obj/, so they are added here instead; without them,
        /// files that rely on implicit usings can't resolve Guid, Task, List&lt;T&gt;, ...
        /// Overlapping with a file's own usings is harmless.
        /// </summary>
        private const string ImplicitGlobalUsings = @"
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
";

        private static readonly CSharpParseOptions ParseOptions =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        /// <summary>The .NET base class library, loaded once per process.</summary>
        private static readonly Lazy<IReadOnlyList<MetadataReference>> FrameworkReferences =
            new(LoadFrameworkReferences);

        private IReadOnlyList<SyntaxTree> trees = Array.Empty<SyntaxTree>();
        private Compilation? compilation;
        private readonly Dictionary<SyntaxTree, SemanticModel> semanticModels = new();

        public Parser()
        {
        }

        /// <summary>
        /// Parses every file into its own SyntaxTree and builds ONE Compilation over all of them,
        /// so types in one file can reference types in another (Residence → Address).
        /// The file path is stored in each tree, so every Location points back to the real file and line.
        /// </summary>
        /// <param name="filePaths">The .cs files to analyze.</param>
        /// <param name="addImplicitUsings">
        /// Add the SDK's implicit global usings. Turn off for projects that don't use ImplicitUsings
        /// if it causes ambiguous type names.
        /// </param>
        public void ParseFiles(IEnumerable<string> filePaths, bool addImplicitUsings = true)
        {
            var parsed = new List<SyntaxTree>();

            foreach (var path in filePaths.Distinct())
            {
                try
                {
                    var text = File.ReadAllText(path);
                    parsed.Add(CSharpSyntaxTree.ParseText(text, ParseOptions, path));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // A file deleted or locked since the extension listed it shouldn't fail the whole run.
                    // stderr, because stdout carries only the JSON result.
                    Console.Error.WriteLine($"Skipping {path}: {ex.Message}");
                }
            }

            BuildCompilation(parsed, addImplicitUsings);
        }

        /// <summary>Parses a single source string (handy for tests). Same pipeline as <see cref="ParseFiles"/>.</summary>
        public void Parse(string input)
        {
            var tree = CSharpSyntaxTree.ParseText(input, ParseOptions, path: "input.cs");
            BuildCompilation(new[] { tree }, addImplicitUsings: true);
        }

        private void BuildCompilation(IReadOnlyList<SyntaxTree> sourceTrees, bool addImplicitUsings)
        {
            trees = sourceTrees;
            semanticModels.Clear();

            IEnumerable<SyntaxTree> allTrees = sourceTrees;
            if (addImplicitUsings)
            {
                allTrees = allTrees.Append(
                    CSharpSyntaxTree.ParseText(ImplicitGlobalUsings, ParseOptions, path: "ImplicitUsings.g.cs"));
            }

            compilation = CSharpCompilation.Create(
                "DddAnalysis",
                syntaxTrees: allTrees,
                references: FrameworkReferences.Value,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
        }

        /// <summary>
        /// References every assembly of the .NET runtime this analyzer runs on (System.Runtime,
        /// System.Collections, System.Collections.Concurrent, ...), so BCL types like HashSet&lt;T&gt;,
        /// ConcurrentDictionary and Task resolve. Assemblies of the analyzer itself (Roslyn, etc.)
        /// are left out.
        /// </summary>
        private static IReadOnlyList<MetadataReference> LoadFrameworkReferences()
        {
            var coreLib = typeof(object).Assembly.Location;
            var frameworkDir = Path.GetDirectoryName(coreLib);

            var trustedAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            var references = trustedAssemblies
                .Where(p => string.Equals(Path.GetDirectoryName(p), frameworkDir, StringComparison.OrdinalIgnoreCase))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToList();

            if (references.Count == 0)
            {
                references.Add(MetadataReference.CreateFromFile(coreLib));
            }

            return references;
        }

        /// <summary>Compilation berisi syntax tree yang di-parse. Dipakai AggregateIdentifier untuk SemanticModel.</summary>
        public Compilation Compilation =>
            compilation ?? throw new InvalidOperationException("Call Parse or ParseFiles first.");

        /// <summary>The parsed source files (without the generated implicit-usings file).</summary>
        public IReadOnlyList<SyntaxTree> SyntaxTrees => trees;

        /// <summary>
        /// A SemanticModel is scoped to ONE tree, so always get it for the tree the node lives in.
        /// Models are cached per tree.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxNode node)
        {
            if (!semanticModels.TryGetValue(node.SyntaxTree, out var model))
            {
                model = Compilation.GetSemanticModel(node.SyntaxTree);
                semanticModels[node.SyntaxTree] = model;
            }
            return model;
        }

        public IEnumerable<ClassDeclarationSyntax> GetClasses()
        {
            return trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>());
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