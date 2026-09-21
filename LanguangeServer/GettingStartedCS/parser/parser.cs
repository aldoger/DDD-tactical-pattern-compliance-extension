using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.parser
{
    public class Parser
    {
        private CompilationUnitSyntax root;

        public Parser()
        {
        }

        public void Parse(string input)
        {
            this.root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(input).GetRoot();
        }

        public IEnumerable<ClassDeclarationSyntax> GetClasses()
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            return classes;
        }

        public IEnumerable<MethodDeclarationSyntax> GetMethods(ClassDeclarationSyntax classDeclaration)
        {
            var methods = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            return methods;
        }

        public IEnumerable<PropertyDeclarationSyntax> GetProperties(ClassDeclarationSyntax classDeclaration)
        {
            var properties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            return properties;
        }
    }
}
