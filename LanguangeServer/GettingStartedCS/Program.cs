using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                @"using System;
                using System.Collections;
                using System.Linq;
                using System.Text;
 
                namespace Person
                {
                    class Human
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }
                        
                        public string Introduce()
                        {
                            return $""Hello, my name is {Name} and I am {Age} years old."";
                        }
                    }
                }");

            var root = (CompilationUnitSyntax)tree.GetRoot();
            var nameSpace = (NamespaceDeclarationSyntax)root.Members[0];
            var classProgram = (ClassDeclarationSyntax)nameSpace.Members[0];
            string className = classProgram.Identifier.Text;
            var classMembers = classProgram.Members[1];
        }

        

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
