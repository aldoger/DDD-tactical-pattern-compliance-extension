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

        public IEnumerable<PropertyDeclarationSyntax> GetCProperties(ClassDeclarationSyntax classDeclare)
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
    }
}