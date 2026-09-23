using GettingStartedCS.main.ClassStructureInfo;
using GettingStartedCS.main.IdentifyDomainModel;
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
            string input = @"using System;
                using System.Collections;
                using System.Linq;
                using System.Text;
 
                namespace Person
                {
                    class Human
                    {
                        public string Id { get; set; }
                        public string Name { get; set; }
                        public int Age { get; set; }

                        public Human(string name, int age)
                        {
                            Name = name;
                            Age = age;
                        }
                        
                        public string Introduce()
                        {
                            return $""Hello, my name is {Name} and I am {Age} years old."";
                        }
                    }
                }";

            // SyntaxTree tree = CSharpSyntaxTree.ParseText(input);

            DomainModelList domainModelList = new DomainModelList();
            DomainModelIdentifier domainModelIdentifier = new DomainModelIdentifier();
            var parser = new parser.Parser();
            parser.Parse(input);
            var classes = parser.GetClasses();

            foreach (var classDeclaration in classes)
            {
                var classInfo = new ClassStructureInfo(classDeclaration.Identifier.Text);
                Console.WriteLine($"Class: {classDeclaration.Identifier.Text}");
                var methods = parser.GetMethods(classDeclaration);
                foreach (var method in methods)
                {
                    var classMethodInfo = new MethodStructureInfo(method.Identifier.Text);
                    classInfo.AddMethod(classMethodInfo);
                }
                var properties = parser.GetProperties(classDeclaration);
                foreach (var property in properties)
                {
                    var classPropertyInfo = new PropertyStructureInfo(property.Identifier.Text);
                    classInfo.AddProperty(classPropertyInfo);
                }
                domainModelIdentifier.IdentifyDomainModels(classDeclaration, classInfo, domainModelList);
            }

            foreach(var domainModel in domainModelList.DomainModels)
            {
                switch(domainModel.DomainType)
                {
                    case "Entity":
                        Console.WriteLine($"Entity: {domainModel.ClassName}");
                        break;
                    case "Value Object":
                        Console.WriteLine($"Value Object: {domainModel.ClassName}");
                        break;
                    case "Aggregate Root":
                        Console.WriteLine($"Aggregate Root: {domainModel.ClassName}");
                        break;
                    default:
                        Console.WriteLine($"Unknown Domain Type: {domainModel.ClassName}");
                        break;
                }
            }

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
